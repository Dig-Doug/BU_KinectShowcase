using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using KinectShowcaseCommon.Helpers;
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
    public class AuthorViewModel : ViewModelBase, IPageViewModel
    {
        public string Name
        {
            get { return "AuthorViewModel"; }
        }

        public ICommand OpenHomeViewCommand { get; private set; }

        private string _description;
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    RaisePropertyChanged("Description");
                }
            }
        }

        public AuthorViewModel()
        {
            OpenHomeViewCommand = new RelayCommand(closeButtonClicked);

            this.Description = FileAccessHelpers.GetContentsOfFile("C:\\KinectShowcase\\author.txt");
        }

        private void closeButtonClicked()
        {
            IPageViewModel homeView = ViewModelLocator.Locator().HomeViewModel;
            MessengerInstance.Send<ApplicationViewModel.ChangePageMessage>(new ApplicationViewModel.ChangePageMessage(homeView));
        }
    }
}
