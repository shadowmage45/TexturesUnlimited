using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for TextureCombineWindow.xaml
    /// </summary>
    public partial class TextureCombineWindow : Window
    {
        
        private ObservableCollection<ImageChannelSelection> imageOptionsRaw = new ObservableCollection<ImageChannelSelection>();
        public ObservableCollection<ImageChannelSelection> imageOptions { get { return imageOptionsRaw; } }

        private ObservableCollection<TextureRemapEntry> recordsRaw = new ObservableCollection<TextureRemapEntry>();
        public ObservableCollection<TextureRemapEntry> records { get { return recordsRaw; } }


        public TextureCombineWindow()
        {
            imageOptionsRaw.Add(ImageChannelSelection.Image1_R);
            imageOptionsRaw.Add(ImageChannelSelection.Image1_G);
            imageOptionsRaw.Add(ImageChannelSelection.Image1_B);
            imageOptionsRaw.Add(ImageChannelSelection.Image1_A);
            imageOptionsRaw.Add(ImageChannelSelection.Image1_RGB);
            imageOptionsRaw.Add(ImageChannelSelection.Image2_R);
            imageOptionsRaw.Add(ImageChannelSelection.Image2_B);
            imageOptionsRaw.Add(ImageChannelSelection.Image2_G);
            imageOptionsRaw.Add(ImageChannelSelection.Image2_A);
            imageOptionsRaw.Add(ImageChannelSelection.Image2_RGB);
            InitializeComponent();

            DataGridComboBoxColumn column;

            column = RecordGrid.Columns[3] as DataGridComboBoxColumn;
            column.ItemsSource = imageOptions;
            column.SelectedItemBinding = new Binding("ImageR");
            
            column = RecordGrid.Columns[4] as DataGridComboBoxColumn;
            column.ItemsSource = imageOptions;
            column.SelectedItemBinding = new Binding("ImageG");
            
            column = RecordGrid.Columns[5] as DataGridComboBoxColumn;
            column.ItemsSource = imageOptions;
            column.SelectedItemBinding = new Binding("ImageB");
            
            column = RecordGrid.Columns[6] as DataGridComboBoxColumn;
            column.ItemsSource = imageOptions;
            column.SelectedItemBinding = new Binding("ImageA");
            
        }

        private void SelectImagesClick(object sender, RoutedEventArgs e)
        {
            string img1 = openFileSelectDialog("Primary Texture");
            string img2 = openFileSelectDialog("Second Texture");
            string img3 = openFileSaveDialog("Output Texture");
            TextureRemapEntry entry = new TextureRemapEntry();
            entry.Image1Name = img1;
            entry.Image2Name = img2;
            entry.OutputName = img3;
            records.Add(entry);
        }

        private void ConvertImagesClick(object sender, RoutedEventArgs e)
        {

        }

        private static string openFileSelectDialog(string title)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = title;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Image Files|*.png;*.dds;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff";
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            dialog.ShowDialog();
            return dialog.FileName;
        }

        private static string openFileSaveDialog(string title)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = title;
            dialog.DefaultExt = ".png";
            dialog.Filter = "PNG Files|*.png";
            dialog.CheckFileExists = false;
            dialog.ShowDialog();
            return dialog.FileName;
        }

        public enum ImageChannelSelection
        {
            Image1_R,
            Image1_B,
            Image1_G,
            Image1_A,
            Image1_RGB,
            Image2_R,
            Image2_G,
            Image2_B,
            Image2_A,
            Image2_RGB
        }

        public class TextureRemapEntry : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            string image1Name = "";
            string image2Name = "";
            string outputName = "";

            ImageChannelSelection imageR = ImageChannelSelection.Image1_R;
            ImageChannelSelection imageG = ImageChannelSelection.Image1_B;
            ImageChannelSelection imageB = ImageChannelSelection.Image1_G;
            ImageChannelSelection imageA = ImageChannelSelection.Image2_RGB;

            public ImageChannelSelection ImageR { get { return imageR; } set { imageR = value; propChanged(); } }
            public ImageChannelSelection ImageG { get { return imageG; } set { imageG = value; propChanged(); } }
            public ImageChannelSelection ImageB { get { return imageB; } set { imageB = value; propChanged(); } }
            public ImageChannelSelection ImageA { get { return imageA; } set { imageA = value; propChanged(); } }

            public string Image1Name { get { return image1Name; } set { image1Name = value; propChanged(); } }
            public string Image2Name { get { return image2Name; } set { image2Name = value; propChanged(); } }
            public string OutputName { get { return outputName; } set { outputName = value; propChanged(); } }
            
            public TextureRemapEntry()
            {
            }

            private void propChanged([CallerMemberName]string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

        }

    }
}
