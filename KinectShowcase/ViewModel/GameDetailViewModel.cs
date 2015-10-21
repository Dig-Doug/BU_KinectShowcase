using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using KinectShowcase.Models.Games;
using KinectShowcaseCommon.Helpers;
using KinectShowcaseCommon.ProcessHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KinectShowcase.ViewModel
{
    public class GameDetailViewModel : ViewModelBase, IPageViewModel
    {
        private const string DESCRIPTION_NONE = "No description available.";

        public string Name
        {
            get { return "GameDetailViewModel"; }
        }

        public ICommand PlayGameCommand { get; private set; }
        public ICommand OpenGameListViewCommand { get; private set; }

        private GameDataModel _model;
        public GameDataModel Game
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
                    RaisePropertyChanged("Game");
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

        public string IconPath
        {
            get
            {
                return _model.RootPath + "/" + _model.IconPath;
            }
        }

        public string ScreenshotPath
        {
            get
            {
                return _model.RootPath + "/" + _model.ScreenshotPath;
            }
        }
        public string Description
        {
            get
            {
                string result = FileAccessHelpers.GetContentsOfFile(_model.RootPath + "\\" + _model.DescriptionPath);
                return (result == null ? DESCRIPTION_NONE : result);
            }
        }


        public GameDetailViewModel()
        {
            PlayGameCommand = new RelayCommand(playGame);
            OpenGameListViewCommand = new RelayCommand(openGameListView);
            this.PropertyChanged += GameDetailViewModel_PropertyChanged;
        }

        private void playGame()
        {
            SystemWatchdog.Default.StartChildProcess(_model.RootPath + "\\" + _model.ExecutablePath);
        }

        private void openGameListView()
        {
            IPageViewModel gameListView = ViewModelLocator.Locator().GameListViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(gameListView));
        }

        private void GameDetailViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Game")
            {
                RaisePropertyChanged("Title");
                RaisePropertyChanged("MediaPath");
                RaisePropertyChanged("ScreenshotPath");
                RaisePropertyChanged("DescriptionPath");
            }
        }
    }
}
