using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using KinectShowcase.Models.Games;
using log4net;
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
    public class GameListViewModel : ViewModelBase, IPageViewModel
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Name
        {
            get { return "GameListViewModel"; }
        }

        public ICommand PageLeftCommand { get; private set; }
        public ICommand PageRightCommand { get; private set; }
        public ICommand OpenHomeViewCommand { get; private set; }

        private ObservableCollection<GameViewModel> _gridObjects;
        public ObservableCollection<GameViewModel> GridObjects
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
                return (_currentPage != _maxPages && _maxPages > 0 ? Visibility.Visible : Visibility.Hidden);
            }
        }

        public int GameGridRowCount
        {
            get
            {
                return 2;
            }
        }

        public int GameGridColumnCount
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
                    _currentPage = value;
                    PopulateGrid();
                    RaisePropertyChanged("CurrentPageText");
                    RaisePropertyChanged("PageLeftButtonVisibility");
                    RaisePropertyChanged("PageRightButtonVisibility");
                }
            }
        }

        private int _maxPages;

        public GameListViewModel()
        {
            PageLeftCommand = new RelayCommand(pageLeft);
            PageRightCommand = new RelayCommand(pageRight);
            OpenHomeViewCommand = new RelayCommand(openHomeView);
            GridObjects = new ObservableCollection<GameViewModel>();
            GridObjects.CollectionChanged += GridObjects_CollectionChanged;

            //calculate max pages and set page to 0
            _maxPages = (int)Math.Ceiling(GamesDatabase.Default.Games.Count / (double)(GameGridRowCount * GameGridColumnCount)) - 1;
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

        private void openHomeView()
        {
            log.Info("Going home");
            IPageViewModel homeView = ViewModelLocator.Locator().HomeViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(homeView));
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
            int startIndex = _currentPage * (GameGridRowCount * GameGridColumnCount);
            int endIndex = startIndex + (GameGridRowCount * GameGridColumnCount);

            _gridObjects.Clear();
            for (int i = startIndex; i < endIndex && i < GamesDatabase.Default.Games.Count; i++)
            {
                GameDataModel game = GamesDatabase.Default.Games.ElementAt(i);
                Action<GameDataModel> onClick = GameWasClicked;
                _gridObjects.Add(new GameViewModel(game, onClick));
            }
        }

        private void GameWasClicked(GameDataModel aGame)
        {
            log.Info("Selected game: " + aGame.Title);
            GameDetailViewModel gameDetailView = ViewModelLocator.Locator().GameDetailViewModel;
            gameDetailView.Game = aGame;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(gameDetailView));
        }
    }
}
