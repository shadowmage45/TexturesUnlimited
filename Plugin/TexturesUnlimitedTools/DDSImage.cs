using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexturesUnlimitedTools
{

    /*
        
        DDS Image Format Data for 2D textures
        Byte layout:
        Header = 128b
        Optional Header = 20b
        Mip0 = XXX
        Mip1 = XXX

    */

    public class DDSImage
    {

        DDSFormat format;
        byte[] byteData;

        public DDSImage() { }

        public void Load(byte[] bytes)
        {

        }

    }

    struct DDS_HEADER
    {
        uint dwSize;
        uint dwFlags;
        uint dwHeight;
        uint dwWidth;
        uint dwPitchOrLinearSize;
        uint dwDepth;
        uint dwMipMapCount;
        uint[] dwReserved1;//should be arr.length = 11
        DDS_PIXELFORMAT ddspf;
        uint dwCaps;
        uint dwCaps2;
        uint dwCaps3;
        uint dwCaps4;
        uint dwReserved2;
    }

    struct DDS_PIXELFORMAT
    {
        uint dwSize;
        uint dwFlags;
        uint dwFourCC;
        uint dwRGBBitCount;
        uint dwRBitMask;
        uint dwGBitMask;
        uint dwBBitMask;
        uint dwABitMask;
    }

}
