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
    /// Custom implementation of bitmap image structure that can be utilized within the application.
    /// Will contain helper methods to convert/provide other API specific formats upon request.
    /// Will provide methods to update external formats with color changes.
    /// WIP, Unused
    /// </summary>
    public class TUBitmap
    {

        private int width;
        private int height;
        private int[] colorData;
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        #region CONSTRUCTORS
        public TUBitmap(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
        public TUBitmap(Bitmap bitmap)
        {
            this.width = bitmap.Width;
            this.height = bitmap.Height;
            //TODO
        }
        #endregion
        #region STATIC CONSTRUCTORS
        public static TUBitmap LoadFromFile(string fileName)
        {
            return null;
        }
        #endregion

        public int GetPixel(int x, int y)
        {
            return 0;
        }

    }

}
