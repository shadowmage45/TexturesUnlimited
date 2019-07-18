using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System;
using System.Diagnostics;
using System.Text;

namespace TexturesUnlimitedTools
{

    public static class ImageTools
    {

        public static Bitmap loadImage(string fileName)
        {
            //quick and dirty check for DDS images
            if (fileName.ToLower().EndsWith(".dds"))
            {
                return BitmapFromDDS(fileName);
            }
            else//let windows built-in decoding handle it
            {
                System.Drawing.Image image = System.Drawing.Image.FromFile(fileName);
                Bitmap bmp = new Bitmap(image);
                image.Dispose();
                return bmp;
            }
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

        public static Bitmap BitmapFromBytes(byte[] data, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height);
            int idx;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    idx = (y * width + x)*4;//4 bytes per pixel
                    byte r = data[idx];
                    byte g = data[idx + 1];
                    byte b = data[idx + 2];
                    byte a = data[idx + 3];
                    bitmap.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                }
            }
            return bitmap;
        }

        public static Bitmap BitmapFromDDS(string fileName)
        {
            string path = System.IO.Path.GetFullPath(fileName);
            Debug.WriteLine("PATH: " + path);

            //rip raw file data into byte array
            byte[] fileData = System.IO.File.ReadAllBytes(path);

            //parse header from file data
            DDSHeader header = new DDSHeader(fileData);

            //check the FourCC code for DXT type
            string fourcc = header.FourCC;
            Debug.WriteLine("Parsed FourCC: " + fourcc);

            //grab an array of bytes with the DXT data for the primary image
            byte[] imageData;
            int len = header.Length;
            imageData = new byte[len];
            Array.Copy(fileData, 128, imageData, 0, len);

            //based on fourcc code, decompress accordingly into RGBA byte array
            if (fourcc == "DXT1")
            {
                //4 bits per pixel
                imageData = DxtUtil.DecompressDxt1(imageData, header.Width, header.Height);
            }
            else if (fourcc == "DXT3")
            {
                imageData = DxtUtil.DecompressDxt3(imageData, header.Width, header.Height);
            }
            else if (fourcc == "DXT5")
            {
                //8 bits per pixel
                imageData = DxtUtil.DecompressDxt5(imageData, header.Width, header.Height);
            }
            else
            {
                throw new BadImageFormatException("Unsupported FourCC type: " + fourcc);
            }

            //return new bitmap from parsed byte array
            return BitmapFromBytes(imageData, header.Width, header.Height);
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

    public class DDSHeader
    {

        /** https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header                 
            typedef struct {
              DWORD           dwSize;
              DWORD           dwFlags;
              DWORD           dwHeight;
              DWORD           dwWidth;
              DWORD           dwPitchOrLinearSize;
              DWORD           dwDepth;
              DWORD           dwMipMapCount;
              DWORD           dwReserved1[11];
              DDS_PIXELFORMAT ddspf;
              DWORD           dwCaps;
              DWORD           dwCaps2;
              DWORD           dwCaps3;
              DWORD           dwCaps4;
              DWORD           dwReserved2;
            } DDS_HEADER;
         **/
        /**
             struct DDS_PIXELFORMAT {
              DWORD dwSize;
              DWORD dwFlags;
              DWORD dwFourCC;
              DWORD dwRGBBitCount;
              DWORD dwRBitMask;
              DWORD dwGBitMask;
              DWORD dwBBitMask;
              DWORD dwABitMask;
            };
         **/
        /**
         * Where a DWORD is an unsigned 32 bit integer
         **/
        //raw header data, cached for ease of use into different encodings
        byte[] headerData;

        //MAGIC WORD 'DDS ' - not part of standard header spec
        UInt32 magic;
        //STANDARD HEADER
        UInt32 dwSize;//should always be numeric value of 124
        UInt32 dwFlags;
        UInt32 dwHeight;//pixel height
        UInt32 dwWidth;//pixel width
        UInt32 dwPitchOrLinearSize;//byte-length of primary image
        UInt32 dwDepth;//??
        UInt32 dwMipMapCount;
        UInt32 dwReserved01;
        UInt32 dwReserved02;
        UInt32 dwReserved03;
        UInt32 dwReserved04;
        UInt32 dwReserved05;
        UInt32 dwReserved06;
        UInt32 dwReserved07;
        UInt32 dwReserved08;
        UInt32 dwReserved09;
        UInt32 dwReserved10;
        UInt32 dwReserved11;
        //DDS_PIXEL_FORMAT
        UInt32 dwSize2;
        UInt32 dwFlags2;
        UInt32 dwFourCC;
        UInt32 dwRGBBitCount;
        UInt32 dwRBitMask;
        UInt32 dwGBitMask;
        UInt32 dwBBitMask;
        UInt32 dwABitMask;
        //REMAINDER OF STANDARD HEADER
        UInt32 dwCaps;
        UInt32 dwCaps2;
        UInt32 dwCaps3;
        UInt32 dwCaps4;
        UInt32 dwReserved12;

        public DDSHeader(byte[] fileData)
        {
            magic = BitConverter.ToUInt32(fileData, 0);//first four bytes are the ASCII string 'DDS '
            dwSize = BitConverter.ToUInt32(fileData, 4);
            dwFlags = BitConverter.ToUInt32(fileData, 8);
            dwHeight = BitConverter.ToUInt32(fileData, 12);
            dwWidth = BitConverter.ToUInt32(fileData, 16);
            dwPitchOrLinearSize = BitConverter.ToUInt32(fileData, 20);
            dwDepth = BitConverter.ToUInt32(fileData, 24);
            dwMipMapCount = BitConverter.ToUInt32(fileData, 28);
            dwReserved01 = BitConverter.ToUInt32(fileData, 32);//the next 11 DWORD are used for 'reserved' data
            dwReserved02 = BitConverter.ToUInt32(fileData, 36);
            dwReserved03 = BitConverter.ToUInt32(fileData, 40);
            dwReserved04 = BitConverter.ToUInt32(fileData, 44);
            dwReserved05 = BitConverter.ToUInt32(fileData, 48);
            dwReserved06 = BitConverter.ToUInt32(fileData, 52);
            dwReserved07 = BitConverter.ToUInt32(fileData, 56);
            dwReserved08 = BitConverter.ToUInt32(fileData, 60);
            dwReserved09 = BitConverter.ToUInt32(fileData, 64);
            dwReserved10 = BitConverter.ToUInt32(fileData, 68);
            dwReserved11 = BitConverter.ToUInt32(fileData, 72);
            dwSize2 = BitConverter.ToUInt32(fileData, 76);//DDX_PIXELFORMAT -- 32 bytes total -- bytes 76-107(inc)
            dwFlags2 = BitConverter.ToUInt32(fileData, 80);
            dwFourCC = BitConverter.ToUInt32(fileData, 84);
            dwRGBBitCount = BitConverter.ToUInt32(fileData, 88);
            dwRBitMask = BitConverter.ToUInt32(fileData, 92);
            dwGBitMask = BitConverter.ToUInt32(fileData, 96);
            dwBBitMask = BitConverter.ToUInt32(fileData, 100);
            dwABitMask = BitConverter.ToUInt32(fileData, 104);
            dwCaps = BitConverter.ToUInt32(fileData, 108);//the remainder of the DXT header
            dwCaps2 = BitConverter.ToUInt32(fileData, 112);
            dwCaps3 = BitConverter.ToUInt32(fileData, 116);
            dwCaps4 = BitConverter.ToUInt32(fileData, 120);
            dwReserved12 = BitConverter.ToUInt32(fileData, 124);
            headerData = new byte[128];
            Array.Copy(fileData, 0, headerData, 0, 128);
        }

        /// <summary>
        /// Pixel width of the image
        /// </summary>
        public int Width { get { return (int)dwWidth; } }

        /// <summary>
        /// Pixel height of the image
        /// </summary>
        public int Height { get { return (int)dwHeight; } }

        /// <summary>
        /// Number of bytes of data for primary image
        /// </summary>
        public int Length { get { return (int)dwPitchOrLinearSize; } }

        /// <summary>
        /// String representation of the FourCC code 
        /// </summary>
        public string FourCC { get { return Encoding.ASCII.GetString(headerData, 84, 4); } }

        /// <summary>
        /// String representation of the primary 'reserved' datablock.  Some applications use this to write metadata.
        /// </summary>
        public string Reserved { get { return Encoding.ASCII.GetString(headerData, 32, 11 * 4); } }

    }

}
