using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TexturesUnlimitedTools
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private ObservableCollection<ImageChannelSelection> imageOptionsRaw = new ObservableCollection<ImageChannelSelection>();
        public ObservableCollection<ImageChannelSelection> ImageOptions { get { return imageOptionsRaw; } }

        private ObservableCollection<DDSFormat> ddsOptionsRaw = new ObservableCollection<DDSFormat>();
        public ObservableCollection<DDSFormat> DDSOptions { get { return ddsOptionsRaw; } }

        private ObservableCollection<TextureRemapEntry> recordsRaw = new ObservableCollection<TextureRemapEntry>();
        public ObservableCollection<TextureRemapEntry> RemapRecords { get { return recordsRaw; } }

        private ObservableCollection<TextureConversionEntry> convertRecordsRaw = new ObservableCollection<TextureConversionEntry>();
        public ObservableCollection<TextureConversionEntry> ConvertRecords { get { return convertRecordsRaw; } }
        

        public static MainWindow instance;

        public MainWindow()
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
            ddsOptionsRaw.Add(DDSFormat.DXT1);
            ddsOptionsRaw.Add(DDSFormat.DXT5);
            ddsOptionsRaw.Add(DDSFormat.DXT5nm);
            InitializeComponent();
            instance = this;
        }

        private void TextureCombineClick(object sender, RoutedEventArgs e)
        {
            TextureCombineWindow window = new TextureCombineWindow();
            window.ShowDialog();            
        }

        private void TextureConvertClick(object sender, RoutedEventArgs e)
        {
            TextureConvertWindow window = new TextureConvertWindow();
            window.ShowDialog();
        }

        private void ConfigurationClick(object sender, RoutedEventArgs e)
        {

        }

        private void TextureNormClick(object sender, RoutedEventArgs e)
        {
            TextureBaseMapCreation window = new TextureBaseMapCreation();
            window.ShowDialog();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            instance = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            instance = null;
            Application.Current.Shutdown();
        }

    }

    public class TextureConversionEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        string imageName;
        DDSFormat format;

        public string ImageName { get { return imageName; } set { imageName = value; propChanged(); } }
        public DDSFormat Format { get { return format; } set { format = value; propChanged(); } }

        private void propChanged([CallerMemberName]string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
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
