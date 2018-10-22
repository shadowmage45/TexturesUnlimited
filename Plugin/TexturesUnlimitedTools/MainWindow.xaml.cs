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

namespace TexturesUnlimitedTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public TextureCombineWindow window { get { return null; } }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextureCombineClick(object sender, RoutedEventArgs e)
        {
            TextureCombineWindow window = new TextureCombineWindow();
            window.ShowDialog();            
        }

        private void TextureConvertClick(object sender, RoutedEventArgs e)
        {

        }

        private void ConfigurationClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
