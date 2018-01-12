using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace BaseMapCreation
{
    public class BaseMapCreator
    {

        private static StreamWriter logStream;

        private static string inputPath;
        private static string outputPath;

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new InvalidOperationException("Not enough arguments passed.  Must include relative or absolute path to GameData folder.");
            }
            string gameDataPath = args[0];
            string fullPath = Path.GetFullPath(gameDataPath);

            outputPath = fullPath + "\\output";
            inputPath = fullPath + "\\input";
            //create the directory in case it didn't previously exist
            Directory.CreateDirectory(outputPath);
            logStream = new StreamWriter(new FileStream(outputPath + "/log.txt", FileMode.Create));
            print("BaseMap creation - running with paths - input/output: " + inputPath + " -> " + outputPath);

            if (!Directory.Exists(inputPath))
            {
                print("ERROR: Could not locate input file directory at: " + inputPath + "  There must be a directory there that contains the textures and configs.");
                Console.ReadLine();
                return;
            }

            string[] fileNames = Directory.GetFiles(inputPath);
            int len = fileNames.Length;
            for (int i = 0; i < len; i++)
            {
                if (fileNames[i].ToLower().EndsWith(".cfg"))
                {
                    ConfigNode fileNode = ConfigNode.Load(fileNames[i]);
                    ConfigNode[] createNodes = fileNode.GetNodes("BASEMAP_CREATE");
                    int len2 = createNodes.Length;
                    for (int k = 0; k < len2; k++)
                    {
                        processTextureFromNode(createNodes[k]);
                    }
                }
            }

            print("Application exiting due to completed run.");
            logStream.Flush();
            logStream.Close();
            Console.ReadLine();
        }

        public static void print(string line)
        {
            logStream.WriteLine(line);
            logStream.Flush();
            Console.WriteLine(line);
        }

        private static void processTextureFromNode(ConfigNode config)
        {
            string fileName = inputPath + "\\" + config.GetValue("texture");

            ConfigNode[] replacementNodes = config.GetNodes("COLOR");
            int len = replacementNodes.Length;
            ColorReplacement[] replacements = new ColorReplacement[len];
            for (int i = 0; i < len; i++)
            {
                replacements[i] = new ColorReplacement(replacementNodes[i]);
            }

            Image img = Image.FromFile(fileName);
            Bitmap bmp = new Bitmap(img);
            int width = bmp.Width;
            int height = bmp.Height;

            Bitmap detailMap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            Color c;
            Color orig;
            Color detail;
            int dr, dg, db;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    c = bmp.GetPixel(x, y);
                    orig = c;
                    for (int i = 0; i < len; i++)
                    {
                        if (replacements[i].isTarget(c))
                        {
                            c = replacements[i].baseColor;
                            break;
                        }
                    }
                    bmp.SetPixel(x, y, c);
                    //dr = 127 + (orig.R - c.R);
                    //dg = 127 + (orig.G - c.G);
                    //db = 127 + (orig.B - c.B);
                    //detail = Color.FromArgb(255, dr, dg, db);
                    //detailMap.SetPixel(x, y, detail);
                }
            }
            bmp.Save(fileName.Substring(0, fileName.Length - 4) + "-basemap.png");
            //detailMap.Save(fileName.Substring(0, fileName.Length - 4) + "-detailmap.png");
        }

        private class ColorReplacement
        {

            public Color baseColor;

            public float inHue;
            public float inSat;
            public float inVal;

            public float hueRange;
            public float satRange;
            public float valRange;

            private float minHue;
            private float maxHue;
            private float minSat;
            private float maxSat;
            private float minVal;
            private float maxVal;

            public ColorReplacement(ConfigNode node)
            {
                //the input HSV to look for
                string[] hsv = node.GetValue("hsv").Split(',');
                inHue = float.Parse(hsv[0].Trim());
                inSat = float.Parse(hsv[1].Trim());
                inVal = float.Parse(hsv[2].Trim());

                //the range of validity (input +/- range)
                hueRange = float.Parse(node.GetValue("hueRange"));
                satRange = float.Parse(node.GetValue("satRange"));
                valRange = float.Parse(node.GetValue("valRange"));

                minHue = inHue - hueRange;
                maxHue = inHue + hueRange;
                minSat = inSat - satRange;
                maxSat = inSat + satRange;
                minVal = inVal - valRange;
                maxVal = inVal + valRange;

                //the output replacement color for inputs within range
                int r, g, b;
                string[] baseVals = node.GetValue("base").Split(',');
                r = byte.Parse(baseVals[0].Trim());
                g = byte.Parse(baseVals[1].Trim());
                b = byte.Parse(baseVals[2].Trim());
                baseColor = Color.FromArgb(255, r, g, b);                
            }

            public bool isTarget(Color input)
            {
                float cH = inHue;
                float cS = inSat;
                float cV = inVal;

                float iH = input.GetHue();
                float iS = input.GetSaturation();
                float iV = input.GetBrightness();
                bool hueGood = iH >= minHue && iH <= maxHue;
                bool satGood = iS >= minSat && iS <= maxSat;
                bool valGood = iV >= minVal && iV <= maxVal;
                return hueGood && satGood && valGood;
            }

        }

    }
}
