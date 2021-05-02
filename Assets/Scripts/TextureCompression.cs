using UnityEngine;
using Unity.Collections;

namespace CFRenderLib.Unity
{
    public static class TextureCompression
    {
        public static void Clear(TextureCompressionInfo tc, RawTextureData data, AtlasClearColor clearColor)
        {
            var clearBlock = tc.GetColor(clearColor);
            for (int i = 0; i < data.mipmapCount; ++i)
            {
                ClearMipmap(tc, data, i, clearBlock);
            }
        }

        private static void ClearMipmap(TextureCompressionInfo tc, RawTextureData data, int mipmap, byte[] clearBlock)
        {
            int mipDivider = 1 << mipmap;

            var width = data.width / mipDivider;
            var height = data.height / mipDivider;

            var blocksX = Mathf.Max(1, width / tc.blockWidth);
            var blocksY = Mathf.Max(1, height / tc.blockWidth);

            for (int y = 0; y < blocksY; ++y)
                for (int x = 0; x < blocksX; ++x)
                    ClearBlock(tc, data, x, y, mipmap, clearBlock);
        }

        private static void ClearBlock(TextureCompressionInfo tc, RawTextureData data, int x, int y, int mipmap, byte[] clearBlock)
        {
            int blockIndex = GetBlockStartIndex(tc, data, mipmap, x, y);

            NativeArray<byte>.Copy(clearBlock, 0, data.rawDataNative, blockIndex, tc.bytesPerBlock);
            //System.Array.Copy(source.rawData, sourceIndex, target.rawData, targetIndex, tc.bytesPerBlock);
        }

        public static void CopyTexture(TextureCompressionInfo tc, RawTextureData source, RawTextureData target, int targetX, int targetY, int targetWidth, int targetHeight)
        {
            // Source texture has less mipmaps than atlas texture.
            // Mipmaps where pixel size is smaller than source texture size
            // would have blending of colors between different source textures.
            // We don't want that, so the max mipmap level should be limited.
            // Unity doesn't support this yet.
            // https://forum.unity.com/threads/limiting-the-amount-of-mipmap-levels.650011/

            try
            {
                // Sample from higher source mipmap to make the texture fit in the target size
                int width = source.width;
                int height = source.height;
                int sourceMip = 0;
                while (targetWidth < width || targetHeight < height)
                {
                    sourceMip++;
                    width /= 2;
                    height /= 2;
                }

                var mipmapCount = Mathf.Min(source.mipmapCount - sourceMip, target.mipmapCount);

                for (int i = 0; i < mipmapCount; ++i)
                {
                    CopyMipmap(tc, source, target, targetX, targetY, i + sourceMip, i);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to copy texture into atlas" + ex.ToString());
            }
        }

        private static void CopyMipmap(TextureCompressionInfo tc, RawTextureData source, RawTextureData target, int targetX, int targetY, int sourceMipmap, int targetMipmap)
        {
            int sourceMipDivider = 1 << sourceMipmap;
            var width = source.width / sourceMipDivider;
            var height = source.height / sourceMipDivider;

            // Don't copy mips smaller than block size
            if (width < tc.blockWidth
            || height < tc.blockWidth)
                return;

            int targetMipDivider = 1 << targetMipmap;

            // Don't copy if target position is misaligned with block size
            if (targetX % (tc.blockWidth * targetMipDivider) != 0 ||
               targetY % (tc.blockWidth * targetMipDivider) != 0)
                return;

            var targetBlockX = targetX / tc.blockWidth / targetMipDivider;
            var targetBlockY = targetY / tc.blockWidth / targetMipDivider;

            var blocksX = width / tc.blockWidth;
            var blocksY = height / tc.blockWidth;

            for (int y = 0; y < blocksY; ++y)
            {
                for (int x = 0; x < blocksX; ++x)
                {
                    CopyBlock(tc, source, x, y, target, targetBlockX + x, targetBlockY + y, sourceMipmap, targetMipmap);
                    //CopyBlock(source, x, y, target, targetBlockX + y, targetBlockY + blocksX - x - 1, mipmap);
                }
            }
        }

        private static void CopyBlock(TextureCompressionInfo tc, RawTextureData source, int sourceX, int sourceY, RawTextureData target, int targetX, int targetY, int sourceMipmap, int targetMipmap)
        {
            int sourceIndex = GetBlockStartIndex(tc, source, sourceMipmap, sourceX, sourceY);
            int targetIndex = GetBlockStartIndex(tc, target, targetMipmap, targetX, targetY);

            if(source.rawDataNative.IsCreated && target.rawDataNative.IsCreated)
                NativeArray<byte>.Copy(source.rawDataNative, sourceIndex, target.rawDataNative, targetIndex, tc.bytesPerBlock);
            else if(source.rawDataNative.IsCreated && !target.rawDataNative.IsCreated)
                NativeArray<byte>.Copy(source.rawDataNative, sourceIndex, target.rawData, targetIndex, tc.bytesPerBlock);
            else if (!source.rawDataNative.IsCreated && target.rawDataNative.IsCreated)
                NativeArray<byte>.Copy(source.rawData, sourceIndex, target.rawDataNative, targetIndex, tc.bytesPerBlock);
            else
                System.Array.Copy(source.rawData, sourceIndex, target.rawData, targetIndex, tc.bytesPerBlock);
        }

        public static int GetBlockStartIndex(TextureCompressionInfo tc, RawTextureData tex, int mipmap, int blockX, int blockY)
        {
            if (tc.blocksInMortonOrder)
                return tex.mipmaps[mipmap].startIndex + MortonEncode(blockX, blockY) * tc.bytesPerBlock;
            else
                return tex.mipmaps[mipmap].startIndex + (blockX + blockY * tex.mipmaps[mipmap].blocksX) * tc.bytesPerBlock;
        }

        private static void Assert(bool condition)
        {
            if (!condition)
                throw new System.Exception();
        }

        private static int MortonEncode(int x, int y)
        {
            x = (x | (x << 16)) & 0x0000FFFF;
            x = (x | (x << 8)) & 0x00FF00FF;
            x = (x | (x << 4)) & 0x0F0F0F0F;
            x = (x | (x << 2)) & 0x33333333;
            x = (x | (x << 1)) & 0x55555555;

            y = (y | (y << 16)) & 0x0000FFFF;
            y = (y | (y << 8)) & 0x00FF00FF;
            y = (y | (y << 4)) & 0x0F0F0F0F;
            y = (y | (y << 2)) & 0x33333333;
            y = (y | (y << 1)) & 0x55555555;

            return y | (x << 1);
        }
    }
}