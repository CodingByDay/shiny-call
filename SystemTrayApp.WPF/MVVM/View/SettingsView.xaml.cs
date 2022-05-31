using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShinyCall.MVVM.View
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            InitializeView();
        }

        private void InitializeView()
        {

            password.Text = Services.Services.GetAppSettings("SIPPassword");
            server.Text = Services.Services.GetAppSettings("SIPServer");
            display_name.Text = Services.Services.GetAppSettings("SIPUsername");
            phone_number.Text = Services.Services.GetAppSettings("SIPPhoneNumber");

        }

        private void SaveData()
        {
            string phone_number_data = phone_number.Text;
            string server_data = server.Text;
            string password_data = password.Text;
            string port_data = port.Text;
            string display_data = display_name.Text;

            if (IsValid(phone_number_data, "phone") && IsValid(server_data, "server"))
            {
                MessageBox.Show("Uspešno spremenjeni podatki.");
                Services.Services.AddUpdateAppSettings("SIPUsername", display_data);
                Services.Services.AddUpdateAppSettings("SIPServer", server_data);
                Services.Services.AddUpdateAppSettings("SIPPassword", password_data);
                Services.Services.AddUpdateAppSettings("SIPPhoneNumber", phone_number_data);

                // TODO: RESTART
                var currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(currentExecutablePath);
                Application.Current.Shutdown();

            } else
            {
                MessageBox.Show("Napaka v podatkih.");
            }
        }
        public void Restart()
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }
        private bool IsValid(string data, string type_data)
        {
            string pattern = string.Empty;
            bool isValid = false;
            switch(type_data)
            {
                case "phone":

                    try
                    {
                        int correct = Int32.Parse(data);
                        isValid = true;
                    }
                    catch (Exception) {
                        isValid = false;
                    }

                    break;
                  
                    
                    
                case "server":

                        if (Services.Services.IsMachineUp(data))
                        {
                            isValid = true;
                        } else
                        {
                            isValid=false;
                        }

                    break;

            }

            return isValid; 
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            SaveData();

        }

    
    }
}
