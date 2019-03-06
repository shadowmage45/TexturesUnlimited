using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TexturesUnlimitedTools
{

    public class ViewModelTextureCombine : ViewModelBase
    {

        /// <summary>
        /// Static selection lists for use by Data Grid Combo Box columns
        /// </summary>
        public static ObservableCollection<ImageFormat> ImageFormats { get; } = new ObservableCollection<ImageFormat>();
        public static ObservableCollection<ImageChannelSelection> ChannelSelections { get; } = new ObservableCollection<ImageChannelSelection>();
        
        /// <summary>
        /// Static constructor to fill in the combo-box selection lists, which must be static for some odd reason
        /// </summary>
        static ViewModelTextureCombine()
        {
            ImageFormats.Add(ImageFormat.PNG);
            ImageFormats.Add(ImageFormat.DXT1);
            ImageFormats.Add(ImageFormat.DXT5);
            ImageFormats.Add(ImageFormat.DXT5nm);

            ChannelSelections.Add(ImageChannelSelection.Image1_R);
            ChannelSelections.Add(ImageChannelSelection.Image1_G);
            ChannelSelections.Add(ImageChannelSelection.Image1_B);
            ChannelSelections.Add(ImageChannelSelection.Image1_A);
            ChannelSelections.Add(ImageChannelSelection.Image1_RGB);
            ChannelSelections.Add(ImageChannelSelection.Image2_R);
            ChannelSelections.Add(ImageChannelSelection.Image2_B);
            ChannelSelections.Add(ImageChannelSelection.Image2_G);
            ChannelSelections.Add(ImageChannelSelection.Image2_A);
            ChannelSelections.Add(ImageChannelSelection.Image2_RGB);
        }

        public ObservableCollection<TextureCombineEntry> TextureEntries { get; } = new ObservableCollection<TextureCombineEntry>();
        public ObservableCollection<TextureCombineEntry> SelectedEntries { get; } = new ObservableCollection<TextureCombineEntry>();

        private string exportFolderPath = "/";
        public string ExportFolderPath { get { return exportFolderPath; } set { exportFolderPath = value; OnPropertyChanged(); ConvertBatchCommand.FireCanExecuteChanged(); } }

        public CommandBase<object> AddImageCommand { get; private set; }
        public CommandBase<object> DeleteImageCommand { get; private set; }
        public CommandBase<object> SelectFolderCommand { get; private set; }
        public CommandBase<object> ConvertBatchCommand { get; private set; }
        public CommandBase<object> SetSelectedToAOCombineCommand { get; private set; }

        public ViewModelTextureCombine()
        {
            loadCommands();
        }

        private void loadCommands()
        {
            Predicate<object> canAddImage  = (a) => true;
            Action<object> addImage = (a) => 
            {
                string img1 = ImageTools.openFileSelectDialog("Primary Texture");
                string img2 = ImageTools.openFileSelectDialog("Second Texture");
                TextureCombineEntry entry = new TextureCombineEntry();
                entry.Image1Path = img1;
                entry.Image2Path = img2;
                TextureEntries.Add(entry);
                ConvertBatchCommand.FireCanExecuteChanged();
            };
            AddImageCommand = new CommandBase<object>(canAddImage, addImage);

            Predicate<object> canDeleteImage = (a) => SelectedEntries.Count > 0;
            Action<object> deleteImage = (a) => 
            {
                ConvertBatchCommand.FireCanExecuteChanged();
            };
            DeleteImageCommand = new CommandBase<object>(canDeleteImage, deleteImage);

            Predicate<object> canSelectFolder = (a) => true;
            Action<object> selectFolder = (a) => 
            {
                string outputPath = "output/";
                outputPath = ImageTools.openDirectorySelectDialog("Conversion Export Folder");
                ExportFolderPath = outputPath;
                ConvertBatchCommand.FireCanExecuteChanged();
            };
            SelectFolderCommand = new CommandBase<object>(canSelectFolder, selectFolder);

            Predicate<object> canConvertbatch = (a) => true;// TextureEntries.Count > 0 && !string.IsNullOrEmpty(exportFolderPath);
            Action<object> convertBatch = (a) => 
            {
                foreach (TextureCombineEntry entry in TextureEntries) { entry.performConversion(ExportFolderPath); }
            };
            ConvertBatchCommand = new CommandBase<object>(canConvertbatch, convertBatch);

            Predicate<object> canSetToAO = (a) => true;
            Action<object> setToAO = (a) => { };
            SetSelectedToAOCombineCommand = new CommandBase<object>(canSetToAO, setToAO);
        }

    }

    public class TextureCombineEntry : ViewModelBase
    {

        private string image1path;
        private string image2path;
        private ImageChannelSelection outRChannel = ImageChannelSelection.Image1_R;
        private ImageChannelSelection outGChannel = ImageChannelSelection.Image1_G;
        private ImageChannelSelection outBChannel = ImageChannelSelection.Image1_B;
        private ImageChannelSelection outAChannel = ImageChannelSelection.Image1_A;
        private ImageFormat outputFormat;

        private bool selected;

        public string Image1Path { get { return image1path; } set { image1path = value; OnPropertyChanged(); } }
        public string Image2Path { get { return image2path; } set { image2path = value; OnPropertyChanged(); } }
        public ImageChannelSelection OutRChannel { get { return outRChannel; } set { outRChannel = value; OnPropertyChanged(); } }
        public ImageChannelSelection OutGChannel { get { return outGChannel; } set { outGChannel = value; OnPropertyChanged(); } }
        public ImageChannelSelection OutBChannel { get { return outBChannel; } set { outBChannel = value; OnPropertyChanged(); } }
        public ImageChannelSelection OutAChannel { get { return outAChannel; } set { outAChannel = value; OnPropertyChanged(); } }
        public ImageFormat OutputFormat { get { return outputFormat; } set { outputFormat = value; OnPropertyChanged(); } }

        public bool Selected { get { return selected; } set { selected = value; } }

        public void performConversion(string exportFolder)
        {
            Bitmap image1 = ImageTools.loadImage(Image1Path);
            if (image1 == null)
            {
                MessageBox.Show("Cannot process pair; first image is null");
                return;
            }
            int width = image1.Width;
            int height = image1.Height;
            Bitmap image2 = ImageTools.loadImage(Image2Path);
            if (image2 != null)
            {
                if (image2.Width != width || image2.Height != height)
                {
                    image1.Dispose();
                    image2.Dispose();
                    MessageBox.Show("Cannot convert images, they must be the same width and height");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Cannot process pair; second image is null");
                image1.Dispose();
                return;
            }
            Bitmap output = new Bitmap(width, height);
            Color color1, color2;
            byte r, g, b, a;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    color1 = image1.GetPixel(x, y);
                    color2 = image2 == null ? Color.White : image2.GetPixel(x, y);
                    r = ImageTools.getChannelSelection(color1, color2, OutRChannel);
                    g = ImageTools.getChannelSelection(color1, color2, OutGChannel);
                    b = ImageTools.getChannelSelection(color1, color2, OutBChannel);
                    a = ImageTools.getChannelSelection(color1, color2, OutAChannel);
                    output.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                }
            }
            if (image1 != null) { image1.Dispose(); }
            if (image2 != null) { image2.Dispose(); }
            string fileName = System.IO.Path.GetFileName(Image1Path);//get just the filename from the image
            string exportFilePath = exportFolder + System.IO.Path.DirectorySeparatorChar + fileName;
            //TODO -- display error message if the output destination file already exists, prompt for overwrite?

            output.Save(exportFilePath);//append to the export path for save operations
            output.Dispose();

            //if something other than PNG was specified for the output,
            //convert it using NVDXT tools to the specified format and delete the PNG
            if (OutputFormat != ImageFormat.PNG)
            {

                string inputPath = exportFilePath;
                string outputPath = inputPath.Substring(0, inputPath.LastIndexOf('.')) + ".dds";//strip file extension from the input, replace with '.dds'
                Process process = new Process();
                process.StartInfo.FileName = "nvdxt.exe";
                int format = OutputFormat==ImageFormat.DXT1 ? 1 : OutputFormat == ImageFormat.DXT5 ? 5 : 6;
                process.StartInfo.Arguments = getDDSCommand(inputPath, outputPath, format);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                process.WaitForExit();
                process.Close();

                System.IO.File.Delete(exportFilePath);//remove the combined PNG, leave the converted DDS
            }

        }

        private string getDDSCommand(string inputFile, string outputFile, int format)
        {
            StringBuilder builder = new StringBuilder();
            //input file
            builder.Append("-file \"").Append(inputFile + "\" ");
            //output dir
            builder.Append("-output \"").Append(outputFile + "\" ");
            //filter mode - triangle; same as used by other ksp-related dds conversion utils; unsure what it does?
            builder.Append("-Triangle ");
            //specify to flip images (hopefully this is on Y?)
            builder.Append("-flip ");
            //specify highest quality
            builder.Append("-quality_highest ");
            string formatString = format == 6 ? "5nm" : format.ToString();
            //output format
            builder.Append("-dxt").Append(formatString);
            //MessageBox.Show("NVDXT Command: " + builder.ToString());
            return builder.ToString();
        }

    }

    public class ViewModelBase : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }

    public enum ImageFormat
    {
        PNG,
        DXT1,
        DXT5,
        DXT5nm
    }

    public class CommandBase<T> : ICommand
    {

        private Predicate<T> canExecute;
        private Action<T> execute;

        public CommandBase(Predicate<T> canExecute, Action<T> execute)
        {
            this.canExecute = canExecute;
            this.execute = execute;
        }

        public event EventHandler CanExecuteChanged;

        public void FireCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public bool CanExecute(object parameter)
        {
            return canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            execute((T)parameter);
        }

    }

}
