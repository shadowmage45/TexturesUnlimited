using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;

namespace TexturesUnlimitedTools
{

    public sealed class DirectBitmap : IBitmap, IDisposable
    {

        //code for class from: https://stackoverflow.com/questions/24701703/c-sharp-faster-alternatives-to-setpixel-and-getpixel-for-bitmaps-for-windows-f
        // with modifications and additions.  Used for both inputs and output textures.
        // for this use it results in about a 2x improvement in speed
        public Bitmap Bitmap { get; private set; }
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        private GCHandle BitsHandle { get; set; }

        public Color this[int x, int y]
        {
            get
            {
                return GetPixel(x, y);
            }

            set
            {
                SetPixel(x, y, value);
            }
        }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, BitsHandle.AddrOfPinnedObject());
        }

        public DirectBitmap(Bitmap source)
        {
            Width = source.Width;
            Height = source.Height;
            Bits = new Int32[Width * Height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(Width, Height, Width * 4, PixelFormat.Format32bppArgb, BitsHandle.AddrOfPinnedObject());
            int bitsPerPixel = Image.GetPixelFormatSize(source.PixelFormat);
            if (bitsPerPixel != 32)
            {
                throw new ArgumentException("Input bitmap had invalid pixel format.  Need 32bpp ARGB");
            }
            BitmapData lockD =  source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                //pointer to the start of the array of pixel data
                //should be laid out linearly, where each row is written in sequence
                //addr = row * width + column
                int* startPointer = (int*)lockD.Scan0;
                int addr;
                //iterating by pixel....
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        addr = y * Width + x;
                        SetPixel(x, y, Color.FromArgb(startPointer[addr]));
                    }
                }
            }
            source.UnlockBits(lockD);
            source.Dispose();
        }

        public void SetPixel(int x, int y, Color colour)
        {
            Bits[x + (y * Width)] = colour.ToArgb();
        }

        public Color GetPixel(int x, int y)
        {
            return Color.FromArgb(Bits[x + (y * Width)]);
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }

    }
}
