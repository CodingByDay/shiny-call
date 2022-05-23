using ShinyCall.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShinyCall.MVVM.ViewModel
{
    internal class MainViewModel: ObservableObject
    {
        public HomeViewModel HomeVm { get; set; }
        public object _currentView { get; set; }


        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value;
                OnPropertyChanged();
            }
        }


        public MainViewModel()
        {
            HomeVm = new HomeViewModel();
            CurrentView = HomeVm;
        }
    }
}
