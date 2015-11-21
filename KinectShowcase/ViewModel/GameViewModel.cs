using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using KinectShowcase.Models.Games;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KinectShowcase.ViewModel
{
    public class GameViewModel : ViewModelBase
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private GameDataModel _model;
        private Action<GameDataModel> _onClick;

        public ICommand OnClickCommand { get; private set; }

        public GameViewModel(GameDataModel aModel, Action<GameDataModel> aOnClick)
        {
            _model = aModel;
            _onClick = aOnClick;
            OnClickCommand = new RelayCommand(wasClicked); ;
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

        private void wasClicked()
        {
            _onClick(_model);
        }
    }
}
