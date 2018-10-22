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

        private ObservableCollection<ChannelSelection> channelOptionsRaw = new ObservableCollection<ChannelSelection>();
        public ObservableCollection<ChannelSelection> channelOptions { get { return channelOptionsRaw; } }
        
        private ObservableCollection<ImageSelection> imageOptionsRaw = new ObservableCollection<ImageSelection>();
        public ObservableCollection<ImageSelection> imageOptions { get { return imageOptionsRaw; } }

        private ObservableCollection<TextureRemapEntry> recordsRaw = new ObservableCollection<TextureRemapEntry>();
        public ObservableCollection<TextureRemapEntry> records { get { return recordsRaw; } }


        public TextureCombineWindow()
        {
            channelOptionsRaw.Add(ChannelSelection.R);
            channelOptionsRaw.Add(ChannelSelection.G);
            channelOptionsRaw.Add(ChannelSelection.B);
            channelOptionsRaw.Add(ChannelSelection.A);
            channelOptionsRaw.Add(ChannelSelection.RGB);
            imageOptionsRaw.Add(ImageSelection.Image1);
            imageOptionsRaw.Add(ImageSelection.Image2);
            InitializeComponent();

            DataGridComboBoxColumn column;

            column = RecordGrid.Columns[3] as DataGridComboBoxColumn;
            column.ItemsSource = imageOptions;
            column.SelectedItemBinding = new Binding("ImageR");

            column = RecordGrid.Columns[4] as DataGridComboBoxColumn;
            column.ItemsSource = channelOptions;
            column.SelectedItemBinding = new Binding("ChannelR");


            column = RecordGrid.Columns[5] as DataGridComboBoxColumn;
            column.ItemsSource = imageOptions;
            column.SelectedItemBinding = new Binding("ImageG");

            column = RecordGrid.Columns[6] as DataGridComboBoxColumn;
            column.ItemsSource = channelOptions;
            column.SelectedItemBinding = new Binding("ChannelG");


            column = RecordGrid.Columns[7] as DataGridComboBoxColumn;
            column.ItemsSource = imageOptions;
            column.SelectedItemBinding = new Binding("ImageB");

            column = RecordGrid.Columns[8] as DataGridComboBoxColumn;
            column.ItemsSource = channelOptions;
            column.SelectedItemBinding = new Binding("ChannelB");


            column = RecordGrid.Columns[9] as DataGridComboBoxColumn;
            column.ItemsSource = imageOptions;
            column.SelectedItemBinding = new Binding("ImageA");

            column = RecordGrid.Columns[10] as DataGridComboBoxColumn;
            column.ItemsSource = channelOptions;
            column.SelectedItemBinding = new Binding("ChannelA");

        }

        private void SelectImagesClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".png";
            dialog.Filter = "Image Files|*.png;*.dds;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff";
            dialog.CheckFileExists = true;
            dialog.Multiselect = true;
            dialog.ShowDialog();
            string[] files = dialog.FileNames;
            int len = files.Length;
            for (int i = 0; i < len; i++)
            {
                MessageBox.Show("File name: " + files[i]);
            }
        }

        private void ConvertImagesClick(object sender, RoutedEventArgs e)
        {

        }

        public enum ChannelSelection
        {
            R,
            G,
            B,
            A,
            RGB
        }

        public enum ImageSelection
        {
            Image1,
            Image2
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

            ImageSelection imageR = ImageSelection.Image1;
            ChannelSelection channelR = ChannelSelection.R;
            ImageSelection imageG = ImageSelection.Image1;
            ChannelSelection channelG = ChannelSelection.G;
            ImageSelection imageB = ImageSelection.Image1;
            ChannelSelection channelB = ChannelSelection.B;
            ImageSelection imageA = ImageSelection.Image2;
            ChannelSelection channelA = ChannelSelection.RGB;

            string outputName = "";

            public ImageSelection ImageR { get { return imageR; } set { imageR = value; propChanged(); } }
            public ImageSelection ImageG { get { return imageG; } set { imageG = value; propChanged(); } }
            public ImageSelection ImageB { get { return imageB; } set { imageB = value; propChanged(); } }
            public ImageSelection ImageA { get { return imageA; } set { imageA = value; propChanged(); } }
            public ChannelSelection ChannelR { get { return channelR; } set { channelR = value; propChanged(); } }
            public ChannelSelection ChannelG { get { return channelG; } set { channelG = value; propChanged(); } }
            public ChannelSelection ChannelB { get { return channelB; } set { channelB = value; propChanged(); } }
            public ChannelSelection ChannelA { get { return channelA; } set { channelA = value; propChanged(); } }

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
