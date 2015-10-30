using CefSharp;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using KinectShowcaseCommon.ProcessHandling;
using KinectShowcaseCommon.UI_Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace KinectShowcase.ViewModel
{
    public class ApplicationViewModel : ViewModelBase, ISystemTimeoutListener
    {
        private IPageViewModel _currentPageViewModel;
        private List<IPageViewModel> _pageViewModels;

        public SkeletonView SkeletonView { get; set; }

        public class ChangePageMessage
        {
            public IPageViewModel ViewModel { get; private set; }
            public ChangePageMessage(IPageViewModel aModel)
            {
                ViewModel = aModel;
            }
        }

        public ApplicationViewModel()
        {
            //Register for change view model messages
            MessengerInstance.Register<ChangePageMessage>(this, ChangeViewModelMessageRecieved);

            // Add available pages
            PageViewModels.Add(ViewModelLocator.Locator().HomeViewModel);

            // Set starting page
            CurrentPageViewModel = PageViewModels[0];

            //Start the Application Watchdog
            SystemWatchdog.Default.NavigationHandler = this;

            //Set user agent
            CefSettings settings = new CefSettings();
            settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 5_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9B176 Safari/7534.48.3";
            Cef.Initialize(settings);
        }

        #region Properties / Commands

        public List<IPageViewModel> PageViewModels
        {
            get
            {
                if (_pageViewModels == null)
                    _pageViewModels = new List<IPageViewModel>();

                return _pageViewModels;
            }
        }

        public IPageViewModel CurrentPageViewModel
        {
            get
            {
                return _currentPageViewModel;
            }
            set
            {
                if (_currentPageViewModel != value)
                {
                    _currentPageViewModel = value;
                    RaisePropertyChanged("CurrentPageViewModel");
                }
            }
        }

        #endregion

        #region Methods

        private void ChangeViewModel(IPageViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
                PageViewModels.Add(viewModel);

            CurrentPageViewModel = PageViewModels.FirstOrDefault(vm => vm == viewModel);
        }

        private void ChangeViewModelMessageRecieved(ChangePageMessage aMessage)
        {
            ChangeViewModel(aMessage.ViewModel);
        }

        #endregion

        public void Reset()
        {
            CurrentPageViewModel = PageViewModels[0];
        }
    }
}