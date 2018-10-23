using System.Diagnostics;
using System.IO;
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
            string img1 = ImageTools.openFileSelectDialog("Select a DDS image");
            if (!img1.EndsWith(".dds", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                System.Windows.MessageBox.Show("You must select a DDS texture");
                return;
            }
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
                string outputName = "output/" + inputName.Substring(inputName.LastIndexOf("/") + 1);//output/xxx.png
                
                outputName = outputName.Substring(0, outputName.Length - 3) + ".dds";
                Process process = new Process();
                process.StartInfo.FileName = "SSTUUtilDDS.exe";
                process.StartInfo.Arguments = "\""+inputName+"\" \""+outputName+"\" 5";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.Start();
                process.WaitForExit();
                process.Close();
            }

            MainWindow.instance.ConvertRecords.Clear();
        }
               

    }
}
