using CefSharp.Wpf;
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

        private ChromiumWebBrowser _webBrowser;
        public ChromiumWebBrowser WebBrowser
        {
            get
            {
                return _webBrowser;
            }
            set
            {
                if (_webBrowser != value)
                {
                    if (_webBrowser != null)
                    {
                        _webBrowser.MouseMove -= _webBrowser_MouseMove;
                    }

                    _webBrowser = value;

                    if (_webBrowser != null)
                    {
                        _webBrowser.MouseMove +=_webBrowser_MouseMove;
                    }

                    RaisePropertyChanged("PageForwardButtonVisibility");
                    RaisePropertyChanged("PageBackButtonVisibility");
                }
            }
        }

        public Visibility PageForwardButtonVisibility
        {
            get
            {
                if (this.WebBrowser != null)
                    return (WebBrowser.CanGoForward ? Visibility.Visible : Visibility.Hidden);
                else
                    return Visibility.Hidden;
            }
        }

        public Visibility PageBackButtonVisibility
        {
            get
            {
                if (this.WebBrowser != null)
                    return (WebBrowser.CanGoBack ? Visibility.Visible : Visibility.Hidden);
                else
                    return Visibility.Hidden;
            }
        }

        public BrowserViewModel()
        {
            PageForwardCommand = new RelayCommand(pageForward);
            PageBackCommand = new RelayCommand(pageBack);
            OpenHomeViewCommand = new RelayCommand(openHomeView);
        }

        private void pageForward()
        {
            if (WebBrowser.CanGoForward)
                WebBrowser.GetBrowser().GoForward();
        }

        private void pageBack()
        {
            if (WebBrowser.CanGoBack)
                WebBrowser.GetBrowser().GoBack();
        }

        private void _webBrowser_MouseMove(object sender, MouseEventArgs e)
        {
            RaisePropertyChanged("PageForwardButtonVisibility");
            RaisePropertyChanged("PageBackButtonVisibility");
        }

        private void openHomeView()
        {
            IPageViewModel homeView = ViewModelLocator.Locator().HomeViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(homeView));
        }
    }
}
