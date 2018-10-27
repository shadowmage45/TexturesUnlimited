using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BaseMapCreation
{
    public class BaseMapCreator
    {

        private static StreamWriter logStream;
        
        private static Image[] maskImages;
        private static Bitmap[] maskMaps;
        private static Image inputImage;
        private static Bitmap inputMap;

        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //get base folder path of the executable
            string basePath = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            //create a log.txt file to output to
            logStream = new StreamWriter(new FileStream(basePath + "/log.txt", FileMode.Create));
            //check for input/output folder existence, and create if not present
            string inputMaskFolder = basePath + Path.DirectorySeparatorChar + "masks";
            createDirectory(inputMaskFolder);
            string inputBaseFolder = basePath + Path.DirectorySeparatorChar + "base";
            createDirectory(inputBaseFolder);
            string outputFolder = basePath + Path.DirectorySeparatorChar + "output";
            createDirectory(outputFolder);

            loadMasks(inputMaskFolder);
            if (maskImages.Length <= 0)
            {
                print("No masks were found in /masks subfolder.  Please add some RGB section masks in .png format.");
            }
            loadInput(inputBaseFolder);

            //Bitmap output = new Bitmap(inputMap.Width, inputMap.Height, PixelFormat.Format32bppArgb);
            DirectBitmap output = new DirectBitmap(inputMap.Width, inputMap.Height);
            process(output);
            output.Bitmap.Save(outputFolder + Path.DirectorySeparatorChar + "output-mask.png");
            output.Dispose();
            sw.Stop();
            print("Application exiting due to completed run.");
            print("Elapsed time (ms): " + sw.ElapsedMilliseconds);
            print("Masks processed: " + maskMaps.Length);
            logStream.Flush();
            logStream.Close();
            Console.ReadLine();
        }

        public static void createDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                print("Creating image processing folder: " + path);
                Directory.CreateDirectory(path);
            }
        }

        public static void print(string line)
        {
            logStream.WriteLine(line);
            logStream.Flush();
            Console.WriteLine(line);
        }

        private static void loadMasks(string path)
        {
            string[] fileNames = Directory.GetFiles(path, "*.png");
            List <Image> images = new List<Image>();
            foreach (string file in fileNames)
            {
                Image img = loadImage(file);
                if (img != null)
                {
                    images.Add(img);
                    print("Loaded image: " + file);
                }
                else
                {
                    print("Could not load image: " + file);
                }
            }
            int len = images.Count;
            maskImages = new Image[len];
            maskMaps = new Bitmap[len];
            for (int i = 0; i < len; i++)
            {
                maskImages[i] = images[i];
                maskMaps[i] = new Bitmap(images[i]);
            }
        }

        private static void loadInput(string path)
        {
            string[] fileNames = Directory.GetFiles(path, "*.png");
            if (fileNames.Length > 0)
            {
                Image image = loadImage(fileNames[0]);
                inputImage = image;
                inputMap = new Bitmap(inputImage);
            }            
        }

        private static Image loadImage(string fileName)
        {
            return Image.FromFile(fileName);
        }

        private static void process(DirectBitmap output)
        {
            //create value cache arrays, one for each of R,G,B in each of the input masks
            //track min and max for reach of R,G,B for each input mask
            //loop through input mask textures, aggregating values
            int len1 = maskMaps.Length;
            MaskData[] data = new MaskData[len1];
            for (int i = 0; i < len1; i++)
            {
                data[i] = new MaskData(maskMaps[i]);
                data[i].processMaskCount(inputMap);
                data[i].writeOutputs(output);
            }
        }

        public class DirectBitmap : IDisposable
        {
            //code from: https://stackoverflow.com/questions/24701703/c-sharp-faster-alternatives-to-setpixel-and-getpixel-for-bitmaps-for-windows-f
            // with so far no modification.  Used for both inputs and output textures.
            public Bitmap Bitmap { get; private set; }
            public Int32[] Bits { get; private set; }
            public bool Disposed { get; private set; }
            public int Height { get; private set; }
            public int Width { get; private set; }

            protected GCHandle BitsHandle { get; private set; }

            public DirectBitmap(int width, int height)
            {
                Width = width;
                Height = height;
                Bits = new Int32[width * height];
                BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
                Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, BitsHandle.AddrOfPinnedObject());
            }

            public void SetPixel(int x, int y, Color colour)
            {
                int index = x + (y * Width);
                int col = colour.ToArgb();

                Bits[index] = col;
            }

            public Color GetPixel(int x, int y)
            {
                int index = x + (y * Width);
                int col = Bits[index];
                Color result = Color.FromArgb(col);

                return result;
            }

            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                Bitmap.Dispose();
                BitsHandle.Free();
            }
        }

        private class MaskData
        {

            long aggregateValueR;
            long aggregateValueG;
            long aggregateValueB;

            long sampleCountR;
            long sampleCountG;
            long sampleCountB;

            int minR = 255;
            int maxR = 0;
            int minG = 255;
            int maxG = 0;
            int minB = 255;
            int maxB = 0;

            Bitmap mask;

            public MaskData(Bitmap image)
            {
                mask = image;
            }

            public void processMaskCount(Bitmap inputTexture)
            {
                int width = inputTexture.Width;
                int height = inputTexture.Height;
                Color maskColor;
                Color imageColor;
                int luminosity;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        maskColor = mask.GetPixel(x, y);
                        if (maskColor.R > 0 || maskColor.G > 0 || maskColor.B > 0)
                        {
                            imageColor = inputTexture.GetPixel(x, y);
                            luminosity = getLuminosity(imageColor);// (int)(imageColor.GetBrightness() * 255);
                            if (maskColor.R > 0)
                            {
                                sampleCountR++;
                                minR = (int)Math.Min(minR, luminosity);
                                maxR = (int)Math.Max(maxR, luminosity);
                                aggregateValueR += luminosity;
                            }
                            if (maskColor.G > 0)
                            {
                                sampleCountG++;
                                minG = (int)Math.Min(minR, luminosity);
                                maxG = (int)Math.Max(maxR, luminosity);
                                aggregateValueG += luminosity;
                            }
                            if (maskColor.B > 0)
                            {
                                sampleCountB++;
                                minB = (int)Math.Min(minR, luminosity);
                                maxB = (int)Math.Max(maxR, luminosity);
                                aggregateValueB += luminosity;
                            }
                        }
                    }
                }
            }

            private int getLuminosity(Color color)
            {
                float r = color.R * 255;
                float g = color.G * 255;
                float b = color.B * 255;
                return (int)((r * 0.22f + g * 0.707f + b * 0.071f)/255);
            }

            public void writeOutputs(DirectBitmap map)
            {
                Color maskColor;
                int width = map.Width;
                int height = map.Height;
                double valR = (double)aggregateValueR / (double)sampleCountR;
                double valG = (double)aggregateValueG / (double)sampleCountG;
                double valB = (double)aggregateValueB / (double)sampleCountB;
                print(valR + ", " + valG + "," + valB);
                double maskR, maskG, maskB;
                double maskSum;
                double colorValR;
                double colorValG;
                double colorValB;
                byte byteColorR;
                byte byteColorG;
                byte byteColorB;
                byte outColor;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        maskColor = mask.GetPixel(x, y);
                        maskR = (double)maskColor.R / 255d;
                        maskG = (double)maskColor.G / 255d;
                        maskB = (double)maskColor.B / 255d;
                        maskSum = maskR + maskG + maskB;
                        if (maskSum > 0)
                        {
                            colorValR = 0;
                            colorValG = 0;
                            colorValB = 0;
                            //normalize the mask value
                            double length = Math.Sqrt(maskR * maskR + maskG * maskG + maskB * maskB);
                            if (length > 1)
                            {
                                maskR = maskR / length;
                                maskG = maskG / length;
                                maskB = maskB / length;
                            }
                            colorValR = maskR * valR;
                            colorValG = maskG * valG;
                            colorValB = maskB * valB;
                            byteColorR = (byte)((colorValR));
                            byteColorG = (byte)((colorValG));
                            byteColorB = (byte)((colorValB));
                            outColor = (byte)(byteColorR + byteColorG + byteColorB);
                            map.SetPixel(x, y, Color.FromArgb(255, outColor, outColor, outColor));
                        }
                        else
                        {
                            //TODO read the existing texture, only write to it once; or do a pre-fill of the texture to black
                            map.SetPixel(x, y, Color.FromArgb(255, 0, 0, 0));
                        }
                    }
                }
            }

        }

    }
}
