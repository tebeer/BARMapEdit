using System;
using UnityEngine;

namespace TGA
{
    public struct TGAHeader
    {
        public const int TgaHeaderSize = 18;
        public int idFieldLength;
        public int colorMapType;
        public int imageType;
        public int colorMapIndex;
        public int colorMapLength;
        public int colorMapDepth;
        public int imageOriginX;
        public int imageOriginY;
        public int imageWidth;
        public int imageHeight;
        public int bitPerPixel;
        public int descriptor;
    }

    public struct TGAData
    {
        public TGAHeader header;
        public Color32[] colors;
    }

    public static class TGALoader
    {
        public static TGAData LoadTGA(byte[] image)
        {
            TGAHeader header;
            header.idFieldLength = image[0];
            header.colorMapType = image[1];
            header.imageType = image[2];
            header.colorMapIndex = image[4] << 8 | image[3];
            header.colorMapLength = image[6] << 8 | image[5];
            header.colorMapDepth = image[7];
            header.imageOriginX = image[9] << 8 | image[8];
            header.imageOriginY = image[11] << 8 | image[10];
            header.imageWidth = image[13] << 8 | image[12];
            header.imageHeight = image[15] << 8 | image[14];
            header.bitPerPixel = image[16];
            header.descriptor = image[17];

            var colorData = new byte[image.Length - TGAHeader.TgaHeaderSize];

            Array.Copy(image, TGAHeader.TgaHeaderSize, colorData, 0, colorData.Length);

            // Index color RLE or Full color RLE or Gray RLE
            Debug.Log(header.imageType);
            if (header.imageType == 9 || header.imageType == 10 || header.imageType == 11)
                colorData = DecodeRLE(header, colorData);

            TGAData tgaData;
            tgaData.header = header;
            tgaData.colors = GetPixels(header, colorData);

            return tgaData;
        }

        private static Color32[] GetPixels(TGAHeader header, byte[] colorData)
        {
            if (header.colorMapType == 0)
            {
                switch (header.imageType)
                {
                    // Index color
                    case 1:
                    case 9:
                        throw new NotImplementedException();

                    // Full color
                    case 2:
                    case 10:

                        var colors = new Color32[header.imageWidth * header.imageHeight];
                        int elementCount = header.bitPerPixel / 8;

                        for (int y = 0; y < header.imageHeight; ++y)
                        {
                            for (int x = 0; x < header.imageWidth; ++x)
                            {
                                int dy = ((header.descriptor & 0x20) == 0 ? y : (header.imageHeight - 1 - y)) * (header.imageWidth * elementCount);
                                int dx = ((header.descriptor & 0x10) == 0 ? x : (header.imageWidth - 1 - x)) * elementCount;
                                int index = dy + dx;

                                byte b = colorData[index + 0];
                                byte g = colorData[index + 1];
                                byte r = colorData[index + 2];

                                if (elementCount == 4) // bitPerPixel == 32
                                {
                                    byte a = colorData[index + 3];
                                    colors[y * header.imageWidth + x] = new Color32(r, g, b, a);//(a << 24) | (r << 16) | (g << 8) | b;
                                }
                                else if (elementCount == 3) // bitPerPixel == 24
                                {
                                    colors[y * header.imageWidth + x] = new Color32(r, g, b, 255);// (r << 16) | (g << 8) | b;
                                }
                            }
                        }

                        return colors;

                    // Gray
                    case 3:
                    case 11:
                        throw new NotImplementedException();

                    default:
                        throw new Exception("Unknown imageType " + header.imageType);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static byte[] DecodeRLE(TGAHeader header, byte[] colorData)
        {
            int elementCount = header.bitPerPixel / 8;
            byte[] elements = new byte[elementCount];
            int decodeBufferLength = elementCount * header.imageWidth * header.imageHeight;
            byte[] decodeBuffer = new byte[decodeBufferLength];
            int decoded = 0;
            int offset = 0;
            while (decoded < decodeBufferLength)
            {
                int packet = colorData[offset++] & 0xFF;
                if ((packet & 0x80) != 0)
                {
                    for (int i = 0; i < elementCount; i++)
                    {
                        elements[i] = colorData[offset++];
                    }
                    int count = (packet & 0x7F) + 1;
                    for (int i = 0; i < count; i++)
                    {
                        for (int j = 0; j < elementCount; j++)
                        {
                            decodeBuffer[decoded++] = elements[j];
                        }
                    }
                }
                else
                {
                    int count = (packet + 1) * elementCount;
                    for (int i = 0; i < count; i++)
                    {
                        decodeBuffer[decoded++] = colorData[offset++];
                    }
                }
            }

            return decodeBuffer;
        }
    }
}