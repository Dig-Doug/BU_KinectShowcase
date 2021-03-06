﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KinectShowcase.ViewModel;

namespace KinectShowcase.Views
{
    /// <summary>
    /// Interaction logic for GameDetailView.xaml
    /// </summary>
    public partial class GameDetailView : UserControl
    {
        public GameDetailView()
        {
            InitializeComponent();

            //set the background to blur
            ViewModelLocator.Locator().ApplicationViewModel.SkeletonView.SetPercents(0.0f, 0.0f);
            ViewModelLocator.Locator().ApplicationViewModel.SkeletonView.ChangeRandomly = false;
            ViewModelLocator.Locator().ApplicationViewModel.SkeletonView.SetMode(KinectShowcaseCommon.UI_Elements.LiveBackground.BackgroundMode.BodyIndex);
        }
    }
}
