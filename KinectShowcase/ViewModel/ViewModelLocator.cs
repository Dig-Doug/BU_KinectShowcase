/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:KinectShowcase"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, ParentFolderPath=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace KinectShowcase.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        public static ViewModelLocator Locator()
        {
            return App.Current.Resources["Locator"] as ViewModelLocator;
        }

        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}

            SimpleIoc.Default.Register<ApplicationViewModel>();
            SimpleIoc.Default.Register<HomeViewModel>();
            SimpleIoc.Default.Register<GameListViewModel>();
            SimpleIoc.Default.Register<GameDetailViewModel>();
            SimpleIoc.Default.Register<GalleryViewModel>();
            SimpleIoc.Default.Register<GalleryDetailViewModel>();
            SimpleIoc.Default.Register<BrowserViewModel>();
        }

        public ApplicationViewModel ApplicationViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ApplicationViewModel>();
            }
        }

        public HomeViewModel HomeViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<HomeViewModel>();
            }
        }

        public GameListViewModel GameListViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<GameListViewModel>();
            }
        }

        public GameDetailViewModel GameDetailViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<GameDetailViewModel>();
            }
        }

        public GalleryViewModel GalleryViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<GalleryViewModel>();
            }
        }

        public GalleryDetailViewModel GalleryDetailViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<GalleryDetailViewModel>();
            }
        }

        public BrowserViewModel BrowserViewModel
        {
            get
            {
                return ServiceLocator.Current.GetInstance<BrowserViewModel>();
            }
        }
        
        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}