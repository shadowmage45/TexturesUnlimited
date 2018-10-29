using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {

        private Action onFinish;

        public ProgressWindow()
        {
            InitializeComponent();
        }

        public void start(DoWorkEventHandler work, Action onFinish)
        {
            this.onFinish = onFinish;
            ImageTools.startWorker(work, progressChanged, close);
            this.ShowDialog();
        }

        public void progressChanged(object sender, ProgressChangedEventArgs e)
        {
            Debug.WriteLine("Worker Progress Update: "+e.ProgressPercentage);
            ProgressBar.Value = e.ProgressPercentage;
            ProgressLabel.Content = e.ProgressPercentage;
        }

        private void close(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug.WriteLine("Worker Close!");
            onFinish?.Invoke();
            this.Close();
        }

    }
}
