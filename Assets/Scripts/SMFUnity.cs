using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static unsafe class SMFUnity
{
    public static SMFData Load(string path)
    {
        SMFData data;
        SMFHeader header;

        var dir = Path.GetDirectoryName(path);

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            header = LoadHeader(reader);
			data = LoadData(reader, header);
            Debug.Log(header.mapx + " " + header.mapy);

            reader.BaseStream.Position = header.tilesPtr;
            var mapTileHeader = reader.ReadStruct<MapTileHeader>();

            for (int i = 0; i < mapTileHeader.numTileFiles; ++i)
            {
                int tiles = reader.ReadInt32();
                var tileFileName = reader.ReadNullTerminatedString();

                var tileFilePath = Path.Combine(dir, tileFileName);
                Debug.Log(tiles+ " " + tileFilePath);

                LoadTileFile(tileFilePath);
            }
        }



        //var smtFilePath = Path.ChangeExtension(path, "smt");

        //Debug.Log(smtFilePath);
        //


        return data;

    }

    public static void LoadTileFile(string path)
    {
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            var header = LoadTileFileHeader(reader);
            Debug.Log(header.tileSize);
            Debug.Log(header.numTiles);
        }
    }

    public static SMFHeader LoadHeader(BinaryReader reader)
	{
		var header = reader.ReadStruct<SMFHeader>();

		var magic = new string(header.magic);

		if (magic != SMFHeader.Magic)
			throw new System.Exception("Not a Spring unit file: " + magic);

		if (header.version != 1)
			throw new System.Exception("Invalid version: " + header.version);

		return header;
	}

	public static SMFData LoadData(BinaryReader reader, SMFHeader header)
	{
		SMFData data;
        data.resX = header.mapx + 1;
        data.resY = header.mapy + 1;
        data.scale = header.squareSize;
		data.heightMap = LoadHeightMap(reader, header);
		return data;
	}

	public static float[] LoadHeightMap(BinaryReader reader, SMFHeader header)
	{
		reader.BaseStream.Position = header.heightmapPtr;

		int size = (header.mapx + 1) * (header.mapy + 1);
		var data = reader.ReadBytes(size * sizeof(ushort));
		var hmap = new ushort[size];

		System.Buffer.BlockCopy(data, 0, hmap, 0, data.Length);

		var height = new float[size];
		for (int i = 0; i < size; ++i)
		{
			height[i] = header.minHeight + ((float)hmap[i] / ushort.MaxValue) * (header.maxHeight - header.minHeight);
		}

		return height;
	}

    public static TileFileHeader LoadTileFileHeader(BinaryReader reader)
    {
        var header = reader.ReadStruct<TileFileHeader>();
        var magic = new string(header.magic);
        if(magic != TileFileHeader.Magic)
            throw new System.Exception("Invalid TileFileHeader magic: " + magic);

        return header;
    }

    const int chunkSize = 32;

    public static GameObject CreateMapObject(SMFData data)
	{
        GameObject rootGO = new GameObject("Map");

        int divisionsX = data.resX / chunkSize;
        int divisionsY = data.resY / chunkSize;

        m_tempVerts = new List<Vector3>(chunkSize * chunkSize);
        m_tempUV = new List<Vector2>(chunkSize * chunkSize);
        m_tempNormals = new List<Vector3>(chunkSize * chunkSize);
        m_tempTriangles = new List<int>(chunkSize * chunkSize * 6);

        //m_splatMap = new Texture2D(m_resX, m_resY);
        //m_splatMap.SetPixels(colors);
        //m_splatMap.Apply();
        //
        //m_material = Object.Instantiate(material);
        //SetMaterialProps();

        var material = new Material(Shader.Find("Standard"));

        for (int x = 0; x < divisionsX; ++x)
        {
            for (int y = 0; y < divisionsY; ++y)
            {
                GenerateChunk(data, rootGO, material, x * chunkSize, y * chunkSize, chunkSize, chunkSize);
            }
        }

        return rootGO;
    }

    private static void GenerateChunk(SMFData data, GameObject parent, Material mat, int ox, int oy, int resX, int resY)
    {
        var chunkGO = new GameObject($"Chunk_{ox}_{oy}");
        chunkGO.transform.parent = parent.transform;
        chunkGO.transform.localPosition = new Vector3(ox * data.scale, 0, -oy * data.scale);
        chunkGO.transform.localScale = new Vector3(data.scale, 1, -data.scale);
        chunkGO.isStatic = true;

        var lodGroup = chunkGO.AddComponent<LODGroup>();
        var lods = new LOD[4];

        int stride = 1;
        for (int i = 0; i < lods.Length; ++i)
        {
            var lod = GenerateMesh(data, mat, ox, oy, resX, resY, stride);
            lod.name = $"{lod.name}_{ox}_{oy}";
            stride *= 2;
            lod.transform.parent = chunkGO.transform;
            lod.transform.localPosition = Vector3.zero;
            lod.transform.localScale = Vector3.one;
            lods[i].renderers = new Renderer[] { lod.GetComponent<Renderer>() };
            lods[i].screenRelativeTransitionHeight = 1.0f / (stride);
        }

        lodGroup.SetLODs(lods);
    }

    public static GameObject GenerateMesh(SMFData data, Material mat, int ox, int oy, int resX, int resY, int stride)
    {
        var mesh = new Mesh();
        mesh.name = $"{ox}_{oy}_{stride}";

        int vertsX = resX / stride;
        int vertsY = resY / stride;
        if (ox + resX < data.resX)
            vertsX += 1;
        if (oy + resY < data.resY)
            vertsY += 1;

        m_tempVerts.Clear();
        m_tempUV.Clear();
        m_tempNormals.Clear();

        for (int y = 0; y < vertsY; ++y)
        {
            for (int x = 0; x < vertsX; ++x)
            {
                int i = x + y * vertsX;
                int xx = x * stride;
                int yy = y * stride;
                var uv = new Vector2((float)(ox + xx) / data.resX, (float)(oy + yy) / data.resY);

                m_tempVerts.Add(new Vector3(xx, GetHeight(data, ox + xx, oy + yy), yy));

                var l = GetHeight(data, ox + xx - stride, oy + yy);
                var r = GetHeight(data, ox + xx + stride, oy + yy);
                var u = GetHeight(data, ox + xx, oy + yy + stride);
                var d = GetHeight(data, ox + xx, oy + yy - stride);
                m_tempNormals.Add(new Vector3(l - r, 2 * stride, d - u).normalized);

                m_tempUV.Add(uv);
            }
        }

        m_tempTriangles.Clear();
        for (int y = 0; y < vertsY - 1; ++y)
        {
            for (int x = 0; x < vertsX - 1; ++x)
            {
                int t0 = (x + 0) + (y + 0) * vertsX;
                int t1 = (x + 1) + (y + 0) * vertsX;
                int t2 = (x + 1) + (y + 1) * vertsX;
                int t3 = (x + 0) + (y + 1) * vertsX;


                //if (x % 2 == 0)
                //{
                //    m_tempTriangles.Add(t3);
                //    m_tempTriangles.Add(t2);
                //    m_tempTriangles.Add(t1);
                //    m_tempTriangles.Add(t1);
                //    m_tempTriangles.Add(t0);
                //    m_tempTriangles.Add(t3);
                //}
                //else
                //{
                //    m_tempTriangles.Add(t0);
                //    m_tempTriangles.Add(t3);
                //    m_tempTriangles.Add(t2);
                //    m_tempTriangles.Add(t2);
                //    m_tempTriangles.Add(t1);
                //    m_tempTriangles.Add(t0);
                //}

                var v0 = m_tempVerts[t0];
                var v1 = m_tempVerts[t1];
                var v2 = m_tempVerts[t2];
                var v3 = m_tempVerts[t3];

                if (Mathf.Abs(v2.y - v0.y) > Mathf.Abs(v3.y - v1.y))
                {
                    m_tempTriangles.Add(t3);
                    m_tempTriangles.Add(t2);
                    m_tempTriangles.Add(t1);
                    m_tempTriangles.Add(t1);
                    m_tempTriangles.Add(t0);
                    m_tempTriangles.Add(t3);
                }
                else
                {
                    m_tempTriangles.Add(t0);
                    m_tempTriangles.Add(t3);
                    m_tempTriangles.Add(t2);
                    m_tempTriangles.Add(t2);
                    m_tempTriangles.Add(t1);
                    m_tempTriangles.Add(t0);
                }
            }
        }

        // LOD seam fix
        //if (stride > 1)
        //{
        //    for (int y = 0; y < vertsY-1; ++y)
        //    {
        //        int yy = y * stride;
        //        int v = m_tempVerts.Count;
        //        m_tempVerts.Add(new Vector3(0, GetHeight(ox, oy + yy + 1) - stride, yy + 1));
        //
        //        int t0 = (0) + (y + 0) * vertsX;
        //        int t1 = (0) + (y + 1) * vertsX;
        //        int t2 = v;
        //
        //        m_tempUV.Add(m_tempUV[t0]);
        //        m_tempNormals.Add(m_tempNormals[t0]);
        //
        //        m_tempTriangles.Add(t2);
        //        m_tempTriangles.Add(t1);
        //        m_tempTriangles.Add(t0);
        //    }
        //}

        mesh.SetVertices(m_tempVerts);
        mesh.SetUVs(0, m_tempUV);
        mesh.SetNormals(m_tempNormals);
        mesh.SetTriangles(m_tempTriangles, 0);

        mesh.UploadMeshData(true);

        var go = new GameObject("TerrainRenderer_" + stride);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.isStatic = true;

        var mr = go.AddComponent<MeshRenderer>();
        var mf = go.AddComponent<MeshFilter>();

        mr.sharedMaterial = mat;
        mf.sharedMesh = mesh;

        return go;
    }

    private static float GetHeight(SMFData data, int x, int y)
    {
        if (x < 0)
            x = 0;
        if (x >= data.resX)
            x = data.resX - 1;
        if (y < 0)
            y = 0;
        if (y >= data.resY)
            y = data.resY - 1;
        return data.heightMap[x + y * data.resX];
    }

    private static List<Vector3> m_tempVerts;
    private static List<Vector2> m_tempUV;
    private static List<Vector3> m_tempNormals;
    private static List<int> m_tempTriangles;
}
