using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using KinectShowcase.Models.Gallery;
using KinectShowcaseCommon.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectShowcase.ViewModel
{
    public class GalleryDetailViewModel : ViewModelBase, IPageViewModel
    {
        private const int MAX_WIDTH_IMAGE = 1400;
        private const int MAX_HEIGHT_IMAGE = 700;
        private const int MAX_WIDTH_BLUR = 140;
        private const int MAX_HEIGHT_BLUR = 70;
        private const string DESCRIPTION_NONE = "No description available.";

        public string Name
        {
            get { return "GalleryDetailViewModel"; }
        }

        public ICommand OpenGalleryViewCommand { get; private set; }
        public ICommand PageLeftCommand { get; private set; }
        public ICommand PageRightCommand { get; private set; }

        private GalleryItem _model;
        public GalleryItem Item
        {
            get
            {
                return _model;
            }
            set
            {
                if (_model != value)
                {
                    _model = value;
                    int indexOfElement = 0;
                    foreach (GalleryItem currentItem in GalleryItemManager.Default.MediaFiles)
                    {
                        if (currentItem == _model)
                            break;
                        indexOfElement++;
                    }
                    _currentIndex = indexOfElement;
                    RaisePropertyChanged("Item");
                }
            }
        }

        public string Title
        {
            get
            {
                return _model.Title;
            }
        }

        private BitmapImage _source;
        public BitmapImage ImageSource
        {
            get
            {
                return _source;
            }
        }

        private BitmapSource _blurSource;
        public BitmapSource BlurSource
        {
            get
            {
                return _blurSource;
            }
        }

        public string Description
        {
            get
            {
                string result = FileAccessHelpers.GetContentsOfFile(_model.DescriptionPath);
                return (result == null ? DESCRIPTION_NONE : result);
            }
        }

        public Visibility PageLeftButtonVisibility
        {
            get
            {
                return (_currentIndex > 0 ? Visibility.Visible : Visibility.Hidden);
            }
        }

        public Visibility PageRightButtonVisibility
        {
            get
            {
                return (_currentIndex != GalleryItemManager.Default.MediaFiles.Count - 1 ? Visibility.Visible : Visibility.Hidden);
            }
        }

        private int _currentIndex = 0;
        private int CurrentIndex
        {
            get
            {
                return _currentIndex;
            }
            set
            {
                if (_currentIndex != value)
                {
                    _currentIndex = value;
                    if (_currentIndex < 0)
                        _currentIndex = 0;
                    if (_currentIndex > GalleryItemManager.Default.MediaFiles.Count - 1)
                        _currentIndex = GalleryItemManager.Default.MediaFiles.Count - 1;

                    Item = GalleryItemManager.Default.MediaFiles.ElementAt(_currentIndex);
                }
            }
        }

        public GalleryDetailViewModel()
        {
            PageLeftCommand = new RelayCommand(pageLeft);
            PageRightCommand = new RelayCommand(pageRight);
            OpenGalleryViewCommand = new RelayCommand(openGalleryView);
            this.PropertyChanged += GalleryDetailViewModel_PropertyChanged;
        }

        private void pageLeft()
        {
            CurrentIndex--;
        }

        private void pageRight()
        {
            CurrentIndex++;
        }

        private void openGalleryView()
        {
            IPageViewModel galleryView = ViewModelLocator.Locator().GalleryViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(galleryView));
        }

        private void GalleryDetailViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item")
            {
                _source = ImageSourceFromFile(_model.MediaPath, MAX_WIDTH_IMAGE, MAX_WIDTH_BLUR);
                //blur a lot
                //TODO try other kernels
                WriteableBitmap blur = new WriteableBitmap(ImageSourceFromFile(_model.MediaPath, MAX_WIDTH_BLUR, MAX_HEIGHT_BLUR));
                for (int i = 0; i < 10; i++ )
                    blur = blur.Convolute(WriteableBitmapExtensions.KernelGaussianBlur5x5);
                _blurSource = blur;
                RaisePropertyChanged("Title");
                RaisePropertyChanged("ImageSource");
                RaisePropertyChanged("BlurSource");
                RaisePropertyChanged("Description");
                RaisePropertyChanged("PageLeftButtonVisibility");
                RaisePropertyChanged("PageRightButtonVisibility");
            }
        }

        private static BitmapImage ImageSourceFromFile(String aFilePath, int aMaxWidth, int aMaxHeight)
        {
            //TODO blur edges to black for better looking image display
            //TODO limit image size for height as well as width
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.DecodePixelWidth = aMaxWidth;
            bitmapImage.UriSource = new Uri(aFilePath);
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
    }
}
