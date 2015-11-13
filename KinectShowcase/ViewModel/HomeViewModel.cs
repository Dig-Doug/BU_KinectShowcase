﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using KinectShowcaseCommon.Kinect_Processing;
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
        public string Name
        {
            get { return "HomeViewModel"; }
        }

        public ICommand OpenGameListView { get; private set; }
        public ICommand OpenGalleryView { get; private set; }
        public ICommand OpenBrowserView { get; private set; }

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
            IPageViewModel gameListView = ViewModelLocator.Locator().GameListViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(gameListView));
        }

        private void openGalleryView()
        {
            IPageViewModel galleryView = ViewModelLocator.Locator().GalleryViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(galleryView));
        }

        private void openBrowserView()
        {
            IPageViewModel browserView = ViewModelLocator.Locator().BrowserViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(browserView));
        }

        public void KinectManagerDidBeginTracking(KinectManager aManager)
        {
            _isInteracting = true;
            RaisePropertyChanged("ControlVisibility");

            //set the background to blur
            ViewModelLocator.Locator().ApplicationViewModel.SkeletonView.SetPercents(0.0f, 0.25f);
        }

        public void KinectManagerDidFinishTracking(KinectManager aManager)
        {
            _isInteracting = false;
            RaisePropertyChanged("ControlVisibility");

            //set the background to blur
            ViewModelLocator.Locator().ApplicationViewModel.SkeletonView.SetPercents(0.0f, 0.0f);
        }
    }
}
