using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TexturesUnlimitedTools
{
    /// <summary>
    /// Interaction logic for TextureBaseMapCreation.xaml
    /// </summary>
    public partial class TextureBaseMapCreation : Window
    {

        Bitmap baseMap;//input
        Bitmap maskMap;//mask
        Bitmap normMap;//output
        Bitmap diffMap;//difference texture between input and output
        Bitmap colDiffMap;//difference texture between input and output -- green = negative diff, red = positive diff

        BitmapImage baseImage;
        BitmapImage maskImage;
        BitmapImage normImage;
        BitmapImage diffImage;
        BitmapImage colDiffImage;

        ImageChannelSelection sourceChannel;
        NormGenerator generator;
        
        List<ImagePreviewSelection> previewOptions = new List<ImagePreviewSelection>();
        public List<ImagePreviewSelection> PreviewOptions { get { return previewOptions; } }

        public TextureBaseMapCreation()
        {
            previewOptions.Add(ImagePreviewSelection.SOURCE);
            previewOptions.Add(ImagePreviewSelection.MASK);
            previewOptions.Add(ImagePreviewSelection.NORM);
            previewOptions.Add(ImagePreviewSelection.DIFF);
            previewOptions.Add(ImagePreviewSelection.COLDIFF);
            InitializeComponent();
            PreviewSelectionComboBox.ItemsSource = previewOptions;
            PreviewSelectionComboBox.SelectedIndex = 0;
            PreviewSelectionComboBox.SelectionChanged += PreviewTypeSelected;
        }

        private void PreviewTypeSelected(object sender, SelectionChangedEventArgs e)
        {
            updatePreview();
        }

        private void SelectBaseMapClik(object sender, RoutedEventArgs e)
        {
            string img1 = ImageTools.openFileSelectDialog("Select a PNG base image");
            InputFileBox.Text = img1;
            baseMap = new Bitmap(System.Drawing.Image.FromFile(img1));
            baseImage = ImageTools.BitmapToBitmapImage(baseMap);
            updatePreview();
        }

        private void SelctMaskClick(object sender, RoutedEventArgs e)
        {
            string img2 = ImageTools.openFileSelectDialog("Select a PNG mask image");
            MaskFileBox.Text = img2;
            maskMap = new Bitmap(System.Drawing.Image.FromFile(img2));
            maskImage = ImageTools.BitmapToBitmapImage(maskMap);
            updatePreview();
        }

        private void GenerateNormTexClick(object sender, RoutedEventArgs e)
        {
            generateOutput();
        }

        private void UpdatePreviewClick(object sender, RoutedEventArgs e)
        {
            updatePreview();
        }

        private void ViewDifferenceToggle(object sender, RoutedEventArgs e)
        {

        }

        private void ColoredDifferenceToggle(object sender, RoutedEventArgs e)
        {

        }

        private bool generateOutput()
        {
            if (baseImage == null || maskImage == null)
            {
                return false;
            }
            generator = new NormGenerator(baseMap, maskMap, sourceChannel, new NormParams());
            ProgressWindow window = new ProgressWindow();
            window.start(generator.generate, generateFinished);
            return true;
        }

        private void generateFinished()
        {
            Debug.WriteLine("Generation finished!");
            normMap = generator.dest;
            normImage = ImageTools.BitmapToBitmapImage(normMap);
            diffMap = generator.difference;
            diffImage = ImageTools.BitmapToBitmapImage(diffMap);
            colDiffMap = generator.coloredDiff;
            colDiffImage = ImageTools.BitmapToBitmapImage(colDiffMap);
            Debug.WriteLine("norm: " + normImage + " diff: " + diffImage + " col: " + colDiffImage);
            updatePreview();
        }

        private void updatePreview()
        {
            ImagePreviewSelection previewSelection = (ImagePreviewSelection)PreviewSelectionComboBox.SelectedItem;
            switch (previewSelection)
            {
                case ImagePreviewSelection.SOURCE:
                    MapImage.Source = baseImage;
                    break;
                case ImagePreviewSelection.MASK:
                    MapImage.Source = maskImage;
                    break;
                case ImagePreviewSelection.NORM:
                    MapImage.Source = normImage;
                    break;
                case ImagePreviewSelection.DIFF:
                    MapImage.Source = diffImage;
                    break;
                case ImagePreviewSelection.COLDIFF:
                    MapImage.Source = colDiffImage;
                    break;
                default:
                    MapImage.Source = null;
                    break;
            }
        }
        
        private void ExportClick(object sender, RoutedEventArgs e)
        {
            if (normMap == null) { return; }
            string dest = ImageTools.openFileSaveDialog("Save Image");
            if (!string.IsNullOrEmpty(dest))
            {
                normMap.Save(dest);
            }
        }

    }

    public class NormGenerator
    {

        private Bitmap src;
        private Bitmap mask;
        public Bitmap dest;
        public Bitmap difference;
        public Bitmap coloredDiff;
        private NormParams parameters;

        private double sumR =0, sumG = 0, sumB = 0;
        private double countR = 0, countG = 0, countB = 0;
        private double minR = 1, minG = 1, minB = 1;
        private double maxR = 0, maxG = 0, maxB = 0;

        public double outR, outG, outB;

        public NormGenerator(Bitmap source, Bitmap mask, ImageChannelSelection sourceChannel, NormParams opts)
        {
            this.src = source;
            this.mask = mask;
            this.parameters = opts;
        }

        public void generate(object sender, DoWorkEventArgs work)
        {
            System.Drawing.Color srcColor, maskColor;
            int width = src.Width;
            int height = src.Height;
            double sr, sg, sb, mr, mg, mb;

            //progress updating vars
            double totalPixels = width * height * 3;
            double processedPixels = 0;
            double progress = 0;
            double prevProg = 0;

            //difference mask vars
            
            //first pass -- get min/max bounds
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    maskColor = mask.GetPixel(x, y);
                    if (maskColor.R == 0 && maskColor.G == 0 && maskColor.B == 0) { continue; }//unmasked pixel, skip
                    getPixelColors(maskColor, out mr, out mg, out mb);
                    normalizeMask(ref mr, ref mg, ref mb);
                    srcColor = src.GetPixel(x, y);
                    getPixelColors(srcColor, out sr, out sg, out sb);
                    channelCalc(sr, sg, sb, mr, ref countR, ref sumR, ref minR, ref maxR);
                    channelCalc(sr, sg, sb, mg, ref countG, ref sumG, ref minG, ref maxG);
                    channelCalc(sr, sg, sb, mb, ref countB, ref sumB, ref minB, ref maxB);
                    processedPixels++;
                    progress = (processedPixels / totalPixels) * 100d;
                    if (progress - prevProg > 1)
                    {
                        prevProg = progress;
                        Debug.WriteLine("Worker Work Update: " + progress);
                        ((BackgroundWorker)sender).ReportProgress((int)progress);
                    }
                }
            }

            ////second pass -- count, sum
            //for (int x = 0; x < width; x++)
            //{
            //    for (int y = 0; y < height; y++)
            //    {
            //        maskColor = mask.GetPixel(x, y);
            //        if (maskColor.R == 0 && maskColor.G == 0 && maskColor.B == 0) { continue; }//unmasked pixel, skip
            //        getPixelColors(maskColor, out mr, out mg, out mb);
            //        srcColor = src.GetPixel(x, y);
            //        getPixelColors(srcColor, out sr, out sg, out sb);
            //        channelCalc(sr, mr, ref countR, ref sumR, ref minR, ref maxR);
            //        channelCalc(sg, mg, ref countG, ref sumG, ref minG, ref maxG);
            //        channelCalc(sb, mb, ref countB, ref sumB, ref minB, ref maxB);
            //    }
            //}

            outR = sumR / countR;
            outG = sumG / countG;
            outB = sumB / countB;

            //third pass -- write output
            dest = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            difference = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            coloredDiff = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    maskColor = mask.GetPixel(x, y);
                    if (maskColor.R == 0 && maskColor.G == 0 && maskColor.B == 0) { continue; }//unmasked pixel, skip
                    getPixelColors(maskColor, out mr, out mg, out mb);
                    normalizeMask(ref mr, ref mg, ref mb);
                    double pix = outR * mr + outG * mg + outB * mb;
                    pix = Math.Min(pix, 1);
                    dest.SetPixel(x, y, setPixelColors(pix, pix, pix));
                    
                    srcColor = src.GetPixel(x, y);
                    getPixelColors(srcColor, out sr, out sg, out sb);
                    difference.SetPixel(x, y, getDiffColor(pix, sr, sg, sb, false));
                    coloredDiff.SetPixel(x, y, getDiffColor(pix, sr, sg, sb, true));

                    //double op in this loop, count twice for progress reporting
                    processedPixels++;
                    processedPixels++;
                    progress = (processedPixels / totalPixels) * 100d;
                    if (progress - prevProg > 1)
                    {
                        prevProg = progress;
                        Debug.WriteLine("Worker Work Update: " + progress);
                        ((BackgroundWorker)sender).ReportProgress((int)progress);
                    }
                }
            }
        }

        private void getPixelColors(System.Drawing.Color color, out double r, out double g, out double b)
        {
            r = color.R / 255d;
            g = color.G / 255d;
            b = color.B / 255d;
        }

        private void normalizeMask(ref double r, ref double g, ref double b)
        {
            double len = Math.Sqrt(r * r + g * g + b * b);
            r /= len;
            g /= len;
            b /= len;
        }

        private System.Drawing.Color setPixelColors(double r, double g, double b)
        {
            int ir, ig, ib;
            ir = (int)(r * 255d);
            ig = (int)(g * 255d);
            ib = (int)(b * 255d);
            return System.Drawing.Color.FromArgb(255, ir, ig, ib);
        }

        private void channelCalc(double r, double g, double b, double maskVal, ref double count, ref double sum, ref double min, ref double max)
        {
            if (maskVal <= 0) { return; }
            double adjSrc = getLuminosity(r, g, b) * maskVal;
            count += maskVal;
            sum += adjSrc;
            if (adjSrc < min) { min = adjSrc; }
            if (adjSrc > max) { max = adjSrc; }
        }

        private double getLuminosity(double r, double g, double b)
        {
            return r * 0.22f + g * 0.707f + b * 0.071f;
        }

        private System.Drawing.Color getDiffColor(double outPix, double sr, double sg, double sb, bool colored)
        {
            double r=0, g=0, b=0;
            double lum = getLuminosity(sr, sg, sb);
            double diff = lum - outPix;
            if (colored)
            {
                if (diff < 0)
                {
                    r = Math.Abs(diff);
                }
                else if (diff > 0)
                {
                    g = Math.Abs(diff);
                }
            }
            else
            {
                r = g = b = Math.Abs(diff);
            }
            return setPixelColors(r, g, b);
        }

    }

    public class NormParams
    {
        float oneMin = 0.0f;
        float oneMid = 0.5f;
        float oneMax = 1.0f;
        float twoMin = 0.0f;
        float twoMid = 0.5f;
        float twoMax = 1.0f;
        float threeMin = 0.0f;
        float threeMid = 0.5f;
        float threeMax = 1.0f;
        public NormParams() { }
    }

}
