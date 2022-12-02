﻿using AsterNET.Manager;
using AsterNET.Manager.Event;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using ShinyCall;
using ShinyCall.Mappings;
using ShinyCall.MVVM.ViewModel;
using ShinyCall.Services;
using ShinyCall.Sqlite;
using SIPSorcery.SIP.App;
using SIPSorcery.SoftPhone;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;


using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using TinyJson;
using AppCenterExtensions;
using System.Linq.Expressions;

namespace SystemTrayApp.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NotifyIconWrapper.NotifyRequestRecord? _notifyRequest;
        private MainViewModel context = new MainViewModel();
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
        private string? phone;
        private string? currentPhone;
        private ManagerConnection manager;
        private CallModel caller_model;
        private Guid id_unique = Guid.NewGuid();
        private Guid commited_guid = Guid.NewGuid();
        private bool answered = false;
        private string calleridname;
        private string calleridnumber;
        private string nameCaller;

        public bool MainBoleanValue { get; private set; }
#pragma warning restore CS0649
        //private AudioScope.AudioScope _audioScope0;
        //private AudioScope.AudioScopeOpenGL _audioScopeGL0;
        //private AudioScope.AudioScope _audioScope1;
        //private AudioScope.AudioScopeOpenGL _audioScopeGL1;
        //private AudioScope.AudioScope _onHoldAudioScope;
        //private AudioScope.AudioScopeOpenGL _onHoldAudioScopeGL;

        public App()
        {
            Crashes.SetEnabledAsync(true);
            Microsoft.AppCenter.AppCenter.Start("557b220c-9c91-4bc3-909f-90eefae8a75a", typeof(Analytics), typeof(Crashes));
            Crashes.NotifyUserConfirmation(UserConfirmation.AlwaysSend); /* Always send crash reports */ /*https://appcenter.ms/apps */
            Analytics.SetEnabledAsync(true);      
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string path = System.IO.Path.Combine(startupPath, "ShinyCall.exe");
            CreateShortcut(Environment.ProcessPath);
            InitializeComponent();
            BusinessLogic();     
        }

 

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Crashes.TrackError(e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Crashes.TrackError((Exception)e.ExceptionObject);
            var isTerminating = e.IsTerminating;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Crashes.TrackError(e.Exception);
            e.Handled = true;
        }

        public void CreateShortcut(string app)
        {
            string link = Environment.GetFolderPath(Environment.SpecialFolder.Startup)
                + Path.DirectorySeparatorChar + "ShinyCall" + ".lnk";
            var shell = new WshShell();
            var shortcut = shell.CreateShortcut(link) as IWshShortcut;
            shortcut.TargetPath = app;
            shortcut.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            //shortcut...
            shortcut.Save();
        }

        Notifier notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 10,
                offsetY: 10);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(3));
            cfg.DisplayOptions.Width = 200;

            cfg.Dispatcher = Application.Current.Dispatcher;
        });


        private bool alreadyShown = false;
        private Popup popup;

        private async void BusinessLogic()
        {
            var options = new MessageOptions
            {
                ShowCloseButton = false, // set the option to show or hide notification close button
            };
            string reload = Services.GetAppSettings("reload");
            //string SIPUsername = ConfigurationManager.AppSettings["SIPUsername"];
            //string SIPPassword = ConfigurationManager.AppSettings["SIPPassword"];
            //string SIPServer = ConfigurationManager.AppSettings["SIPServer"];
            //string port = ConfigurationManager.AppSettings["SIPport"];
            phone = ConfigurationManager.AppSettings["SIPPhoneNumber"];
            string password = Services.GetAppSettings("SIPPassword");
            string server = Services.GetAppSettings("SIPServer");
            string username = Services.GetAppSettings("SIPUsername");
            string port = Services.GetAppSettings("SIPport");
            //id_data.Text = Services.GetAppSettings("UserData");
            manager = new ManagerConnection(server, Int32.Parse(port), username, password);
            //manager = new ManagerConnection(SIPServer, Int32.Parse(port), SIPUsername, SIPPassword);
            manager.UnhandledEvent += new ManagerEventHandler(manager_Events);
            manager.NewState += new NewStateEventHandler(Monitoring_NewState);
            manager.Hangup += Manager_Hangup;
            try
            {
                manager.Login();
                if (manager.IsConnected())
                {
                    Analytics.TrackEvent($"Login {manager.Username}, time: {DateTime.Now}");
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                Analytics.TrackEvent("Error connect\n" + ex.Message);
                manager.Logoff();
            }
            void manager_Events(object sender, ManagerEvent e)
            {
                Analytics.TrackEvent("Event : " + e.GetType().Name);
            }

            void Monitoring_NewState(object sender, NewStateEvent e)
            {

            
                try {
                    string state = e.State;
                    string callerID = e.CallerId;
                    if ((state == "Ringing") | (e.ChannelState == "5"))
                    {
                        string calleridname_inner = e.CallerIdName;
                        string calleridnumber_inner = e.CallerIdNum;
                        currentPhone = calleridnumber_inner;
                        string channelstatedesc = e.ChannelStateDesc;
                        var datereceived = e.DateReceived;
                        if (!MainBoleanValue)
                        {
                            if (phone != String.Empty && phone == calleridnumber_inner)
                            {
                                MainBoleanValue = true;
                                this.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        if (!alreadyShown)
                                        {
                                            Application.Current.Dispatcher.Invoke((Action)delegate
                                            {
                                                APIHelper.InitializeClient();
                                                string id = ConfigurationManager.AppSettings["UserData"];
                                                string phone = ConfigurationManager.AppSettings["SIPPhoneNumber"];
                                                var popupt = Task.Run(async () => await APIAccess.GetPageAsync(id_unique.ToString(), calleridnumber, id, phone)).Result;
                                                Analytics.TrackEvent($"Popup: {(int)popupt.Data.Attributes.PopupDuration}, {popupt.Data.Attributes.Url.ToString()}, {(int)popupt.Data.Attributes.PopupHeight}, {(int)popupt.Data.Attributes.PopupWidth}");

                                                popup = new Popup((int)popupt.Data.Attributes.PopupDuration, popupt.Data.Attributes.Url.ToString(), (int)popupt.Data.Attributes.PopupHeight, (int)popupt.Data.Attributes.PopupWidth);
                                                popup.Show();
                                                popup.Activate();
                                                popup.Topmost = true;
                                                alreadyShown = true;
                                           
                                            });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Crashes.TrackError(ex);
                                        Analytics.TrackEvent("Error line : " + 236.ToString());

                                    }
                                });
                            }
                            else
                            {
                                MainBoleanValue = false;
                                return;
                            }
                        }
                        else
                        {
                            Analytics.TrackEvent($"Call incoming from - {calleridnumber}, time: {DateTime.Now}");
                        }
                    }
                    else if ((state == "Ring") | (e.ChannelState == "4"))
                    {
                        calleridname = e.CallerIdName;
                        calleridnumber = e.CallerIdNum;
                        caller_model = new CallModel();
                        caller_model.caller = calleridnumber;
                        id_unique = Guid.NewGuid();
                        MainBoleanValue = false;
                    }
                    else if (e.ChannelState == "6" && MainBoleanValue && commited_guid != id_unique)
                    {
                        if (currentPhone == phone)
                        {
                            caller_model.status = "Answered";
                            caller_model.time = DateTime.Now.ToString();
                            caller_model.caller = $"{calleridnumber}-{calleridname}";
                            SqliteDataAccess.InsertCallHistory(caller_model);
                            commited_guid = id_unique;
                            answered = true;
                            MainBoleanValue = false;
                            Analytics.TrackEvent($"Answerered call from - {calleridnumber}, time: {DateTime.Now}");
                        }
                    }
                } catch(Exception ex)
                {
                    Crashes.TrackError(ex);
                    Analytics.TrackEvent("Error line : " + 278.ToString());

                }
            } 
        }

        private void Manager_Hangup(object sender, HangupEvent e)
        {
            try
            {
                if (commited_guid != id_unique && MainBoleanValue)
                {
                    if (currentPhone == phone)
                    {
                        caller_model.status = "Missed";
                        caller_model.time = DateTime.Now.ToString();
                        caller_model.caller = $"{calleridnumber}-{calleridname}";
                        SqliteDataAccess.InsertCallHistory(caller_model);
                        commited_guid = id_unique;
                        alreadyShown = false;
                        MainBoleanValue = false;
                        Analytics.TrackEvent($"Missed call from - {calleridnumber}, time: {DateTime.Now}");

                    }
                }
            }
            catch(Exception ex) {
                Crashes.TrackError(ex);
                Analytics.TrackEvent("Error line : " + 306.ToString());

            }
        }
    }
}