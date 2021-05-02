using UnityEngine;

namespace CFRenderLib.Unity
{
    public enum AtlasClearColor
    {
        Black,
        Grey,
    }

    public class TextureCompressionInfo
    {
        public int bitsPerPixel;
        public int blockWidth;
        public int bytesPerBlock;
        public int minWidth;
        public bool blocksInMortonOrder;
        public byte[] Black;
        public byte[] Grey;

        public byte[] GetColor(AtlasClearColor color)
        {
            switch(color)
            {
                case AtlasClearColor.Black:
                    return Black;
                case AtlasClearColor.Grey:
                    return Grey;
                default:
                    throw new System.Exception("Unknown atlas clear color: " + color);
            }
        }

        public static TextureCompressionInfo Get(Texture2D tex)
        {
            var format = tex.format;
            TextureCompressionInfo tc;
            switch(format)
            {
                case TextureFormat.ETC_RGB4:
                    tc = new TextureCompressionInfo()
                    {
                        bitsPerPixel = 4,
                        blockWidth = 4,
                        minWidth = 4,
                        Black = new byte[] { 0, 0, 0, 2, 255, 255, 255, 255, 0, 0, 0, 2, 255, 255, 255, 255, 0, 0, 0, 2, 255, 255, 255, 255 },
                        Grey = new byte[] { 120, 120, 120, 38, 0, 0, 0, 0, 120, 120, 120, 38, 0, 0, 0, 0, 120, 120, 120, 38, 0, 0, 0, 0 },
                    };
                    break;
                case TextureFormat.DXT1:
                    tc = new TextureCompressionInfo()
                    {
                        bitsPerPixel = 4,
                        blockWidth = 4,
                        minWidth = 4,
                        Black = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                        Grey = new byte[] { 239, 123, 16, 132, 170, 170, 170, 170, 239, 123, 16, 132, 170, 170, 170, 170, 239, 123, 16, 132, 170, 170, 170, 170 },
                    };
                    break;
                case TextureFormat.PVRTC_RGB4:
                    tc = new TextureCompressionInfo()
                    {
                        bitsPerPixel = 4,
                        blockWidth = 4,
                        minWidth = 8,
                        blocksInMortonOrder = true,
                        Black = new byte[] { 170, 170, 170, 170, 0, 128, 0, 128, 170, 170, 170, 170, 0, 128, 0, 128, 170, 170, 170, 170, 0, 128, 0, 128 },
                        Grey = new byte[] { 170, 170, 170, 170, 206, 185, 49, 198, 170, 255, 255, 255, 238, 189, 239, 189, 254, 254, 254, 254, 238, 189, 239, 189 },
                    };
                    break;
                //case TextureFormat.ASTC_RGB_4x4:
                //    tc = new TextureCompressionInfo()
                //    {
                //        bitsPerPixel = 8,
                //        blockWidth = 4,
                //        minWidth = 4,
                //        blocksInMortonOrder = false,
                //        Black = new byte[] { 252, 253, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 255, 255 },
                //        Grey = new byte[] { 252, 253, 255, 255, 255, 255, 255, 255, 127, 127, 127, 127, 127, 127, 255, 255 },
                //    };
                //    break;
                //case TextureFormat.ASTC_RGB_8x8:
                //    tc = new TextureCompressionInfo()
                //    {
                //        bitsPerPixel = 2,
                //        blockWidth = 8,
                //        minWidth = 8,
                //        blocksInMortonOrder = false,
                //        Black = new byte[] { 252, 253, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 255, 255 },
                //        Grey = new byte[] { 252, 253, 255, 255, 255, 255, 255, 255, 127, 127, 127, 127, 127, 127, 255, 255 },
                //    };
                //break;
                default:
                    throw new System.Exception("TextureFormat not supported: " + format + " " + tex);
            }

            tc.bytesPerBlock = tc.blockWidth * tc.blockWidth * tc.bitsPerPixel / 8;

            return tc;
        }
    }
}
