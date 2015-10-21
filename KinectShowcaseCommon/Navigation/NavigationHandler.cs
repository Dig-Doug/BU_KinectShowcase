using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.Navigation
{
    public class NavigationHandler
    {
        #region NavigationPage

        public interface NavigationPage
        {
            void PageDidAppear();
            void PageDidDisappear();
            void PagePop();
        }

        #endregion

        private static volatile NavigationHandler instance;
        private static object syncRoot = new Object();

        private List<NavigationPage> navigationPages = new List<NavigationPage>();

        public static NavigationHandler Default
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new NavigationHandler();
                    }
                }

                return instance;
            }
        }

        private NavigationHandler()
        {

        }

        #region Listener Handling

        public void NavigationPageDidLoad(NavigationPage aListener)
        {
            if (!this.navigationPages.Contains(aListener))
            {
                if (this.navigationPages.Count > 0)
                    this.navigationPages.Last().PageDidDisappear();
                this.navigationPages.Add(aListener);
                this.navigationPages.Last().PageDidAppear();
            }
        }

        public void NavigationPageDidUnload(NavigationPage aListener)
        {
            if (this.navigationPages.Contains(aListener))
            {
                this.navigationPages.Last().PageDidDisappear();
                this.navigationPages.Remove(aListener);
                if (this.navigationPages.Count > 0)
                    this.navigationPages.Last().PageDidAppear();
            }
        }

        #endregion

        #region Navigation Management

        public void PopTopPage()
        {
            if (this.navigationPages.Count > 1)
            {
                NavigationPage currentPage = this.navigationPages.Last();
                currentPage.PagePop();
            }
        }

        public void GoToRootPage()
        {
            while (this.navigationPages.Count > 1)
            {
                NavigationPage currentPage = this.navigationPages.Last();
                currentPage.PagePop();
            }
        }

        #endregion
    }
}
