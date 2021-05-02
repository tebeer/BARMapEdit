using System.IO;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SMFHeader
{
	public const string Magic = "spring map file";

	public fixed byte magic[16];///< "spring map file\0"
	public int version;         ///< Must be 1 for now
	public int mapid;           ///< Sort of a GUID of the file, just set to a random value when writing a map
	public int mapx;            ///< Must be divisible by 128
	public int mapy;            ///< Must be divisible by 128
	public int squareSize;      ///< Distance between vertices. Must be 8
	public int texelPerSquare;  ///< Number of texels per square, must be 8 for now
	public int tilesize;        ///< Number of texels in a tile, must be 32 for now
	public float minHeight;     ///< Height value that 0 in the heightmap corresponds to
	public float maxHeight;     ///< Height value that 0xffff in the heightmap corresponds to
	public int heightmapPtr;    ///< File offset to elevation data (short int[(mapy+1)*(mapx+1)])
	public int typeMapPtr;      ///< File offset to typedata (unsigned char[mapy/2 * mapx/2])
	public int tilesPtr;        ///< File offset to tile data (see MapTileHeader)
	public int minimapPtr;      ///< File offset to minimap (always 1024*1024 dxt1 compresed data plus 8 mipmap sublevels)
	public int metalmapPtr;     ///< File offset to metalmap (unsigned char[mapx/2 * mapy/2])
	public int featurePtr;      ///< File offset to feature data (see MapFeatureHeader)
	public int numExtraHeaders; ///< Numbers of extra headers following main header
}

[StructLayout(LayoutKind.Sequential)]
public struct SMFExtraHeader
{
	public int size; ///< Size of extra header
	public int type; ///< Type of extra header
	public int extraoffset; //MISSING FROM DOCS, only exists if type=1 (vegmap)'''
}

[StructLayout(LayoutKind.Sequential)]
public struct MapTileHeader
{
	public int numTileFiles; ///< Number of tile files to read in (usually 1)
	public int numTiles;     ///< Total number of tiles'''
}

[StructLayout(LayoutKind.Sequential)]
public struct MapFeatureHeader
{
	public int numFeatureType;
	public int numFeatures;
}

[StructLayout(LayoutKind.Sequential)]
public struct MapFeature
{
	public int featureType;    ///< Index to one of the strings above
	public float xpos;         ///< X coordinate of the feature
	public float ypos;         ///< Y coordinate of the feature (height)
	public float zpos;         ///< Z coordinate of the feature
	public float rotation;     ///< Orientation of this feature (-32768..32767 for full circle)
	public float relativeSize; ///< Not used at the moment keep 1'''
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct TileFileHeader
{
	public const string Magic = "spring tilefile";
	public fixed byte magic[16];      ///< "spring tilefile\0"
	public int version;         ///< Must be 1 for now
	public int numTiles;        ///< Total number of tiles in this file
	public int tileSize;        ///< Must be 32 for now
	public int compressionType; ///< Must be 1 (= dxt1) for now'''
}

[StructLayout(LayoutKind.Sequential)]
public struct SMFData
{
	public float[] heightMap;
	public int resX;
	public int resY;
	public float scale;
}