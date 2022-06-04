using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Windows.Themes;
using Squirrel;

namespace ShinyCall
{
    /// <summary>
    /// Interaction logic for Interface.xaml
    /// </summary>
    public partial class Interface : Window
    {

        // Prep stuff needed to remove close button on window.
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        private UpdateManager manager;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // Asterix.

        void ToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Code to remove close box from window
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);


            // Code to put the windows to the bottom right.
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }

        /// <summary>
        /// Above part of the code is there in order to remove the close button.
        /// </summary>

        public Interface()
        {
            InitializeComponent();
            Loaded += ToolWindow_Loaded;
            var theme = Services.Services.GetTheme();
            SetUpLookAndFeel(theme);
            Loaded += Interface_Loaded;
            
        }

        private async void Interface_Loaded(object sender, RoutedEventArgs e)
        {
            manager = await UpdateManager
                 .GitHubUpdateManager("@https://github.com/CodingByDay/shiny-call.git");
            version.Text = $"Shiny Call {manager.CurrentlyInstalledVersion().ToString()}";
        }

      
        private void SetUpLookAndFeel(string theme)
        {
         
        }

   

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            var state = this.WindowState;

            if (state == WindowState.Minimized)
            {
                this.Opacity = 0;
            } else
            {
                Opacity = 1;
            }

        }
    }
}
