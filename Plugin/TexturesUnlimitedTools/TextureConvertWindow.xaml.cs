using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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
        }

        private void SelectImagesClick(object sender, RoutedEventArgs e)
        {
            string img1 = ImageTools.openFileSelectDialog("Select a PNG image");
            TextureConversionEntry entry = new TextureConversionEntry();
            entry.ImageName = img1;
            MainWindow.instance.ConvertRecords.Add(entry);
        }

        private void ConvertImagesClick(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory("output");
            System.Windows.MessageBox.Show("All textures will be output to the /output folder");
            foreach (TextureConversionEntry entry in MainWindow.instance.ConvertRecords)
            {
                string inputName = entry.ImageName;                
                string outputName = "output/" + inputName.Substring(inputName.LastIndexOf("\\") + 1);//output/xxx.png
                outputName = outputName.Substring(0, outputName.Length - 3) + ".dds";
                Process process = new Process();
                process.StartInfo.FileName = "nvdxt.exe";
                process.StartInfo.Arguments = getDDSCommand(inputName, outputName, (int)entry.Format);
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
}
