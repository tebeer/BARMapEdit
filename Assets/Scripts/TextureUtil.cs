using UnityEngine;
using System.IO;

public static class TextureUtil
{
    public static Texture2D LoadSupportedTexture(byte[] bytes)
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

    public static Texture2D LoadDDSTexture(byte[] bytes, string name)
    {
        using (BinaryReader r = new BinaryReader(new MemoryStream(bytes)))
        {
            string code = r.ReadASCIIString(4);

            if (code != "DDS ")
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

    public static Texture2D LoadTGATexture(byte[] bytes)
    {
        var tga = TGA.TGALoader.LoadTGA(bytes);

        Texture2D tex;
        if (tga.header.bitPerPixel / 8 == 4)
            tex = new Texture2D(tga.header.imageWidth, tga.header.imageHeight, TextureFormat.RGBA32, true);
        else if (tga.header.bitPerPixel / 8 == 4)
            tex = new Texture2D(tga.header.imageWidth, tga.header.imageHeight, TextureFormat.RGB24, true);
        else
            throw new System.Exception("Unknown format " + tga.header.bitPerPixel);

        tex.SetPixels32(tga.colors);
        tex.Apply();

        return tex;
    }

    public static Texture2D LoadBMPTexture(byte[] bytes)
    {
        var bmpLoader = new B83.Image.BMP.BMPLoader();

        var bmpImg = bmpLoader.LoadBMP(bytes);

        return bmpImg.ToTexture2D();
    }
}
