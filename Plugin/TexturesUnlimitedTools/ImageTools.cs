using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    break;
            }
            return (byte)0;
        }

    }
}
