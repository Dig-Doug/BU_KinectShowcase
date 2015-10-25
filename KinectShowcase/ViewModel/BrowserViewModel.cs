using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace KinectShowcase.ViewModel
{
    public class BrowserViewModel : ViewModelBase, IPageViewModel
    {
        private const string USER_AGENT = "User-Agent: Mobile \r\n";

        public string Name
        {
            get { return "BrowserViewModel"; }
        }

        public ICommand PageForwardCommand { get; private set; }
        public ICommand PageBackCommand { get; private set; }
        public ICommand OpenHomeViewCommand { get; private set; }

        /*
        public Visibility PageForwardButtonVisibility
        {
            get
            {
                return (_currentPage > 0 ? Visibility.Visible : Visibility.Hidden);
            }
        }

        public Visibility PageBackButtonVisibility
        {
            get
            {
                return (_currentPage != _maxPages ? Visibility.Visible : Visibility.Hidden);
            }
        }
         * */

        public BrowserViewModel()
        {
            PageForwardCommand = new RelayCommand(pageForward);
            PageBackCommand = new RelayCommand(pageBack);
            OpenHomeViewCommand = new RelayCommand(openHomeView);
        }

        private void pageForward()
        {
            Debug.WriteLine("TODO - Browser");
        }

        private void pageBack()
        {
            
        }

        private void openHomeView()
        {
            IPageViewModel homeView = ViewModelLocator.Locator().HomeViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(homeView));
        }
    }
}
