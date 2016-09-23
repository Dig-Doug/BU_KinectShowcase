using System;
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

namespace KinectLogin
{
    using System.Globalization;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for ResultDisplay.xaml
    /// </summary>
    public partial class ResultDisplay : UserControl
    {
        public ResultDisplay(string message)
        {
            this.InitializeComponent();
            this.messageTextBlock.Text = message;
        }

        private void OnLoadedStoryboardCompleted(object sender, System.EventArgs e)
        {
            var parent = (Panel)this.Parent;
            //removes self after display
            parent.Children.Remove(this);
        }
    }
}
