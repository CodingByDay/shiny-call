using System;
using System.Collections.Generic;
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
            server.Text = Properties.Settings.Default.server;
            phone_number.Text = Properties.Settings.Default.phone;
        }

        private void SaveData()
        {
            string phone_number_data = phone_number.Text;
            string server_data = server.Text;

            if (IsValid(phone_number_data, "phone") && IsValid(server_data, "server"))
            {
                MessageBox.Show("Uspešno spremenjeni podatki.");
                Properties.Settings.Default.server = server_data;
                Properties.Settings.Default.phone = phone_number_data;
                Properties.Settings.Default.Save();

            } else
            {
                MessageBox.Show("Napaka v podatkih.");
            }
        }

        private bool IsValid(string data, string type_data)
        {
            string pattern = string.Empty;
            bool isValid = false;
            switch(type_data)
            {
                case "phone":
                    pattern = @"^(([0-9]{3})[ \-\/]?([0-9]{3})[ \-\/]?([0-9]{3}))|([0-9]{9})|([\+]?([0-9]{3})[ \-\/]?([0-9]{2})[ \-\/]?([0-9]{3})[ \-\/]?([0-9]{3}))$";
                    // Create a Regex  
                    if(Regex.IsMatch(data, pattern)) {
                        isValid = true;
                    } else
                    {
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
