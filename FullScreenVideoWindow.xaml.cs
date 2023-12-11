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
using System.Windows.Shapes;

namespace MP42MP3
{
    /// <summary>
    /// Interaction logic for FullScreenVideoWindow.xaml
    /// </summary>
    public partial class FullscreenVideoWindow : Window
    {
        public FullscreenVideoWindow()
        {
            InitializeComponent();
            this.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }


}
