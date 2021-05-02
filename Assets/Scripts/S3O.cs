using System.IO;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct S3OHeader
{
    public const string Magic = "Spring unit";

    public fixed char magic[12]; // "Spring unit\0"
    public uint version;// = 0    # uint = 0
    public float radius;// = 0.0 # float: radius of collision sphere
    public float height;// = 0.0 # float: height of whole object
    public float midx;// = 0.0 # float offset from origin
    public float midy;// = 0.0 #
    public float midz;// = 0.0 #
    public uint rootPieceOffset;// = 0 # offset of root piece
    public uint collisionDataOffset;// = 0 # offset of collision data, 0 = no data
    public uint texture1Offset;// = 0 # offset to filename of 1st texture
    public uint texture2Offset;// = 0 # offset to filename of 2nd texture}
}

public enum S3OPrimitiveType : uint
{
    Triangles = 0,
    TriangleStrips = 1,
    Quads = 2,
}

public struct S3OPieceHeader
{
    public uint nameOffset;
    public uint numChildren;
    public uint childrenOffset;
    public uint numVerts;
    public uint vertsOffset;
    public uint vertType;
    public S3OPrimitiveType primitiveType;
    public uint vertTableSize;
    public uint vertTableOffset;
    public uint collisionDataOffset;
    public float xoffset;
    public float yoffset;
    public float zoffset;
}

public class S3OPiece
{
    public string name;
    public S3OVertex[] verts;
    public int[] faces;
    public S3OPiece parent;
    public S3OPiece[] children;
    public float xoffset;
    public float yoffset;
    public float zoffset;
}

public struct S3OVertex
{
    //binary_format = "<8f"
    public float xpos;
    public float ypos;
    public float zpos;
    public float xnormal;
    public float ynormal;
    public float znormal;
    public float texu;
    public float texv;

    public static S3OVertex Load(BinaryReader reader, long offset)
    {
        reader.BaseStream.Position = offset;

        S3OVertex vert;

        vert.xpos = reader.ReadSingle();
        vert.ypos = reader.ReadSingle();
        vert.zpos = reader.ReadSingle();
        vert.xnormal = reader.ReadSingle();
        vert.ynormal = reader.ReadSingle();
        vert.znormal = reader.ReadSingle();
        vert.texu = reader.ReadSingle();
        vert.texv = reader.ReadSingle();

        return vert;
    }
}