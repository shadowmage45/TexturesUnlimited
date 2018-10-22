using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexturesUnlimitedTools
{
    public static class ImageTools
    {
        public static Bitmap loadImage(string fileName)
        {
            Image image = Image.FromFile(fileName);
            return new Bitmap(image);
        }

        public static void saveImage(Bitmap image, string fileName)
        {
            image.Save(fileName);
        }
    }
}
