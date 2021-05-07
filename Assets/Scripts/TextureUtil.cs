using UnityEngine;
using System.IO;

public static class TextureUtil
{
    public static Texture2D LoadSupportedTexture(byte[] bytes, bool alpha)
    {
        Texture2D tex = new Texture2D(2, 2);// alpha ? TextureFormat.DXT5: TextureFormat.DXT1, true);
        if (!tex.LoadImage(bytes))
            return null;
        return tex;
    }

    struct DDSPixelFormat
    {
        public int dwSize;
        public int dwFlags;
        public int dwFourCC;
        public int dwRGBBitCount;
        public int dwRBitMask;
        public int dwGBitMask;
        public int dwBBitMask;
        public int dwABitMask;
    }

    struct DDSHeader
    {
        public int dwSize;
        public int dwFlags;
        public int dwHeight;
        public int dwWidth;
        public int dwPitchOrLinearSize;
        public int dwDepth;
        public int dwMipMapCount;
        public int dwReserved1_1;
        public int dwReserved1_2;
        public int dwReserved1_3;
        public int dwReserved1_4;
        public int dwReserved1_5;
        public int dwReserved1_6;
        public int dwReserved1_7;
        public int dwReserved1_8;
        public int dwReserved1_9;
        public int dwReserved1_10;
        public int dwReserved1_11;
        public DDSPixelFormat ddspf;
        public int dwCaps;
        public int dwCaps2;
        public int dwCaps3;
        public int dwCaps4;
        public int dwReserved2;
    }

    private const int FOURCC_DXT1 = 0x31545844;
    private const int FOURCC_DXT3 = 0x33545844;
    private const int FOURCC_DXT5 = 0x35545844;

    public static Texture2D LoadDDSTexture(byte[] bytes, bool alpha, string name)
    {
        using (BinaryReader r = new BinaryReader(new MemoryStream(bytes)))
        {
            string code = r.ReadASCIIString(4);

            if(code != "DDS ")
                throw new System.Exception("Not a DDS file");

            var header = r.ReadStruct<DDSHeader>();

            if (header.dwSize != 124)
                throw new System.Exception("Invalid DDS DXTn texture. header.dwSize != 124");

            byte[] dxtBytes = r.ReadBytes((int)(r.BaseStream.Length - r.BaseStream.Position));
            
            TextureFormat format = TextureFormat.DXT1;

            switch (header.ddspf.dwFourCC)
            {
                case FOURCC_DXT1:
                    format = TextureFormat.DXT1;
                    break;
                case FOURCC_DXT3:
                    throw new System.Exception("DXT3 Unsupported");
                case FOURCC_DXT5:
                    format = TextureFormat.DXT5;
                    break;
            }

            //Debug.Log(name + " " + format + " " + header.dwMipMapCount);

            int mipmapCount = 1;
            if (header.dwMipMapCount > 0)
                mipmapCount = header.dwMipMapCount;

            Texture2D texture = new Texture2D(header.dwWidth, header.dwHeight, format, mipmapCount, false);
            texture.LoadRawTextureData(dxtBytes);
            texture.Apply();

            return texture;
        }
    }

    public static Texture2D LoadTGATexture(byte[] bytes, bool usealpha)
    {
        using (BinaryReader r = new BinaryReader(new MemoryStream(bytes)))
        {
            // Skip some header info we don't care about.
            // Even if we did care, we have to move the stream seek point to the beginning,
            // as the previous method in the workflow left it at the end.
            r.BaseStream.Seek(12, SeekOrigin.Begin);

            short width = r.ReadInt16();
            short height = r.ReadInt16();
            int bitDepth = r.ReadByte();

            // Skip a byte of header information we don't care about.
            r.BaseStream.Seek(1, SeekOrigin.Current);

            Texture2D tex = new Texture2D(width, height);
            Color32[] pulledColors = new Color32[width * height];

            if (bitDepth == 32)
            {
                for (int i = 0; i < width * height; i++)
                {
                    byte red = r.ReadByte();
                    byte green = r.ReadByte();
                    byte blue = r.ReadByte();
                    byte alpha = r.ReadByte();

                    pulledColors[i] = new Color32(blue, green, red, alpha);
                }
            }
            else if (bitDepth == 24)
            {
                for (int i = 0; i < width * height; i++)
                {
                    byte red = r.ReadByte();
                    byte green = r.ReadByte();
                    byte blue = r.ReadByte();

                    pulledColors[i] = new Color32(blue, green, red, 1);
                }
            }
            else
            {
                throw new System.Exception("TGA texture had non 32/24 bit depth.");
            }

            tex.SetPixels32(pulledColors);
            tex.Apply();
            return tex;
        }
    }
}
