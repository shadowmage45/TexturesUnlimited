//XNA - DXT Utility class
//originally from:
//https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Graphics/DxtUtil.cs
#region License
// 
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
// 
// 
#endregion License
using System;
using System.IO;

//modified to place into TU namespace, for access to internal methods
namespace TexturesUnlimitedTools
{
    internal static class DxtUtil
    {
        #region REGION - DXT1 Encoding
        internal static byte[] DecompressDxt1(byte[] imageData, int width, int height)
        {
            using (MemoryStream imageStream = new MemoryStream(imageData))
                return DecompressDxt1(imageStream, width, height);
        }

        internal static byte[] DecompressDxt1(Stream imageStream, int width, int height)
        {
            byte[] imageData = new byte[width * height * 4];

            using (BinaryReader imageReader = new BinaryReader(imageStream))
            {
                int blockCountX = (width + 3) / 4;
                int blockCountY = (height + 3) / 4;

                for (int y = 0; y < blockCountY; y++)
                {
                    for (int x = 0; x < blockCountX; x++)
                    {
                        DecompressDxt1Block(imageReader, x, y, blockCountX, width, height, imageData);
                    }
                }
            }

            return imageData;
        }

        private static void DecompressDxt1Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, byte[] imageData)
        {
            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            byte r0, g0, b0;
            byte r1, g1, b1;
            ConvertRgb565ToRgb888(c0, out r0, out g0, out b0);
            ConvertRgb565ToRgb888(c1, out r1, out g1, out b1);

            uint lookupTable = imageReader.ReadUInt32();

            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 255;
                    uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                    if (c0 > c1)
                    {
                        switch (index)
                        {
                            case 0:
                                r = r0;
                                g = g0;
                                b = b0;
                                break;
                            case 1:
                                r = r1;
                                g = g1;
                                b = b1;
                                break;
                            case 2:
                                r = (byte)((2 * r0 + r1) / 3);
                                g = (byte)((2 * g0 + g1) / 3);
                                b = (byte)((2 * b0 + b1) / 3);
                                break;
                            case 3:
                                r = (byte)((r0 + 2 * r1) / 3);
                                g = (byte)((g0 + 2 * g1) / 3);
                                b = (byte)((b0 + 2 * b1) / 3);
                                break;
                        }
                    }
                    else
                    {
                        switch (index)
                        {
                            case 0:
                                r = r0;
                                g = g0;
                                b = b0;
                                break;
                            case 1:
                                r = r1;
                                g = g1;
                                b = b1;
                                break;
                            case 2:
                                r = (byte)((r0 + r1) / 2);
                                g = (byte)((g0 + g1) / 2);
                                b = (byte)((b0 + b1) / 2);
                                break;
                            case 3:
                                r = 0;
                                g = 0;
                                b = 0;
                                a = 0;
                                break;
                        }
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        imageData[offset] = r;
                        imageData[offset + 1] = g;
                        imageData[offset + 2] = b;
                        imageData[offset + 3] = a;
                    }
                }
            }
        }
        #endregion

        #region REGION - DXT3 Encoding
        internal static byte[] DecompressDxt3(byte[] imageData, int width, int height)
        {
            using (MemoryStream imageStream = new MemoryStream(imageData))
                return DecompressDxt3(imageStream, width, height);
        }

        internal static byte[] DecompressDxt3(Stream imageStream, int width, int height)
        {
            byte[] imageData = new byte[width * height * 4];

            using (BinaryReader imageReader = new BinaryReader(imageStream))
            {
                int blockCountX = (width + 3) / 4;
                int blockCountY = (height + 3) / 4;

                for (int y = 0; y < blockCountY; y++)
                {
                    for (int x = 0; x < blockCountX; x++)
                    {
                        DecompressDxt3Block(imageReader, x, y, blockCountX, width, height, imageData);
                    }
                }
            }

            return imageData;
        }

        private static void DecompressDxt3Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, byte[] imageData)
        {
            byte a0 = imageReader.ReadByte();
            byte a1 = imageReader.ReadByte();
            byte a2 = imageReader.ReadByte();
            byte a3 = imageReader.ReadByte();
            byte a4 = imageReader.ReadByte();
            byte a5 = imageReader.ReadByte();
            byte a6 = imageReader.ReadByte();
            byte a7 = imageReader.ReadByte();

            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            byte r0, g0, b0;
            byte r1, g1, b1;
            ConvertRgb565ToRgb888(c0, out r0, out g0, out b0);
            ConvertRgb565ToRgb888(c1, out r1, out g1, out b1);

            uint lookupTable = imageReader.ReadUInt32();

            int alphaIndex = 0;
            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 0;

                    uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                    switch (alphaIndex)
                    {
                        case 0:
                            a = (byte)((a0 & 0x0F) | ((a0 & 0x0F) << 4));
                            break;
                        case 1:
                            a = (byte)((a0 & 0xF0) | ((a0 & 0xF0) >> 4));
                            break;
                        case 2:
                            a = (byte)((a1 & 0x0F) | ((a1 & 0x0F) << 4));
                            break;
                        case 3:
                            a = (byte)((a1 & 0xF0) | ((a1 & 0xF0) >> 4));
                            break;
                        case 4:
                            a = (byte)((a2 & 0x0F) | ((a2 & 0x0F) << 4));
                            break;
                        case 5:
                            a = (byte)((a2 & 0xF0) | ((a2 & 0xF0) >> 4));
                            break;
                        case 6:
                            a = (byte)((a3 & 0x0F) | ((a3 & 0x0F) << 4));
                            break;
                        case 7:
                            a = (byte)((a3 & 0xF0) | ((a3 & 0xF0) >> 4));
                            break;
                        case 8:
                            a = (byte)((a4 & 0x0F) | ((a4 & 0x0F) << 4));
                            break;
                        case 9:
                            a = (byte)((a4 & 0xF0) | ((a4 & 0xF0) >> 4));
                            break;
                        case 10:
                            a = (byte)((a5 & 0x0F) | ((a5 & 0x0F) << 4));
                            break;
                        case 11:
                            a = (byte)((a5 & 0xF0) | ((a5 & 0xF0) >> 4));
                            break;
                        case 12:
                            a = (byte)((a6 & 0x0F) | ((a6 & 0x0F) << 4));
                            break;
                        case 13:
                            a = (byte)((a6 & 0xF0) | ((a6 & 0xF0) >> 4));
                            break;
                        case 14:
                            a = (byte)((a7 & 0x0F) | ((a7 & 0x0F) << 4));
                            break;
                        case 15:
                            a = (byte)((a7 & 0xF0) | ((a7 & 0xF0) >> 4));
                            break;
                    }
                    ++alphaIndex;

                    switch (index)
                    {
                        case 0:
                            r = r0;
                            g = g0;
                            b = b0;
                            break;
                        case 1:
                            r = r1;
                            g = g1;
                            b = b1;
                            break;
                        case 2:
                            r = (byte)((2 * r0 + r1) / 3);
                            g = (byte)((2 * g0 + g1) / 3);
                            b = (byte)((2 * b0 + b1) / 3);
                            break;
                        case 3:
                            r = (byte)((r0 + 2 * r1) / 3);
                            g = (byte)((g0 + 2 * g1) / 3);
                            b = (byte)((b0 + 2 * b1) / 3);
                            break;
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        imageData[offset] = r;
                        imageData[offset + 1] = g;
                        imageData[offset + 2] = b;
                        imageData[offset + 3] = a;
                    }
                }
            }
        }
        #endregion

        #region REGION - DXT5 Encoding
        internal static byte[] DecompressDxt5(byte[] imageData, int width, int height)
        {
            using (MemoryStream imageStream = new MemoryStream(imageData))
                return DecompressDxt5(imageStream, width, height);
        }

        internal static byte[] DecompressDxt5(Stream imageStream, int width, int height)
        {
            byte[] imageData = new byte[width * height * 4];

            using (BinaryReader imageReader = new BinaryReader(imageStream))
            {
                int blockCountX = (width + 3) / 4;
                int blockCountY = (height + 3) / 4;

                for (int y = 0; y < blockCountY; y++)
                {
                    for (int x = 0; x < blockCountX; x++)
                    {
                        DecompressDxt5Block(imageReader, x, y, blockCountX, width, height, imageData);
                    }
                }
            }

            return imageData;
        }

        private static void DecompressDxt5Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, byte[] imageData)
        {
            byte alpha0 = imageReader.ReadByte();
            byte alpha1 = imageReader.ReadByte();

            ulong alphaMask = (ulong)imageReader.ReadByte();
            alphaMask += (ulong)imageReader.ReadByte() << 8;
            alphaMask += (ulong)imageReader.ReadByte() << 16;
            alphaMask += (ulong)imageReader.ReadByte() << 24;
            alphaMask += (ulong)imageReader.ReadByte() << 32;
            alphaMask += (ulong)imageReader.ReadByte() << 40;

            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            byte r0, g0, b0;
            byte r1, g1, b1;
            ConvertRgb565ToRgb888(c0, out r0, out g0, out b0);
            ConvertRgb565ToRgb888(c1, out r1, out g1, out b1);

            uint lookupTable = imageReader.ReadUInt32();

            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 255;
                    uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                    uint alphaIndex = (uint)((alphaMask >> 3 * (4 * blockY + blockX)) & 0x07);
                    if (alphaIndex == 0)
                    {
                        a = alpha0;
                    }
                    else if (alphaIndex == 1)
                    {
                        a = alpha1;
                    }
                    else if (alpha0 > alpha1)
                    {
                        a = (byte)(((8 - alphaIndex) * alpha0 + (alphaIndex - 1) * alpha1) / 7);
                    }
                    else if (alphaIndex == 6)
                    {
                        a = 0;
                    }
                    else if (alphaIndex == 7)
                    {
                        a = 0xff;
                    }
                    else
                    {
                        a = (byte)(((6 - alphaIndex) * alpha0 + (alphaIndex - 1) * alpha1) / 5);
                    }

                    switch (index)
                    {
                        case 0:
                            r = r0;
                            g = g0;
                            b = b0;
                            break;
                        case 1:
                            r = r1;
                            g = g1;
                            b = b1;
                            break;
                        case 2:
                            r = (byte)((2 * r0 + r1) / 3);
                            g = (byte)((2 * g0 + g1) / 3);
                            b = (byte)((2 * b0 + b1) / 3);
                            break;
                        case 3:
                            r = (byte)((r0 + 2 * r1) / 3);
                            g = (byte)((g0 + 2 * g1) / 3);
                            b = (byte)((b0 + 2 * b1) / 3);
                            break;
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        imageData[offset] = r;
                        imageData[offset + 1] = g;
                        imageData[offset + 2] = b;
                        imageData[offset + 3] = a;
                    }
                }
            }
        }
        #endregion

        private static void ConvertRgb565ToRgb888(ushort color, out byte r, out byte g, out byte b)
        {
            int temp;

            temp = (color >> 11) * 255 + 16;
            r = (byte)((temp / 32 + temp) / 32);
            temp = ((color & 0x07E0) >> 5) * 255 + 32;
            g = (byte)((temp / 64 + temp) / 64);
            temp = (color & 0x001F) * 255 + 16;
            b = (byte)((temp / 32 + temp) / 32);
        }

        //custom additions to decode BC5 normal maps
        // can use nvtt?  https://github.com/castano/nvidia-texture-tools/blob/master/project/vc9/Nvidia.TextureTools/TextureTools.cs
        #region REGION - BC5 Dencoding

        /// <summary>
        /// Decompress the input BC5 encoded byte array into standard 4-component RGBA byte array
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        internal static byte[] DecompressBC5(byte[] imageData, int width, int height)
        {
            using (MemoryStream imageStream = new MemoryStream(imageData))
            {
                return DecompressBC5(imageStream, width, height);
            }
        }

        internal static byte[] DecompressBC5(Stream imageStream, int width, int height)
        {
            //only two channels in a BC5 map; X, and Y
            byte[] imageData = new byte[width * height * 2];

            using (BinaryReader imageReader = new BinaryReader(imageStream))
            {
                int blockCountX = (width + 3) / 4;
                int blockCountY = (height + 3) / 4;

                for (int y = 0; y < blockCountY; y++)
                {
                    for (int x = 0; x < blockCountX; x++)
                    {
                        DecompressBC5Block(imageReader, x, y, blockCountX, width, height, imageData);
                    }
                }
            }

            return imageData;
        }

        private static void DecompressBC5Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, byte[] imageData)
        {

            byte alpha10 = imageReader.ReadByte();
            byte alpha11 = imageReader.ReadByte();

            ulong alphaMask1 = (ulong)imageReader.ReadByte();
            alphaMask1 += (ulong)imageReader.ReadByte() << 8;
            alphaMask1 += (ulong)imageReader.ReadByte() << 16;
            alphaMask1 += (ulong)imageReader.ReadByte() << 24;
            alphaMask1 += (ulong)imageReader.ReadByte() << 32;
            alphaMask1 += (ulong)imageReader.ReadByte() << 40;

            byte alpha20 = imageReader.ReadByte();
            byte alpha21 = imageReader.ReadByte();

            ulong alphaMask2 = (ulong)imageReader.ReadByte();
            alphaMask2 += (ulong)imageReader.ReadByte() << 8;
            alphaMask2 += (ulong)imageReader.ReadByte() << 16;
            alphaMask2 += (ulong)imageReader.ReadByte() << 24;
            alphaMask2 += (ulong)imageReader.ReadByte() << 32;
            alphaMask2 += (ulong)imageReader.ReadByte() << 40;

            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte a1, a2;

                    uint alphaIndex1 = (uint)((alphaMask1 >> 3 * (4 * blockY + blockX)) & 0x07);
                    if (alphaIndex1 == 0)
                    {
                        a1 = alpha10;
                    }
                    else if (alphaIndex1 == 1)
                    {
                        a1 = alpha11;
                    }
                    else if (alpha10 > alpha11)
                    {
                        a1 = (byte)(((8 - alphaIndex1) * alpha10 + (alphaIndex1 - 1) * alpha11) / 7);
                    }
                    else if (alphaIndex1 == 6)
                    {
                        a1 = 0;
                    }
                    else if (alphaIndex1 == 7)
                    {
                        a1 = 0xff;
                    }
                    else
                    {
                        a1 = (byte)(((6 - alphaIndex1) * alpha10 + (alphaIndex1 - 1) * alpha11) / 5);
                    }


                    uint alphaIndex2 = (uint)((alphaMask2 >> 3 * (4 * blockY + blockX)) & 0x07);
                    if (alphaIndex2 == 0)
                    {
                        a2 = alpha20;
                    }
                    else if (alphaIndex2 == 1)
                    {
                        a2 = alpha21;
                    }
                    else if (alpha20 > alpha21)
                    {
                        a2 = (byte)(((8 - alphaIndex2) * alpha20 + (alphaIndex2 - 1) * alpha21) / 7);
                    }
                    else if (alphaIndex2 == 6)
                    {
                        a2 = 0;
                    }
                    else if (alphaIndex2 == 7)
                    {
                        a2 = 0xff;
                    }
                    else
                    {
                        a2 = (byte)(((6 - alphaIndex2) * alpha20 + (alphaIndex2 - 1) * alpha21) / 5);
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        imageData[offset] = a1;
                        imageData[offset + 1] = a2;
                    }
                }
            }
        }
        #endregion

        #region BC5 Encoding (based on Nvidia Texture Tools C++ source, ported to C#)

        internal class BC5Block
        {
            public byte alpha0, alpha1;
            public byte[] alpha;
        }

        internal static void CompressBC5Block(BC5Block src, BC5Block dst)
        {

            //byte mina = 255;
            //byte maxa = 0;

            //byte mina_no01 = 255;
            //byte maxa_no01 = 0;

            //// Get min/max alpha.
            //for (uint i = 0; i < 16; i++)
            //{
            //    byte alpha = src.alpha[i];
            //    mina = Math.Min(mina, alpha);
            //    maxa = Math.Max(maxa, alpha);

            //    if (alpha != 0 && alpha != 255)
            //    {
            //        mina_no01 = Math.Min(mina_no01, alpha);
            //        maxa_no01 = Math.Max(maxa_no01, alpha);
            //    }
            //}

            //if (maxa - mina < 8)
            //{
            //    dst.alpha0 = maxa;
            //    dst.alpha1 = mina;

            //    //nvDebugCheck(computeAlphaError(src, dst) == 0);
            //}
            //else if (maxa_no01 - mina_no01 < 6)
            //{
            //    dst.alpha0 = mina_no01;
            //    dst.alpha1 = maxa_no01;

            //    //nvDebugCheck(computeAlphaError(src, dst) == 0);
            //}
            //else
            //{
            //    float besterror = computeAlphaError(src, dst);
            //    int besta0 = maxa;
            //    int besta1 = mina;

            //    // Expand search space a bit.
            //    const byte alphaExpand = 8;
            //    mina = (byte)((mina <= alphaExpand) ? 0 : mina - alphaExpand);
            //    maxa = (byte)((maxa >= 255 - alphaExpand) ? (byte)255 : maxa + alphaExpand);

            //    for (int a0 = mina + 9; a0 < maxa; a0++)
            //    {
            //        for (int a1 = mina; a1 < a0 - 8; a1++)
            //        {
            //            //nvDebugCheck(a0 - a1 > 8);

            //            dst.alpha0 = (byte)a0;
            //            dst.alpha1 = (byte)a1;
            //            float error = computeAlphaError(src, dst, besterror);

            //            if (error < besterror)
            //            {
            //                besterror = error;
            //                besta0 = a0;
            //                besta1 = a1;
            //            }
            //        }
            //    }

            //    // Try using the 6 step encoding.
            //    /*if (mina == 0 || maxa == 255)*/
            //    {

            //        // Expand search space a bit.
            //        const int alphaExpand = 6;
            //        mina_no01 = (mina_no01 <= alphaExpand) ? 0 : mina_no01 - alphaExpand;
            //        maxa_no01 = (maxa_no01 >= 255 - alphaExpand) ? 255 : maxa_no01 + alphaExpand;

            //        for (int a0 = mina_no01 + 9; a0 < maxa_no01; a0++)
            //        {
            //            for (int a1 = mina_no01; a1 < a0 - 8; a1++)
            //            {
            //                nvDebugCheck(a0 - a1 > 8);

            //                dst->alpha0 = a1;
            //                dst->alpha1 = a0;
            //                float error = computeAlphaError(src, dst, besterror);

            //                if (error < besterror)
            //                {
            //                    besterror = error;
            //                    besta0 = a1;
            //                    besta1 = a0;
            //                }
            //            }
            //        }
            //    }

            //    dst->alpha0 = besta0;
            //    dst->alpha1 = besta1;
            //}

            //computeAlphaIndices(src, dst);
        }

        static float computeAlphaError(BC5Block src, BC5Block dst, float bestError = float.MaxValue)
	    {

            //byte[] alphas = new byte[8];
            //dst.evaluatePalette(alphas, false); // @@ Use target decoder.

            float totalError = 0;

            //for (uint i = 0; i< 16; i++)
            //{
            //    byte alpha = src.alpha[i];

            //    int minDist = int.MaxValue;
            //    for (uint p = 0; p< 8; p++)
            //    {
            //        int dist = alphaDistance(alpha, alphas[p]);
            //        minDist = Math.Min(dist, minDist);
            //    }

            //    totalError += minDist* src.weights[i];

            //    if (totalError > bestError)
            //    {
            //        // early out
            //        return totalError;
            //    }
            //}

            return totalError;
	    }

        #endregion
    }
}