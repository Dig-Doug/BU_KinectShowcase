using CefSharp;
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
    public class BrowserViewModel : ViewModelBase, IPageViewModel, CefSharp.IRequestHandler
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
                        _webBrowser.RequestHandler = null;
                    }

                    _webBrowser = value;

                    if (_webBrowser != null)
                    {
                        _webBrowser.RequestHandler = this;
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

            UpdateAfterDelay();
        }

        private void pageBack()
        {
            if (WebBrowser.CanGoBack)
                WebBrowser.GetBrowser().GoBack();

            UpdateAfterDelay();
        }

        private void UpdateAfterDelay()
        {
            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer((obj) =>
            {
                RaisePropertyChanged("PageForwardButtonVisibility");
                RaisePropertyChanged("PageBackButtonVisibility");
                timer.Dispose();
            },
                        null, 1000, System.Threading.Timeout.Infinite);
        }

        private void openHomeView()
        {
            IPageViewModel homeView = ViewModelLocator.Locator().HomeViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(homeView));
        }

        #region CefSharp.IRequestHandler

        public bool OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool isRedirect)
        {
            return false;
        }

        public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
        {
            return false;
        }

        public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
        {
            return true;
        }

        public void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
        {

        }

        public CefReturnValue OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            return CefReturnValue.Continue;
        }

        public bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            return true;
        }

        public bool OnBeforePluginLoad(IWebBrowser browserControl, IBrowser browser, string url, string policyUrl, WebPluginInfo info)
        {
            return false;
        }

        public void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
        {

        }

        public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
        {
            return true;
        }

        public void OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, ref string newUrl)
        {
            UpdateAfterDelay();
        }

        public bool OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url)
        {
            return false;
        }

        public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
        {
        }

        #endregion
    }
}
