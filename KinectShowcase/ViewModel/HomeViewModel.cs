using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using KinectShowcaseCommon.Kinect_Processing;
using log4net;
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
    public class HomeViewModel : ViewModelBase, IPageViewModel, KinectManager.ActivelyTrackingListener
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Name
        {
            get { return "HomeViewModel"; }
        }

        public ICommand OpenGameListView { get; private set; }
        public ICommand OpenGalleryView { get; private set; }
        public ICommand OpenBrowserView { get; private set; }
        public ICommand OpenAuthorView { get; private set; }

        private bool _isInteracting = false;
        public Visibility ControlVisibility
        {
            get
            {
                return (_isInteracting ? Visibility.Visible : Visibility.Hidden);
            }
        }

        public HomeViewModel()
        {
            OpenGameListView = new RelayCommand(openGameListView);
            OpenGalleryView = new RelayCommand(openGalleryView);
            OpenBrowserView = new RelayCommand(openBrowserView);
            OpenAuthorView = new RelayCommand(openAuthorView);
            KinectManager.Default.AddTrackingListener(this);
        }

        ~HomeViewModel()
        {
            if (KinectManager.Default != null)
            {
                KinectManager.Default.RemoveTrackingListener(this);
            }
        }

        private void openGameListView()
        {
            log.Info("Opening game view");
            IPageViewModel gameListView = ViewModelLocator.Locator().GameListViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(gameListView));
        }

        private void openGalleryView()
        {
            log.Info("Opening gallery view");
            IPageViewModel galleryView = ViewModelLocator.Locator().GalleryViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(galleryView));
        }

        private void openBrowserView()
        {
            log.Info("Opening browser view");
            IPageViewModel browserView = ViewModelLocator.Locator().BrowserViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(browserView));
        }

        private void openAuthorView()
        {
            log.Info("Opening author view");
            IPageViewModel authorView = ViewModelLocator.Locator().AuthorViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(authorView));
        }

        public void KinectManagerDidBeginTracking(KinectManager aManager)
        {
            log.Info("Kinect did begin tracking");
            _isInteracting = true;
            RaisePropertyChanged("ControlVisibility");

            //System.Diagnostics.Process.Start(@"C:\KinectShowcase\scripts\toggle_numlock.vbs");
        }

        public void KinectManagerDidFinishTracking(KinectManager aManager)
        {
            _isInteracting = false;
            RaisePropertyChanged("ControlVisibility");
        }
    }
}
