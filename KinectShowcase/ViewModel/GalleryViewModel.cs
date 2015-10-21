using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using KinectShowcase.Models.Gallery;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace KinectShowcase.ViewModel
{
    public class GalleryViewModel : ViewModelBase, IPageViewModel
    {
        public string Name
        {
            get { return "GalleryViewModel"; }
        }

        public ICommand PageLeftCommand { get; private set; }
        public ICommand PageRightCommand { get; private set; }
        public ICommand OpenHomeViewCommand { get; private set; }

        private ObservableCollection<GalleryItemViewModel> _gridObjects;
        public ObservableCollection<GalleryItemViewModel> GridObjects
        {
            get
            {
                return _gridObjects;
            }
            set
            {
                if (_gridObjects != value)
                {
                    _gridObjects = value;
                    RaisePropertyChanged("GridObjects");
                }
            }
        }

        public string CurrentFolderName
        {
            get
            {
                return GalleryItemManager.Default.CurrentFolder();
            }
        }

        public string CurrentPageText
        {
            get
            {
                return "Pages: " + (_currentPage + 1) + " / " + (_maxPages + 1);
            }
        }

        public Visibility PageLeftButtonVisibility
        {
            get
            {
                return (_currentPage > 0 ? Visibility.Visible : Visibility.Hidden);
            }
        }

        public Visibility PageRightButtonVisibility
        {
            get
            {
                return (_currentPage != _maxPages ? Visibility.Visible : Visibility.Hidden);
            }
        }

        public int GalleryGridRowCount
        {
            get
            {
                return 2;
            }
        }

        public int GalleryGridColumnCount
        {
            get
            {
                return 3;
            }
        }

        private int _currentPage = -1;
        private int CurrentPage
        {
            get
            {
                return _currentPage;
            }
            set
            {
                if (_currentPage != value)
                {
                    _maxPages = (int)Math.Ceiling(GalleryItemManager.Default.Items.Count / (double)(GalleryGridRowCount * GalleryGridColumnCount)) - 1;
                    _currentPage = value;
                    PopulateGrid();
                    RaisePropertyChanged("CurrentPageText");
                    RaisePropertyChanged("PageLeftButtonVisibility");
                    RaisePropertyChanged("PageRightButtonVisibility");
                    RaisePropertyChanged("CurrentFolderName");
                }
            }
        }

        private int _maxPages;

        public GalleryViewModel()
        {
            PageLeftCommand = new RelayCommand(pageLeft);
            PageRightCommand = new RelayCommand(pageRight);
            OpenHomeViewCommand = new RelayCommand(closeButtonClicked);
            GridObjects = new ObservableCollection<GalleryItemViewModel>();
            GridObjects.CollectionChanged += GridObjects_CollectionChanged;

            //calculate max pages and set page to 0
            CurrentPage = 0;
        }

        private void pageLeft()
        {
            ScrollPage(-1);
        }

        private void pageRight()
        {
            ScrollPage(1);
        }

        private void closeButtonClicked()
        {
            if (GalleryItemManager.Default.CanGoBack())
            {
                GalleryItemManager.Default.GoBack();
                _currentPage = -1;
                CurrentPage = 0;
            }
            else
            {
                IPageViewModel homeView = ViewModelLocator.Locator().HomeViewModel;
                MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(homeView));
            }
        }

        private void GridObjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged("GridObjects");
        }

         private void ScrollPage(int aIncrement)
        {
            int newPage = _currentPage + aIncrement;

            if (newPage < 0)
            {
                newPage = 0;
            }
            else if (newPage > _maxPages)
            {
                newPage = _maxPages;
            }

            CurrentPage = newPage;
        }

        private void PopulateGrid()
        {
            int startIndex = _currentPage * (GalleryGridRowCount * GalleryGridColumnCount);
            int endIndex = startIndex + (GalleryGridRowCount * GalleryGridColumnCount);

            _gridObjects.Clear();
            for (int i = startIndex; i < endIndex && i < GalleryItemManager.Default.Items.Count; i++)
            {
                GalleryItem item = GalleryItemManager.Default.Items.ElementAt(i);
                Action<GalleryItem> onClick = GalleryItemWasClicked;
                _gridObjects.Add(new GalleryItemViewModel(item, onClick));
            }
        }

        private void GalleryItemWasClicked(GalleryItem aItem)
        {
            if (aItem != null)
            {
                Debug.WriteLine("GalleryItemClicked: " + aItem.Title);
                if (aItem.Type == GalleryItem.GalleryItemType.GalleryMediaTypeFolder)
                {
                    GalleryItemManager.Default.GoInFolder(aItem);
                    _currentPage = -1;
                    CurrentPage = 0;
                }
                else
                {
                    GalleryDetailViewModel galleryDetailView = ViewModelLocator.Locator().GalleryDetailViewModel;
                    galleryDetailView.Item = aItem;
                    MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(galleryDetailView));
                }
            }
        }
    }
}