using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TexturesUnlimitedTools
{

    /// <summary>
    /// Class for conversion and utility methods between bitmap formats
    /// </summary>
    public static class Bitmaps
    {

    }

    public interface IBitmap
    {
        int Width { get; }
        int Height { get; }
        System.Drawing.Color GetPixel(int x, int y);
        void SetPixel(int x, int y, System.Drawing.Color color);
        System.Drawing.Color this[int x, int y] { get; set; }
    }

    /// <summary>
    /// Wrapper around windows Bitmap (slow)
    /// </summary>
    public class TUBitmapWrapper : IBitmap, IDisposable
    {

        private Bitmap image;

        public TUBitmapWrapper(int width, int height)
        {
            this.image = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        public TUBitmapWrapper(Bitmap source)
        {
            this.image = source;
        }

        public TUBitmapWrapper(Image source)
        {
            this.image = new Bitmap(source);
        }

        public System.Drawing.Color this[int x, int y]
        {
            get { return GetPixel(x, y); }
            set { SetPixel(x, y, value); }
        }

        public int Height => image.Height;

        public int Width => image.Width;

        public System.Drawing.Color GetPixel(int x, int y) => image.GetPixel(x, y);

        public void SetPixel(int x, int y, System.Drawing.Color color) => image.SetPixel(x, y, color);

        public void Dispose() => image.Dispose();

    }

    public class TUWriteableBitmap : IBitmap
    {
        private int width, height;
        WriteableBitmap image;
        public TUWriteableBitmap(int width, int height)
        {
            this.width = width;
            this.height = height;
            image = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        }

        public System.Drawing.Color this[int x, int y]
        {
            get { return GetPixel(x, y); }
            set { SetPixel(x, y, value); }
        }

        public int Height => image.PixelHeight;

        public int Width => image.PixelWidth;

        public System.Drawing.Color GetPixel(int x, int y)
        {
            throw new NotImplementedException();
        }

        public void SetPixel(int x, int y, System.Drawing.Color color)
        {
            throw new NotImplementedException();
        }
    }

}
