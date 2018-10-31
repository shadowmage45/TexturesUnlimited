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

        //raw bitmaps of input textures
        DirectBitmap diffuseMap;//diffuse input
        DirectBitmap auxMap;//metal/specular input
        DirectBitmap smoothMap;//metal/specular input
        DirectBitmap maskMap;//mask

        //raw bitmaps of generated/output textures
        DirectBitmap diffuseNormMap;//diffuse normalization output
        DirectBitmap diffuseDiffMap;//difference texture between input and output
        DirectBitmap diffuseColDiffMap;//difference texture between input and output -- green = negative diff, red = positive diff

        DirectBitmap auxNormMap;//diffuse normalization output
        DirectBitmap auxDiffMap;//difference texture between input and output
        DirectBitmap auxColDiffMap;//difference texture between input and output -- green = negative diff, red = positive diff

        DirectBitmap smoothNormMap;//diffuse normalization output
        DirectBitmap smoothDiffMap;//difference texture between input and output
        DirectBitmap smoothColDiffMap;//difference texture between input and output -- green = negative diff, red = positive diff

        //WPF BitmapImage equivalent Bitmap's that are above
        BitmapImage diffuseImage;
        BitmapImage auxImage;
        BitmapImage smoothImage;
        BitmapImage maskImage;

        BitmapImage diffuseNormImage;
        BitmapImage diffuseDiffImage;
        BitmapImage diffuseColDiffImage;

        BitmapImage auxNormImage;
        BitmapImage auxDiffImage;
        BitmapImage auxColDiffImage;

        BitmapImage smoothNormImage;
        BitmapImage smoothDiffImage;
        BitmapImage smoothColDiffImage;

        NormGenerator generatorDiff;
        NormGenerator generatorAux;
        NormGenerator generatorSmooth;

        private string diffText;
        private string auxText;
        private string smoothText;

        private List<ImagePreviewSelection> previewOptions = new List<ImagePreviewSelection>();

        private List<ChannelSelection> channelOptions = new List<ChannelSelection>();

        public TextureBaseMapCreation()
        {
            previewOptions.Add(ImagePreviewSelection.DIFFUSE);
            previewOptions.Add(ImagePreviewSelection.AUX);
            previewOptions.Add(ImagePreviewSelection.SMOOTH);
            previewOptions.Add(ImagePreviewSelection.MASK);
            previewOptions.Add(ImagePreviewSelection.DIFFUSE_NORM);
            previewOptions.Add(ImagePreviewSelection.DIFFUSE_DIFFERENCE);
            previewOptions.Add(ImagePreviewSelection.DIFFUSE_COLOR_DIFFERENCE);
            previewOptions.Add(ImagePreviewSelection.AUX_NORM);
            previewOptions.Add(ImagePreviewSelection.AUX_DIFFERENCE);
            previewOptions.Add(ImagePreviewSelection.AUX_COLOR_DIFFERENCE);
            previewOptions.Add(ImagePreviewSelection.SMOOTH_NORM);
            previewOptions.Add(ImagePreviewSelection.SMOOTH_DIFFERENCE);
            previewOptions.Add(ImagePreviewSelection.SMOOTH_COLOR_DIFFERENCE);
            channelOptions.Add(ChannelSelection.R);
            channelOptions.Add(ChannelSelection.G);
            channelOptions.Add(ChannelSelection.B);
            channelOptions.Add(ChannelSelection.A);
            channelOptions.Add(ChannelSelection.RGB);

            InitializeComponent();

            PreviewSelectionComboBox.ItemsSource = previewOptions;
            PreviewSelectionComboBox.SelectedIndex = 0;
            PreviewSelectionComboBox.SelectionChanged += PreviewTypeSelected;

            DiffuseChannelComboBox.ItemsSource = channelOptions;
            DiffuseChannelComboBox.SelectedIndex = 4;

            AuxChannelComboBox.ItemsSource = channelOptions;
            AuxChannelComboBox.SelectedIndex = 0;

            SmoothChannelComboBox.ItemsSource = channelOptions;
            SmoothChannelComboBox.SelectedIndex = 3;
        }

        private void PreviewTypeSelected(object sender, SelectionChangedEventArgs e)
        {
            updatePreview();
        }

        private void SelectBaseMapClik(object sender, RoutedEventArgs e)
        {
            string img1 = ImageTools.openFileSelectDialog("Select a PNG base image");
            DiffFileBox.Text = img1;
            diffuseMap = new DirectBitmap(new Bitmap(System.Drawing.Image.FromFile(img1)));
            diffuseImage = ImageTools.BitmapToBitmapImage(diffuseMap.Bitmap);
            updatePreview();
        }

        private void SelectSpecClick(object sender, RoutedEventArgs e)
        {
            string img2 = ImageTools.openFileSelectDialog("Select a PNG base image");
            SpecFileBox.Text = img2;
            auxMap = new DirectBitmap(new Bitmap(System.Drawing.Image.FromFile(img2)));
            auxImage = ImageTools.BitmapToBitmapImage(auxMap.Bitmap);
            updatePreview();
        }

        private void SelectSmoothClick(object sender, RoutedEventArgs e)
        {
            string img3 = ImageTools.openFileSelectDialog("Select a PNG base image");
            SmoothFileBox.Text = img3;
            smoothMap = new DirectBitmap(new Bitmap(System.Drawing.Image.FromFile(img3)));
            smoothImage = ImageTools.BitmapToBitmapImage(auxMap.Bitmap);
            updatePreview();
        }

        private void SelectMaskClick(object sender, RoutedEventArgs e)
        {
            string img4 = ImageTools.openFileSelectDialog("Select a PNG mask image");
            MaskFileBox.Text = img4;
            maskMap = new DirectBitmap(new Bitmap(System.Drawing.Image.FromFile(img4)));
            maskImage = ImageTools.BitmapToBitmapImage(maskMap.Bitmap);
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

        private void generateOutput()
        {
            if (maskMap == null) { return; }
            a = (ChannelSelection)DiffuseChannelComboBox.SelectedItem;
            b = (ChannelSelection)AuxChannelComboBox.SelectedItem;
            c = (ChannelSelection)SmoothChannelComboBox.SelectedItem;
            ProgressWindow window = new ProgressWindow();
            window.start(generatationSequence, generateFinished);
        }

        ChannelSelection a, b, c;

        private void generatationSequence(object sender, DoWorkEventArgs doWork)
        {
            Debug.WriteLine("Starting workers!");
            int valid = 0;
            valid += diffuseMap == null ? 0 : 1;
            valid += auxMap == null ? 0 : 1;
            valid += smoothMap == null ? 0 : 1;
            if (valid == 0) { valid = 1; }

            double offset = 100 / valid;
            double div = 1 * valid;

            valid = 0;
            if (diffuseMap != null)
            {
                Debug.WriteLine("Constructing diffuse worker!");
                generatorDiff = new NormGenerator(diffuseMap, maskMap, a, new NormParams(), valid * offset, div);
                Debug.WriteLine("Starting diffuse worker!");
                generatorDiff.generate(sender, doWork);
                valid++;
            }            

            if (auxMap != null)
            {
                Debug.WriteLine("Starting aux worker!");
                generatorAux = new NormGenerator(auxMap, maskMap, b, new NormParams(), valid * offset, div);
                generatorAux.generate(sender, doWork);
                valid++;
            }

            if (smoothMap != null)
            {
                Debug.WriteLine("Starting smooth worker!");
                generatorSmooth = new NormGenerator(smoothMap, maskMap, c, new NormParams(), valid * offset, div);
                generatorSmooth.generate(sender, doWork);
                valid++;
            }
        }

        private void generateFinished()
        {
            Debug.WriteLine("Generation finished!");

            diffText = string.Empty;
            auxText = string.Empty;
            smoothText = string.Empty;

            if (generatorDiff != null)
            {
                diffuseNormMap = generatorDiff.dest;
                diffuseDiffMap = generatorDiff.difference;
                diffuseColDiffMap = generatorDiff.coloredDiff;
                diffuseNormImage = ImageTools.BitmapToBitmapImage(diffuseNormMap.Bitmap);
                diffuseDiffImage = ImageTools.BitmapToBitmapImage(diffuseDiffMap.Bitmap);
                diffuseColDiffImage = ImageTools.BitmapToBitmapImage(diffuseColDiffMap.Bitmap);
                diffText = generatorDiff.outText;
            }

            if (generatorAux != null)
            {
                auxNormMap = generatorAux.dest;
                auxDiffMap = generatorAux.difference;
                auxColDiffMap = generatorAux.coloredDiff;
                auxNormImage = ImageTools.BitmapToBitmapImage(auxNormMap.Bitmap);
                auxDiffImage = ImageTools.BitmapToBitmapImage(auxDiffMap.Bitmap);
                auxColDiffImage = ImageTools.BitmapToBitmapImage(auxColDiffMap.Bitmap);
                auxText = generatorAux.outText;
            }

            if (generatorSmooth != null)
            {
                smoothNormMap = generatorSmooth.dest;
                smoothDiffMap = generatorSmooth.difference;
                smoothColDiffMap = generatorSmooth.coloredDiff;
                smoothNormImage = ImageTools.BitmapToBitmapImage(smoothNormMap.Bitmap);
                smoothDiffImage = ImageTools.BitmapToBitmapImage(smoothDiffMap.Bitmap);
                smoothColDiffImage = ImageTools.BitmapToBitmapImage(smoothColDiffMap.Bitmap);
                smoothText = generatorSmooth.outText;
            }

            generatorDiff = null;
            generatorAux = null;
            generatorSmooth = null;
            updatePreview();
        }

        private void GetNormTextClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(diffText) && string.IsNullOrEmpty(auxText) && string.IsNullOrEmpty(smoothText)) { return; }
            NormalizationDataWindow window = new NormalizationDataWindow();
            window.setup(diffText, auxText, smoothText);
            window.ShowDialog();
        }

        private void updatePreview()
        {
            ImagePreviewSelection previewSelection = (ImagePreviewSelection)PreviewSelectionComboBox.SelectedItem;
            switch (previewSelection)
            {
                case ImagePreviewSelection.DIFFUSE:
                    MapImage.Source = diffuseImage;
                    break;
                case ImagePreviewSelection.AUX:
                    MapImage.Source = auxImage;
                    break;
                case ImagePreviewSelection.SMOOTH:
                    MapImage.Source = smoothImage;
                    break;
                case ImagePreviewSelection.MASK:
                    MapImage.Source = maskImage;
                    break;
                case ImagePreviewSelection.DIFFUSE_NORM:
                    MapImage.Source = diffuseNormImage;
                    break;
                case ImagePreviewSelection.DIFFUSE_DIFFERENCE:
                    MapImage.Source = diffuseDiffImage;
                    break;
                case ImagePreviewSelection.DIFFUSE_COLOR_DIFFERENCE:
                    MapImage.Source = diffuseColDiffImage;
                    break;
                case ImagePreviewSelection.AUX_NORM:
                    MapImage.Source = auxNormImage;
                    break;
                case ImagePreviewSelection.AUX_DIFFERENCE:
                    MapImage.Source = auxDiffImage;
                    break;
                case ImagePreviewSelection.AUX_COLOR_DIFFERENCE:
                    MapImage.Source = auxColDiffImage;
                    break;
                case ImagePreviewSelection.SMOOTH_NORM:
                    MapImage.Source = smoothNormImage;
                    break;
                case ImagePreviewSelection.SMOOTH_DIFFERENCE:
                    MapImage.Source = smoothDiffImage;
                    break;
                case ImagePreviewSelection.SMOOTH_COLOR_DIFFERENCE:
                    MapImage.Source = smoothColDiffImage;
                    break;
                default:
                    MapImage.Source = null;
                    break;
            }
        }
        
        private void ExportClick(object sender, RoutedEventArgs e)
        {
            if (diffuseNormMap == null) { return; }
            string dest = ImageTools.openFileSaveDialog("Save Image");
            if (!string.IsNullOrEmpty(dest))
            {
                diffuseNormMap.Bitmap.Save(dest);
            }
        }

    }

    public class NormGenerator
    {

        private DirectBitmap src;
        private DirectBitmap mask;
        public DirectBitmap dest;
        public DirectBitmap difference;
        public DirectBitmap coloredDiff;
        private NormParams parameters;
        ChannelSelection sourceChannel;
        private double workDiv;
        private double workStart;

        private double sumR =0, sumG = 0, sumB = 0;
        private double countR = 0, countG = 0, countB = 0;
        private double minR = 1, minG = 1, minB = 1;
        private double maxR = 0, maxG = 0, maxB = 0;

        public double outR, outG, outB;

        public string outText;

        public NormGenerator(DirectBitmap source, DirectBitmap mask, ChannelSelection sourceChannel, NormParams opts, double workStart, double workDivisor)
        {
            Debug.WriteLine("Worker constructor enter");
            this.src = source;
            this.mask = mask;
            this.parameters = opts;
            this.sourceChannel = sourceChannel;
            this.workStart = workStart;
            this.workDiv = workDivisor;
            Debug.WriteLine("Worker constructor exit");
        }

        public void generate(object sender, DoWorkEventArgs work)
        {
            Debug.WriteLine("Worker function enter");
            System.Drawing.Color srcColor, maskColor;
            int width = src.Width;
            int height = src.Height;
            double lum, mr, mg, mb;

            //progress updating vars
            double totalPixels = width * height * 3;
            double processedPixels = 0;
            double progress = 0;
            double prevProg = 0;

            //difference mask vars

            Debug.WriteLine("Worker first loop start");
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
                    lum = getChannelLuminance(srcColor);
                    channelCalc(lum, mr, ref countR, ref sumR, ref minR, ref maxR);
                    channelCalc(lum, mg, ref countG, ref sumG, ref minG, ref maxG);
                    channelCalc(lum, mb, ref countB, ref sumB, ref minB, ref maxB);
                    processedPixels++;
                    progress = workStart + ((processedPixels / totalPixels) * 100d) / workDiv;
                    if (progress - prevProg > 1)
                    {
                        prevProg = progress;
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

            Debug.WriteLine("Worker second loop start");
            //third pass -- write output
            dest = new DirectBitmap(src.Width, src.Height);
            difference = new DirectBitmap(src.Width, src.Height);
            coloredDiff = new DirectBitmap(src.Width, src.Height);
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
                    lum = getChannelLuminance(srcColor);
                    
                    difference.SetPixel(x, y, getDiffColor(pix, lum, false));
                    coloredDiff.SetPixel(x, y, getDiffColor(pix, lum, true));

                    //double op in this loop, count twice for progress reporting
                    processedPixels++;
                    processedPixels++;
                    progress = workStart + ((processedPixels / totalPixels) * 100d) / workDiv;
                    if (progress - prevProg > 1)
                    {
                        prevProg = progress;
                        ((BackgroundWorker)sender).ReportProgress((int)progress);
                    }
                }
            }
            outText = outR + "," + outG + "," + outB;
            Debug.WriteLine("Worker function exit");
        }

        private void getPixelColors(System.Drawing.Color color, out double r, out double g, out double b)
        {
            r = color.R / 255d;
            g = color.G / 255d;
            b = color.B / 255d;
        }

        private double getChannelLuminance(System.Drawing.Color color)
        {
            return ImageTools.getChannelSelection(color, sourceChannel);
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

        private void channelCalc(double lum, double maskVal, ref double count, ref double sum, ref double min, ref double max)
        {
            if (maskVal <= 0) { return; }
            double adjSrc = lum * maskVal;
            count += maskVal;
            sum += adjSrc;
            if (adjSrc < min) { min = adjSrc; }
            if (adjSrc > max) { max = adjSrc; }
        }

        private double getLuminosity(double r, double g, double b)
        {
            return r * 0.22f + g * 0.707f + b * 0.071f;
        }

        private System.Drawing.Color getDiffColor(double outPix, double lum, bool colored)
        {
            double r=0, g=0, b=0;
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
