using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
using PixelColor = System.Drawing.Color;

namespace TexturesUnlimitedTools
{
    public partial class TextureMaskingWindow : Window
    {

        //enum list stashes, for combobox dropdown use
        private List<ImagePreviewSelection> previewOptions = new List<ImagePreviewSelection>();

        //raw bitmaps of input textures
        DirectBitmap diffuseMap;//diffuse input
        DirectBitmap auxMap;//metal/specular input
        DirectBitmap smoothMap;//metal/specular input
        DirectBitmap maskMap;//mask

        //raw bitmaps of generated/output textures
        DirectBitmap diffuseColDiffMap;
        DirectBitmap auxColDiffMap;
        DirectBitmap smoothColDiffMap;

        //WPF BitmapImage equivalent Bitmap's that are above
        BitmapImage diffuseImage;
        BitmapImage auxImage;
        BitmapImage smoothImage;
        BitmapImage maskImage;
        
        BitmapImage diffuseColDiffImage;        
        BitmapImage auxColDiffImage;        
        BitmapImage smoothColDiffImage;

        /**
         * User recoloring value storage
         **/
        PixelColor mainColor;
        PixelColor secondColor;
        PixelColor detailColor;

        float mainAux;
        float secondAux;
        float detailAux;

        float mainSmooth;
        float secondSmooth;
        float detailSmooth;

        /**
         * Recoloring 'workers' -- these generate the output textures from the input textures + recoloring params
         **/
        TextureRecolor diffRecolor;
        TextureRecolor auxRecolor;
        TextureRecolor smoothRecolor;
                
        public TextureMaskingWindow()
        {
            previewOptions.Add(ImagePreviewSelection.DIFFUSE);
            previewOptions.Add(ImagePreviewSelection.AUX);
            previewOptions.Add(ImagePreviewSelection.SMOOTH);
            previewOptions.Add(ImagePreviewSelection.MASK);
            previewOptions.Add(ImagePreviewSelection.DIFFUSE_COLOR_DIFFERENCE);
            previewOptions.Add(ImagePreviewSelection.AUX_COLOR_DIFFERENCE);
            previewOptions.Add(ImagePreviewSelection.SMOOTH_COLOR_DIFFERENCE);

            InitializeComponent();

            PreviewSelectionComboBox.ItemsSource = previewOptions;
            PreviewSelectionComboBox.SelectedIndex = 0;
            PreviewSelectionComboBox.SelectionChanged += PreviewTypeSelected;
                        
        }
        
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            //release any resources that might be hanging out
            diffRecolor = null;
            auxRecolor = null;
            smoothRecolor = null;

            //dispose of all of the Bitmaps used for processing
            diffuseColDiffMap?.Dispose();
            diffuseColDiffMap = null;
            
            auxColDiffMap?.Dispose();
            auxColDiffMap = null;
            
            smoothColDiffMap?.Dispose();
            smoothColDiffMap = null;

            //and all of the bitmaps used for loading
            diffuseMap?.Dispose();//diffuse input
            auxMap?.Dispose();//metal/specular input
            smoothMap?.Dispose();//metal/specular input
            maskMap?.Dispose();//mask

            diffuseMap = null;//diffuse input
            auxMap = null;//metal/specular input
            smoothMap = null;//metal/specular input
            maskMap = null;//mask

            //WPF image controls have no dispose method...

            //trigger GC just to clean up as much as possible
            System.GC.Collect();
        }

        private void PreviewTypeSelected(object sender, SelectionChangedEventArgs e)
        {
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
            ProgressWindow window = new ProgressWindow();
            window.start(generatationSequence, generateFinished);
        }

        private void generatationSequence(object sender, DoWorkEventArgs doWork)
        {
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
                //generatorDiff = new NormGenerator(diffuseMap, maskMap, diffInputChannels, new NormParams(), valid * offset, div);
                //generatorDiff.generate(sender, doWork);
                valid++;
            }

            if (auxMap != null)
            {
                //generatorAux = new NormGenerator(auxMap, maskMap, auxInputChannels, new NormParams(), valid * offset, div);
                //generatorAux.generate(sender, doWork);
                valid++;
            }

            if (smoothMap != null)
            {
                //generatorSmooth = new NormGenerator(smoothMap, maskMap, smoothInputChannels, new NormParams(), valid * offset, div);
                //generatorSmooth.generate(sender, doWork);
                valid++;
            }
        }

        private void generateFinished()
        {

            if (diffRecolor != null)
            {
                //diffuseColDiffMap = (DirectBitmap)generatorDiff.coloredDiff;
                //diffuseColDiffImage = ImageTools.BitmapToBitmapImage(diffuseColDiffMap.Bitmap);
            }

            if (auxRecolor != null)
            {
                //auxColDiffMap = (DirectBitmap)generatorAux.coloredDiff;
                //auxColDiffImage = ImageTools.BitmapToBitmapImage(auxColDiffMap.Bitmap);
            }

            if (smoothRecolor != null)
            {
                //smoothColDiffMap = (DirectBitmap)generatorSmooth.coloredDiff;
                //smoothColDiffImage = ImageTools.BitmapToBitmapImage(smoothColDiffMap.Bitmap);
            }

            diffRecolor = null;
            auxRecolor = null;
            smoothRecolor = null;

            //dispose of all of the Bitmaps used for processing
            diffuseColDiffMap?.Dispose();
            diffuseColDiffMap = null;
            
            auxColDiffMap?.Dispose();
            auxColDiffMap = null;
            
            smoothColDiffMap?.Dispose();
            smoothColDiffMap = null;

            updatePreview();
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
                case ImagePreviewSelection.DIFFUSE_COLOR_DIFFERENCE:
                    MapImage.Source = diffuseColDiffImage;
                    break;
                case ImagePreviewSelection.AUX_COLOR_DIFFERENCE:
                    MapImage.Source = auxColDiffImage;
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

        }

    }

    public class TextureRecolor
    {

    }

}
