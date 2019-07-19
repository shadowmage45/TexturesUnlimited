using System;
using System.Collections.Generic;
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
    /// Interaction logic for NormalizationDataWindow.xaml
    /// </summary>
    public partial class NormalizationDataWindow : Window
    {

        string diffText;
        string auxText;
        string smoothText;

        public NormalizationDataWindow()
        {
            InitializeComponent();
        }

        public void setup(string diffText, string auxText, string smoothText)
        {
            this.diffText = diffText;
            this.auxText = auxText;
            this.smoothText = smoothText;
            setupText();
        }

        private void setupText()
        {
            bool metal = true;
            StringBuilder builder = new StringBuilder();
            builder.Append("    ").Append("vector = _DiffuseNorm,").AppendLine(diffText);
            if (metal)
            {
                builder.Append("    ").Append("vector = _MetalNorm,").AppendLine(auxText);
            }
            else
            {
                builder.Append("    ").Append("vector = _SpecularNorm,").AppendLine(auxText);
            }
            builder.Append("    ").Append("vector = _SmoothnessNorm,").AppendLine(smoothText);
            NormTextBlock.Text = builder.ToString();
        }

    }
}
