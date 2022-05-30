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
        int SIP_LISTEN_PORT = 5060;
        private SIPTransport _sipTransport;
        private SIPRegistrationUserAgent regUserAgent;
        private SIPUserAgent agent;

        struct SendSilenceJob
        {
            public Timer SendSilenceTimer;
            public SIPUserAgent UserAgent;

            public SendSilenceJob(Timer timer, SIPUserAgent ua)
            {
                SendSilenceTimer = timer;
                UserAgent = ua;
            }
        }


        public App()
        {
            InitializeComponent();
            BusinessLogic();

        }

        private void BusinessLogic()
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
         

                _sipClients.Add(sipClient);
            }

            string listeningEndPoints = null;

            foreach (var sipChannel in _sipTransportManager.SIPTransport.GetSIPChannels())
            {
                SIPEndPoint sipChannelEP = sipChannel.ListeningSIPEndPoint.CopyOf();
                sipChannelEP.ChannelID = null;
                listeningEndPoints += (listeningEndPoints == null) ? sipChannelEP.ToString() : $", {sipChannelEP}";
            }

            string msg = $"Listening on: {listeningEndPoints}";

            _sipRegistrationClient = new SIPRegistrationUserAgent(
                _sipTransportManager.SIPTransport,
                m_sipUsername,
                m_sipPassword,
                m_sipServer,
                REGISTRATION_EXPIRY);

            _sipRegistrationClient.Start();
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
        private void ResetToCallStartState(SIPClient sipClient)
        {
            if (sipClient == null || sipClient == _sipClients[0])
            {
             
            }

            if (sipClient == null || sipClient == _sipClients[1])
            {
            }
        }

        /// <summary>
        /// Checks if there is a client that can accept the call and if so sets up the UI
        /// to present the handling options to the user.
        /// </summary>
        private bool SIPCallIncoming(SIPRequest sipRequest)
        {
            string msg = $"Incoming call from {sipRequest.Header.From.FriendlyDescription()}.";

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

        /// <summary>
        /// Set up the UI to present options for an established SIP call, i.e. hide the cancel 
        /// button and display they hangup button.
        /// </summary>
        private async void SIPCallAnswered(SIPClient client)
        {
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
        /// Called when the active SIP client has a bitmap representing the remote video stream
        /// ready.
        /// </summary>
        /// <param name="sample">The bitmap sample in pixel format BGR24.</param>
        /// <param name="width">The bitmap width.</param>
        /// <param name="height">The bitmap height.</param>
        /// <param name="stride">The bitmap stride.</param>
        private void VideoSampleReady(byte[] sample, uint width, uint height, int stride, VideoPixelFormatsEnum pixelFormat, WriteableBitmap wBmp, System.Windows.Controls.Image dst)
        {
            if (sample != null && sample.Length > 0)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var bmpPixelFormat = PixelFormats.Bgr24;
                    switch (pixelFormat)
                    {
                        case VideoPixelFormatsEnum.Bgr:
                            bmpPixelFormat = PixelFormats.Bgr24;
                            break;
                        case VideoPixelFormatsEnum.Bgra:
                            bmpPixelFormat = PixelFormats.Bgra32;
                            break;
                        case VideoPixelFormatsEnum.Rgb:
                            bmpPixelFormat = PixelFormats.Rgb24;
                            break;
                        default:
                            bmpPixelFormat = PixelFormats.Bgr24;
                            break;
                    }

                    if (wBmp == null || wBmp.Width != width || wBmp.Height != height)
                    {
                        wBmp = new WriteableBitmap(
                            (int)width,
                            (int)height,
                            96,
                            96,
                            bmpPixelFormat,
                            null);

                        dst.Source = wBmp;
                    }

                    // Reserve the back buffer for updates.
                    wBmp.Lock();

                    Marshal.Copy(sample, 0, wBmp.BackBuffer, sample.Length);

                    // Specify the area of the bitmap that changed.
                    wBmp.AddDirtyRect(new Int32Rect(0, 0, (int)width, (int)height));

                    // Release the back buffer and make it available for display.
                    wBmp.Unlock();
                }), System.Windows.Threading.DispatcherPriority.Normal);
            }
        }

        

       

      
        

    
        private void RegUserAgent_RegistrationSuccessful(SIPURI obj)
        {
            string message = "Success";
            int r = 4;

            _sipTransport = new SIPTransport();
            _sipTransport.AddSIPChannel(new SIPUDPChannel(new IPEndPoint(IPAddress.Any, SIP_LISTEN_PORT)));

            var userAgent = new SIPUserAgent(_sipTransport, null, true);
   
            userAgent.OnIncomingCall += async (ua, req) =>
            {
                WindowsAudioEndPoint winAudioEP = new WindowsAudioEndPoint(new AudioEncoder());
                VoIPMediaSession voipMediaSession = new VoIPMediaSession(winAudioEP.ToMediaEndPoints());
                voipMediaSession.AcceptRtpFromAny = true;


                var uas = userAgent.AcceptCall(req);
                await userAgent.Answer(uas, voipMediaSession);
            };




        }

        private void RegUserAgent_RegistrationRemoved(SIPURI obj)
        {
            string message = "Removed";
            int r = 4;

        }

        private void RegUserAgent_RegistrationTemporaryFailure(SIPURI arg1, string arg2)
        {
            string message = "temporary";
            int r = 4;

        }

        private void RegUserAgent_RegistrationFailed(SIPURI arg1, string arg2)
        {
            string message = "fail";
                        int r = 4;

        }
    }
    }


