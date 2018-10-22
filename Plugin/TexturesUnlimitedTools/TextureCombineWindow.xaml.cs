using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            foreach (TextureRemapEntry entry in records) { processEntry(entry); }
        }

        private static string openFileSelectDialog(string title)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = title;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff";
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

        private void processEntry(TextureRemapEntry entry)
        {
            MessageBox.Show("Processing entry...");
            Bitmap image1 = ImageTools.loadImage(entry.Image1Name);
            if (image1 == null)
            {
                MessageBox.Show("Cannot process pair; first image is null");
                return;
            }
            MessageBox.Show("Loaded input image 1");
            int width = image1.Width;
            int height = image1.Height;
            Bitmap image2 = ImageTools.loadImage(entry.Image2Name);
            if (image2 != null)
            {
                if (image2.Width != width || image2.Height != height)
                {
                    image1.Dispose();
                    image2.Dispose();
                    MessageBox.Show("Cannot convert images, they must be the same width and height");
                    return;
                }
            }
            MessageBox.Show("Loaded input image 2");
            Bitmap output = new Bitmap(width, height);
            Color color1, color2;
            byte r, g, b, a;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    color1 = image1.GetPixel(x, y);
                    color2 = image2 == null? Color.White : image2.GetPixel(x, y);
                    r = getChannelSelection(color1, color2, entry.ImageR);
                    g = getChannelSelection(color1, color2, entry.ImageG);
                    b = getChannelSelection(color1, color2, entry.ImageB);
                    a = getChannelSelection(color1, color2, entry.ImageA);
                    output.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                }
            }
            if (image1 != null) { image1.Dispose(); }
            if (image2 != null) { image2.Dispose(); }
            output.Save(entry.OutputName);
            output.Dispose();
        }

        private byte getChannelSelection(Color color1, Color color2, ImageChannelSelection selection)
        {
            switch (selection)
            {
                case ImageChannelSelection.Image1_R:
                    return color1.R;
                case ImageChannelSelection.Image1_B:
                    return color1.B;
                case ImageChannelSelection.Image1_G:
                    return color1.G;
                case ImageChannelSelection.Image1_A:
                    return color1.A;
                case ImageChannelSelection.Image1_RGB:
                    return (byte)((color1.R + color1.G + color1.B) / 3);
                case ImageChannelSelection.Image2_R:
                    return color2.R;
                case ImageChannelSelection.Image2_G:
                    return color2.G;
                case ImageChannelSelection.Image2_B:
                    return color2.B;
                case ImageChannelSelection.Image2_A:
                    return color2.A;
                case ImageChannelSelection.Image2_RGB:
                    return (byte)((color2.R + color2.G + color2.B) / 3f);
                default:
                    break;
            }
            return (byte)0;
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
