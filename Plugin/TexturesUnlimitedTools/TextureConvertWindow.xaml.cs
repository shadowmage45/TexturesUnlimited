using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TexturesUnlimitedTools
{
    /// <summary>
    /// Interaction logic for TextureConvertWindow.xaml
    /// </summary>
    public partial class TextureConvertWindow : Window
    {

        public TextureConvertWindow()
        {
            InitializeComponent();
            DataGridComboBoxColumn column = RecordGrid.Columns[1] as DataGridComboBoxColumn;
            column.ItemsSource = MainWindow.instance.DDSOptions;
            column.SelectedValueBinding = new Binding("Format");
        }

        private void SelectImagesClick(object sender, RoutedEventArgs e)
        {
            string[] imgs = ImageTools.openFileMultiSelectDialog("Select a PNG image");
            foreach (string img in imgs)
            {
                if (string.IsNullOrEmpty(img)) { continue; }
                TextureConversionEntry entry = new TextureConversionEntry();
                entry.ImageName = img;
                MainWindow.instance.ConvertRecords.Add(entry);
            }
        }

        private void ConvertImagesClick(object sender, RoutedEventArgs e)
        {
            string outputPath = "output/";
            outputPath = ImageTools.openDirectorySelectDialog("Conversion Export Folder");
            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
            {
                return;
            }            
            foreach (TextureConversionEntry entry in MainWindow.instance.ConvertRecords)
            {
                string inputName = entry.ImageName;                
                string outputName = outputPath + "/" + inputName.Substring(inputName.LastIndexOf("\\") + 1);//output/xxx.png
                outputName = outputName.Substring(0, outputName.Length - 4) + ".dds";
                Process process = new Process();
                process.StartInfo.FileName = "nvdxt.exe";
                int format = entry.Format == "DXT1" ? 1 : entry.Format == "DXT5" ? 5 : 6;
                process.StartInfo.Arguments = getDDSCommand(inputName, outputName, format);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.Start();
                process.WaitForExit();
                process.Close();
            }
            MainWindow.instance.ConvertRecords.Clear();
        }

        private string getDDSCommand(string inputFile, string outputFile, int format)
        {
            StringBuilder builder = new StringBuilder();
            //input file
            builder.Append("-file \"").Append(inputFile + "\" ");
            //output dir
            builder.Append("-output \"").Append(outputFile+"\" ");
            //filter mode - triangle; same as used by other ksp-related dds conversion utils; unsure what it does?
            builder.Append("-Triangle ");
            //specify to flip images (hopefully this is on Y?)
            builder.Append("-flip ");
            //specify highest quality
            builder.Append("-quality_highest ");
            string formatString = format == 6 ? "5nm" : format.ToString();
            //output format
            builder.Append("-dxt").Append(formatString);
            MessageBox.Show("NVDXT Command: " + builder.ToString());
            return builder.ToString();
        }

    }

    public class TextureConversionEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        string imageName;
        string format = "DXT1";

        public string ImageName { get { return imageName; } set { imageName = value; propChanged(); } }
        public string Format { get { return format; } set { format = value; propChanged(); } }

        private void propChanged([CallerMemberName]string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
