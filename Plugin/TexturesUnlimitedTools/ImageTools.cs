using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TexturesUnlimitedTools
{

    public static class ImageTools
    {

        public static Bitmap loadImage(string fileName)
        {
            Image image = Image.FromFile(fileName);
            return new Bitmap(image);
        }

        public static void saveImage(Bitmap image, string fileName)
        {
            image.Save(fileName);
        }

        public static string openFileSelectDialog(string title)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = title;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.dds";
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            dialog.ShowDialog();
            return dialog.FileName;
        }

        public static string[] openFileMultiSelectDialog(string title)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = title;
            dialog.DefaultExt = ".png";
            dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.dds";
            dialog.CheckFileExists = true;
            dialog.Multiselect = true;
            dialog.ShowDialog();
            return dialog.FileNames;
        }

        public static string openFileSaveDialog(string title)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = title;
            dialog.DefaultExt = ".png";
            dialog.Filter = "PNG Files|*.png";
            dialog.CheckFileExists = false;
            dialog.ShowDialog();
            return dialog.FileName;
        }

        public static string openDirectorySelectDialog(string title)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.EnsurePathExists = true;
            dialog.EnsureFileExists = true;
            dialog.Multiselect = false;
            dialog.Title = title;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }
            return "";
        }

        public static byte getChannelSelection(Color color1, Color color2, ImageChannelSelection selection)
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
        
        public static byte getChannelSelection(Color color1, ImageChannelSelection selection)
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
                    return color1.R;
                case ImageChannelSelection.Image2_G:
                    return color1.G;
                case ImageChannelSelection.Image2_B:
                    return color1.B;
                case ImageChannelSelection.Image2_A:
                    return color1.A;
                case ImageChannelSelection.Image2_RGB:
                    return (byte)((color1.R + color1.G + color1.B) / 3f);
                default:
                    return (byte)0;
            }
        }

        public static byte getChannelSelectionByte(Color color1, ChannelSelection selection)
        {
            switch (selection)
            {
                case ChannelSelection.R:
                    return color1.R;
                case ChannelSelection.G:
                    return color1.G;
                case ChannelSelection.B:
                    return color1.B;
                case ChannelSelection.A:
                    return color1.A;
                case ChannelSelection.RGB:
                    {
                        double r = color1.R, g = color1.G, b = color1.B;
                        r /= 255;
                        g /= 255;
                        b /= 255;
                        double lum = r * 0.22 + g * 0.707 + b * 0.071;
                        return (byte)(lum * 255);
                    }
                default:
                    return 0;
            }
        }

        public static double getChannelSelection(Color color1, ChannelSelection selection)
        {
            switch (selection)
            {
                case ChannelSelection.R:
                    return (double)color1.R / 255;
                case ChannelSelection.G:
                    return (double)color1.G / 255;
                case ChannelSelection.B:
                    return (double)color1.B / 255;
                case ChannelSelection.A:
                    return (double)color1.A / 255;
                case ChannelSelection.RGB:
                    {
                        double r = color1.R, g = color1.G, b = color1.B;
                        r /= 255;
                        g /= 255;
                        b /= 255;
                        double lum = r * 0.22 + g * 0.707 + b * 0.071;
                        return lum;
                    }
                default:
                    return 0;
            }
        }

        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            if (bitmap == null) { return null; }
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(ms.ToArray());
            bitmapImage.EndInit();
            return bitmapImage;
        }

        public static void startWorker(DoWorkEventHandler work, ProgressChangedEventHandler update, RunWorkerCompletedEventHandler complete)
        {
            BackgroundWorker worker = new BackgroundWorker();            
            worker.WorkerReportsProgress = true;
            worker.DoWork += work;
            worker.ProgressChanged += update;
            worker.RunWorkerCompleted += complete;
            worker.RunWorkerAsync();
        }

    }

    public enum ImagePreviewSelection
    {
        DIFFUSE,
        AUX,
        SMOOTH,
        MASK,
        DIFFUSE_NORM,
        AUX_NORM,
        SMOOTH_NORM,
        DIFFUSE_DIFFERENCE,
        DIFFUSE_COLOR_DIFFERENCE,
        AUX_DIFFERENCE,
        AUX_COLOR_DIFFERENCE,
        SMOOTH_DIFFERENCE,
        SMOOTH_COLOR_DIFFERENCE
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

    public enum ChannelSelection
    {
        R,
        G,
        B,
        A,
        RGB
    }

    public enum DDSFormat
    {
        DXT1 = 1,
        DXT5 = 5,
        DXT5nm = 6
    }

}
