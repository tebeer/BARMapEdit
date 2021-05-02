using UnityEngine;
using Unity.Collections;

namespace CFRenderLib.Unity
{
    public class RawTextureData
    {
        public NativeArray<byte> rawDataNative;
        public byte[] rawData;
        public MipmapData[] mipmaps;

        public int width;
        public int height;
        public int mipmapCount;

        public struct MipmapData
        {
            public int startIndex;
            public int blocksX;
            public int blocksY;
        }

        public RawTextureData(Texture2D texture, TextureCompressionInfo tc)
        {
            rawDataNative = texture.GetRawTextureData<byte>();
            width = texture.width;
            height = texture.height;
            mipmapCount = texture.mipmapCount;
            FindMipmaps(tc);
        }

        public RawTextureData(byte[] rawData, int width, int height, int mipmapCount, TextureCompressionInfo tc)
        {
            this.rawData = rawData;
            this.width = width;
            this.height = height;
            this.mipmapCount = mipmapCount;
            FindMipmaps(tc);
        }

        private void FindMipmaps(TextureCompressionInfo tc)
        {
            mipmaps = new MipmapData[mipmapCount];

            int totalSizeBytes = 0;
            
            var w = width;
            var h = height;

            for(int i = 0; i < mipmapCount; ++i)
            {
                int blocksX = w / tc.blockWidth;
                int blocksY = h / tc.blockWidth;

                mipmaps[i].blocksX = blocksX;
                mipmaps[i].blocksY = blocksY;
                mipmaps[i].startIndex = totalSizeBytes;

                int blocks = blocksX * blocksY;
                int mipSizeBytes = blocks * tc.bytesPerBlock;

                totalSizeBytes += mipSizeBytes;

                if(w > tc.minWidth)
                    w /= 2;
                if(h > tc.minWidth)
                    h /= 2;
            }
            if (rawDataNative.IsCreated)
            {
                if (rawDataNative.Length != totalSizeBytes)
                    throw new System.Exception($"FindMipmaps failed\nrawDataNative.Length: {rawDataNative.Length}\ntotalSizeBytes: {totalSizeBytes}\nwidth: {width}\nheight: {height}");
            }
            else
            {
                if (rawData.Length != totalSizeBytes)
                    throw new System.Exception($"FindMipmaps failed\nrawData.Length: {rawData.Length}\ntotalSizeBytes: {totalSizeBytes}\nwidth: {width}\nheight: {height}");
            }
        }
    }
}