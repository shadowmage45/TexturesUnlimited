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

namespace TexturesUnlimitedTools
{
    /// <summary>
    /// Interaction logic for TexureSelectionWindow.xaml
    /// </summary>
    public partial class TextureSelectionWindow : Window
    {

        private List<ChannelSelection> channelOptions = new List<ChannelSelection>();
        public TextureSelectionWindow()
        {
            channelOptions.Add(ChannelSelection.R);
            channelOptions.Add(ChannelSelection.G);
            channelOptions.Add(ChannelSelection.B);
            channelOptions.Add(ChannelSelection.A);
            channelOptions.Add(ChannelSelection.RGB);
            InitializeComponent();
            DiffuseChannelComboBox.ItemsSource = channelOptions;
            DiffuseChannelComboBox.SelectedIndex = 4;

            AuxChannelComboBox.ItemsSource = channelOptions;
            AuxChannelComboBox.SelectedIndex = 0;

            SmoothChannelComboBox.ItemsSource = channelOptions;
            SmoothChannelComboBox.SelectedIndex = 3;
        }
        
        private void ProcessClick(object sender, RoutedEventArgs e)
        {
            this.Close();
            TextureMaskingWindow window = new TextureMaskingWindow();

            //set file paths / fully loaded bitmaps
            window.ShowDialog();
        }

        private void SelectBaseMapClik(object sender, RoutedEventArgs e)
        {
            string img1 = ImageTools.openFileSelectDialog("Select a PNG base image");
            DiffFileBox.Text = img1;
            //diffuseMap = new DirectBitmap(new Bitmap(System.Drawing.Image.FromFile(img1)));
            //diffuseImage = ImageTools.BitmapToBitmapImage(diffuseMap.Bitmap);
        }

        private void SelectSpecClick(object sender, RoutedEventArgs e)
        {
            string img2 = ImageTools.openFileSelectDialog("Select a PNG base image");
            SpecFileBox.Text = img2;
            //auxMap = new DirectBitmap(new Bitmap(System.Drawing.Image.FromFile(img2)));
            //auxImage = ImageTools.BitmapToBitmapImage(auxMap.Bitmap);
        }

        private void SelectSmoothClick(object sender, RoutedEventArgs e)
        {
            string img3 = ImageTools.openFileSelectDialog("Select a PNG base image");
            SmoothFileBox.Text = img3;
            //smoothMap = new DirectBitmap(new Bitmap(System.Drawing.Image.FromFile(img3)));
            //smoothImage = ImageTools.BitmapToBitmapImage(smoothMap.Bitmap);
        }

        private void SelectMaskClick(object sender, RoutedEventArgs e)
        {
            string img4 = ImageTools.openFileSelectDialog("Select a PNG mask image");
            MaskFileBox.Text = img4;
            //maskMap = new DirectBitmap(new Bitmap(System.Drawing.Image.FromFile(img4)));
            //maskImage = ImageTools.BitmapToBitmapImage(maskMap.Bitmap);
        }

    }
}
