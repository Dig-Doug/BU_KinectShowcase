﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using KinectShowcase.Models.Gallery;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectShowcase.ViewModel
{
    public class GalleryItemViewModel : ViewModelBase
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const int MAX_TITLE_LENGTH = 16;
        private const string MAX_TITLE_END = "...";
        private const int THUMBNAIL_SIZE = 200;

        private GalleryItem _model;

        private Action<GalleryItem> _onClick;

        public ICommand OnClickCommand { get; private set; }

        public string Name
        {
            get
            {
                return (_model.Title.Length > MAX_TITLE_LENGTH ? _model.Title.Substring(0, MAX_TITLE_LENGTH - MAX_TITLE_END.Length) + MAX_TITLE_END : _model.Title);
            }
        }
        public string Description
        {
            get
            {
                return _model.DescriptionPath;
            }
        }

        private ImageSource _source;

        public ImageSource ImageSource
        {
            get
            {
                return _source;
            }
        }

        public GalleryItemViewModel(GalleryItem aModel, Action<GalleryItem> aOnClick)
        {
            _model = aModel;
            _onClick = aOnClick;
            OnClickCommand = new RelayCommand(wasClicked);
            _source = ImageSourceFromFile(_model.MediaPath);
        }

        private static ImageSource ImageSourceFromFile(String aFilePath)
        {
            BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.DecodePixelWidth = THUMBNAIL_SIZE;
                bitmapImage.UriSource = new Uri(aFilePath);
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            return bitmapImage;
        }

        private void wasClicked()
        {
            _onClick(_model);
        }
    }
}
