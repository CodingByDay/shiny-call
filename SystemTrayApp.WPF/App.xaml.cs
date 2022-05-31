using AsterNET.Manager;
using AsterNET.Manager.Event;
using SIPSorcery.Media;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;

using SIPSorceryMedia.Windows;
using System;
using System.Collections.Generic;
using System;

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

using SIPSorcery.Sys;
using SIPSorceryMedia.Abstractions;
using System.Threading;
using Serilog.Events;
using Serilog;
using System.Configuration;
using System.Net;
using SIPSorcery.SoftPhone;

using Microsoft.Data.Sqlite;
using ShinyCall.Sqlite;
using ShinyCall.Mappings;

namespace SystemTrayApp.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const int SIP_CLIENT_COUNT = 2;                             // The number of SIP clients (simultaneous calls) that the UI can handle.
        private const int ZINDEX_TOP = 10;
        private const int REGISTRATION_EXPIRY = 180;
        private string caller = string.Empty;
        private bool isMissedCall = true;
        private string m_sipUsername = SIPSoftPhoneState.SIPUsername;
        private string m_sipPassword = SIPSoftPhoneState.SIPPassword;
        private string m_sipServer = SIPSoftPhoneState.SIPServer;
        private bool m_useAudioScope = SIPSoftPhoneState.UseAudioScope;

        private SIPTransportManager _sipTransportManager;
        private List<SIPClient> _sipClients;
        private SoftphoneSTUNClient _stunClient;                    // STUN client to periodically check the public IP address.
        private SIPRegistrationUserAgent _sipRegistrationClient;    // Can be used to register with an external SIP provider if incoming calls are required.

#pragma warning disable CS0649
        private WriteableBitmap _client0WriteableBitmap;
        private WriteableBitmap _client1WriteableBitmap;
#pragma warning restore CS0649
        //private AudioScope.AudioScope _audioScope0;
        //private AudioScope.AudioScopeOpenGL _audioScopeGL0;
        //private AudioScope.AudioScope _audioScope1;
        //private AudioScope.AudioScopeOpenGL _audioScopeGL1;
        //private AudioScope.AudioScope _onHoldAudioScope;
        //private AudioScope.AudioScopeOpenGL _onHoldAudioScopeGL;

        public App()
        {
            InitializeComponent();
            BusinessLogic();

            


  

       
         


        }


        private async void BusinessLogic()
        {
            ResetToCallStartState(null);

            _sipTransportManager = new SIPTransportManager();
            _sipTransportManager.IncomingCall += SIPCallIncoming;

            _sipClients = new List<SIPClient>();

            // If a STUN server hostname has been specified start the STUN client to lookup and periodically 
            // update the public IP address of the host machine.
            if (!SIPSoftPhoneState.STUNServerHostname.IsNullOrBlank())
            {
                _stunClient = new SoftphoneSTUNClient(SIPSoftPhoneState.STUNServerHostname);
                _stunClient.PublicIPAddressDetected += (ip) =>
                {
                    SIPSoftPhoneState.PublicIPAddress = ip;
                };
                _stunClient.Run();
            }


            await Initialize();


        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            await Initialize();
        }

        /// <summary>
        /// Initialises the SIP clients and transport.
        /// </summary>
        private async Task Initialize()
        {
            await _sipTransportManager.InitialiseSIP();

            for (int i = 0; i < SIP_CLIENT_COUNT; i++)
            {
                var sipClient = new SIPClient(_sipTransportManager.SIPTransport);

                sipClient.CallAnswer += SIPCallAnswered;
                sipClient.CallEnded += ResetToCallStartState;
                sipClient.StatusMessage += SipClient_StatusMessage;
                sipClient.RemotePutOnHold += RemotePutOnHold;
                sipClient.RemoteTookOffHold += RemoteTookOffHold;

                _sipClients.Add(sipClient);
            }

            string listeningEndPoints = null;

            foreach (var sipChannel in _sipTransportManager.SIPTransport.GetSIPChannels())
            {
                SIPEndPoint sipChannelEP = sipChannel.ListeningSIPEndPoint.CopyOf();
                sipChannelEP.ChannelID = null;
                listeningEndPoints += (listeningEndPoints == null) ? sipChannelEP.ToString() : $", {sipChannelEP}";
            }

            string port = $"Listening on: {listeningEndPoints}";

            _sipRegistrationClient = new SIPRegistrationUserAgent(
                _sipTransportManager.SIPTransport,
                m_sipUsername,
                m_sipPassword,
                m_sipServer,
                REGISTRATION_EXPIRY);

            _sipRegistrationClient.Start();

        }



        private void SipClient_StatusMessage(SIPClient arg1, string arg2)
        {
            var ar1 = arg1;
            var ar2 = arg2;

            var stop = true;
        }

        /// <summary>
        /// Application closing, shutdown the SIP, Google Voice and STUN clients.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var sipClient in _sipClients)
            {
                sipClient.Shutdown();
            }

            _sipTransportManager.Shutdown();
            _stunClient?.Stop();
        }

        /// <summary>
        /// Reset the UI elements to their initial state at the end of a call.
        /// </summary>
        private async void ResetToCallStartState(SIPClient sipClient)
        {
            CallModel call = new CallModel();
            call.caller = caller;
            call.status = "Missed";
            call.time = DateTime.Now.ToString();
            SqliteDataAccess.InsertCallHistory(call);

            if (sipClient == null || sipClient == _sipClients[0])
            {

            }

            if (sipClient == null || sipClient == _sipClients[1])
            {

            }

           
        }
       
        private bool SIPCallIncoming(SIPRequest sipRequest)
        {
            isMissedCall = true;
            caller = sipRequest.Header.From.FriendlyDescription();
            string nameCaller = $"Incoming call from {sipRequest.Header.From.FriendlyDescription()}.";

            if (!_sipClients[0].IsCallActive)
            {
                _sipClients[0].Accept(sipRequest);
            

                return true;
            }
            else if (!_sipClients[1].IsCallActive)
            {
                _sipClients[1].Accept(sipRequest);
          

                return true;
            }
            else
            {
                return false;
            }
           
        }

        private async Task AnswerTest()
        {
            if(_sipClients[0]!=null)
            {
               await _sipClients[0].Answer();
            } else
            {
               await _sipClients[2].Answer();
            }
        }
        private async void SIPCallAnswered(SIPClient client)
        {
            isMissedCall = false;

            if (client == _sipClients[0])
            {
                if (_sipClients[1].IsCallActive && !_sipClients[1].IsOnHold)
                {
                    //_sipClients[1].PutOnHold(_onHoldAudioScopeGL);
                    await _sipClients[1].PutOnHold();
                }


            }
            else if (client == _sipClients[1])
            {


                if (_sipClients[0].IsCallActive)
                {
                    if (!_sipClients[0].IsOnHold)
                    {
                        //_sipClients[0].PutOnHold(_onHoldAudioScopeGL);
                        await _sipClients[0].PutOnHold();
                    }


                }
            }
            CallModel call = new CallModel();
            call.caller = caller;
            call.status = "Answered";
            call.time = DateTime.Now.ToString();
            SqliteDataAccess.InsertCallHistory(call);
        }




        /// <summary>
        /// Answer an incoming call on the SipClient
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private async Task AnswerCallAsync(SIPClient client)
        {
            bool result = await client.Answer();

            if (result)
            {
                SIPCallAnswered(client);
            }
            else
            {
                ResetToCallStartState(client);
            }
        }





        /// <summary>
        /// The button to initiate an attended transfer request between the two in active calls.
        /// </summary>
        private async void AttendedTransferButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool wasAccepted = await _sipClients[1].AttendedTransfer(_sipClients[0].Dialogue);

            if (!wasAccepted)
            {
            }
        }

        /// <summary>
        /// The remote call party put us on hold.
        /// </summary>
        private void RemotePutOnHold(SIPClient sipClient)
        {
            // We can't put them on hold if they've already put us on hold.

            if (sipClient == _sipClients[0])
            {

            }
            else if (sipClient == _sipClients[1])
            {

            }
        }

        /// <summary>
        /// The remote call party has taken us off hold.
        /// </summary>
        private void RemoteTookOffHold(SIPClient sipClient)
        {

            if (sipClient == _sipClients[0])
            {

            }
            else if (sipClient == _sipClients[1])
            {

            }
        }





      

    }
    }



