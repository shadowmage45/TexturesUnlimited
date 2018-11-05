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
        

        public TextureCombineWindow()
        {
            InitializeComponent();

            DataGridComboBoxColumn column;

            column = RecordGrid.Columns[3] as DataGridComboBoxColumn;
            column.ItemsSource = MainWindow.instance.ImageOptions;
            //column.SelectedItemBinding = new Binding("ImageR");
            
            column = RecordGrid.Columns[4] as DataGridComboBoxColumn;
            column.ItemsSource = MainWindow.instance.ImageOptions;
            //column.SelectedItemBinding = new Binding("ImageG");

            column = RecordGrid.Columns[5] as DataGridComboBoxColumn;
            column.ItemsSource = MainWindow.instance.ImageOptions;
            //column.SelectedItemBinding = new Binding("ImageB");

            column = RecordGrid.Columns[6] as DataGridComboBoxColumn;
            column.ItemsSource = MainWindow.instance.ImageOptions;
            //column.SelectedItemBinding = new Binding("ImageA");

        }

        private void SelectImagesClick(object sender, RoutedEventArgs e)
        {
            string img1 = ImageTools.openFileSelectDialog("Primary Texture");
            string img2 = ImageTools.openFileSelectDialog("Second Texture");
            string img3 = ImageTools.openFileSaveDialog("Output Texture");
            TextureRemapEntry entry = new TextureRemapEntry();
            entry.Image1Name = img1;
            entry.Image2Name = img2;
            entry.OutputName = img3;
            MainWindow.instance.RemapRecords.Add(entry);
        }

        private void ConvertImagesClick(object sender, RoutedEventArgs e)
        {
            foreach (TextureRemapEntry entry in MainWindow.instance.RemapRecords) { processEntry(entry); }
        }

        private void processEntry(TextureRemapEntry entry)
        {
            Bitmap image1 = ImageTools.loadImage(entry.Image1Name);
            if (image1 == null)
            {
                MessageBox.Show("Cannot process pair; first image is null");
                return;
            }
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
            Bitmap output = new Bitmap(width, height);
            Color color1, color2;
            byte r, g, b, a;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    color1 = image1.GetPixel(x, y);
                    color2 = image2 == null? Color.White : image2.GetPixel(x, y);
                    r = ImageTools.getChannelSelection(color1, color2, entry.ImageR);
                    g = ImageTools.getChannelSelection(color1, color2, entry.ImageG);
                    b = ImageTools.getChannelSelection(color1, color2, entry.ImageB);
                    a = ImageTools.getChannelSelection(color1, color2, entry.ImageA);
                    output.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                }
            }
            if (image1 != null) { image1.Dispose(); }
            if (image2 != null) { image2.Dispose(); }
            output.Save(entry.OutputName);
            output.Dispose();
        }


    }
}
