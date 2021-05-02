using System.IO;
using UnityEngine;

public static unsafe class S3OUnity
{
    public static S3OPiece Load(string path)
    {
        S3OPiece rootPiece = null;

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            var header = LoadHeader(reader);
            rootPiece = LoadPiece(reader, header.rootPieceOffset, null);
        }

        return rootPiece;
    }

    public static S3OHeader LoadHeader(BinaryReader reader)
    {
        if (reader == null || reader.BaseStream.Length == 0)
            throw new System.Exception("No data");

        var header = reader.ReadStruct<S3OHeader>();
        var magic = new string(header.magic);

        if (magic != S3OHeader.Magic)
            throw new System.Exception("Not a Spring unit file: " + magic);

        if (header.version != 0)
            throw new System.Exception("Wrong file version: " + header.version);

        return header;
    }

    public static S3OPiece LoadPiece(BinaryReader reader, long offset, S3OPiece parent)
    {
        reader.BaseStream.Position = offset;

        var piece = new S3OPiece();

        piece.parent = parent;

        var header = reader.ReadStruct<S3OPieceHeader>();

        piece.xoffset = header.xoffset;
        piece.yoffset = header.yoffset;
        piece.zoffset = header.zoffset;

        piece.name = reader.ReadASCIIStringAtPos(header.nameOffset);

        piece.verts = new S3OVertex[header.numVerts];
        reader.BaseStream.Position = header.vertsOffset;
        for (int i = 0; i < header.numVerts; ++i)
        {
            piece.verts[i] = S3OVertex.Load(reader, reader.BaseStream.Position);
        }
        // self.unique_verts, self.vertids = remove_doubles(self.verts)

        //#load primitives
        reader.BaseStream.Position = header.vertTableOffset;
        piece.faces = new int[header.vertTableSize];

        for (int i = 0; i < header.vertTableSize; ++i)
            piece.faces[i] = (int)reader.ReadUInt32();

        piece.children = new S3OPiece[header.numChildren];

        if (header.numChildren > 0)
        {
            reader.BaseStream.Position = header.childrenOffset;

            for (int i = 0; i < header.numChildren; ++i)
            {
                uint childOffset = reader.ReadUInt32();
                var pos = reader.BaseStream.Position;

                piece.children[i] = LoadPiece(reader, childOffset, piece);

                reader.BaseStream.Position = pos;
            }
        }

        return piece;
    }

    public static Mesh CreateMesh(S3OPiece piece)
    {
        var mesh = new Mesh();
        mesh.name = piece.name + "_mesh";

        int vertexCount = piece.verts.Length;

        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var uv = new Vector2[vertexCount];

        for (int i = 0; i < vertexCount; ++i)
        {
            ref var vert = ref piece.verts[i];
            vertices[i] = new Vector3(vert.xpos, vert.ypos, vert.zpos);
            normals[i] = new Vector3(vert.xnormal, vert.ynormal, vert.znormal);
            uv[i] = new Vector2(vert.texu, vert.texv);
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = piece.faces;

        return mesh;
    }

    public static void CreateGameObject(string path)
    {
        CreateGameObject(Load(path), null);
    }

    public static void CreateGameObject(S3OPiece piece, GameObject parent)
    {
        var gameObject = new GameObject(piece.name);
        if (parent != null)
            gameObject.transform.parent = parent.transform;

        var mr = gameObject.AddComponent<MeshRenderer>();
        var mf = gameObject.AddComponent<MeshFilter>();

        gameObject.transform.localPosition = new Vector3(piece.xoffset, piece.yoffset, piece.zoffset);

        mf.sharedMesh = S3OUnity.CreateMesh(piece);

        for (int i = 0; i < piece.children.Length; ++i)
        {
            CreateGameObject(piece.children[i], gameObject);
        }
    }
}
