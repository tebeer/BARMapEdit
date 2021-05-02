using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public enum RoadType
{
    Circular,
    Straight,
}

public enum RoadCurveMode
{
    Random,
    Noise,
}

[ExecuteInEditMode]
public class ProceduralTerrain : MonoBehaviour
{
    public Material roadIndicatorMaterial;
    public Material material;
    public Texture2D detail1;
    public Vector4 detail1ST = new Vector4(1, 1, 0, 0);
    public Texture2D detail2;
    public Vector4 detail2ST = new Vector4(1, 1, 0, 0);
    public Texture2D detail3;
    public Vector4 detail3ST = new Vector4(1, 1, 0, 0);
    public Texture2D detail4;
    public Vector4 detail4ST = new Vector4(1, 1, 0, 0);

    public float GetHeightBilinear(Vector3 pos)
    {
        pos /= m_scale;

        int x = (int)pos.x;
        int y = (int)pos.z;

        int x1 = Mathf.Max(0, x);
        int x2 = Mathf.Min(m_resX - 1, x + 1);
        int y1 = Mathf.Max(0, y);
        int y2 = Mathf.Min(m_resY - 1, y + 1);

        float h1 = GetHeight(x1, y1);
        float h2 = GetHeight(x2, y1);
        float h3 = GetHeight(x1, y2);
        float h4 = GetHeight(x2, y2);

        float fx = pos.x - x;
        float fy = pos.z - y;

        float hx1 = Mathf.Lerp(h1, h2, fx);
        float hx2 = Mathf.Lerp(h3, h4, fx);

        return Mathf.Lerp(hx1, hx2, fy) * m_scale;
    }

    public Vector3 GetNormalBilinear(Vector3 pos)
    {
        float l = GetHeightBilinear(pos + new Vector3(-1, 0, 0));
        float r = GetHeightBilinear(pos + new Vector3(1, 0, 0));
        float u = GetHeightBilinear(pos + new Vector3(0, 0, 1));
        float d = GetHeightBilinear(pos + new Vector3(0, 0, -1));

        return new Vector3(l - r, 2, d - u).normalized;
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        Clear();

        m_chunkParent = new GameObject("Chunks").transform;
        m_chunkParent.transform.localScale *= m_scale;
        m_chunkParent.transform.parent = transform;
        
        m_heightMap = new float[m_resX * m_resY];
        var colors = new Color[m_resX * m_resY];

        Random.InitState(m_seed);

        var noiseSeed = new Vector2(Random.value, Random.value);

        for (int y = 0; y < m_resY; ++y)
        {
            for (int x = 0; x < m_resX; ++x)
            {
                int i = x + y * m_resX;

                int s = 1;
                for (int h = 0; h < m_noiseOctaves; ++h)
                {
                    var coord = new Vector2(noiseSeed.x + h + (float)x / m_resX * s, noiseSeed.y + (float)y / m_resX * s);
                    coord = Vector2.Scale(coord, m_noiseScale);
                    m_heightMap[i] += Mathf.PerlinNoise(coord.x, coord.y) * m_resX / s;
                    s *= 2;
                }
                m_heightMap[i] *= m_heightFactor;
                m_heightMap[i] += m_hillFactor * x;
            }
        }
        for (int y = 0; y < m_resY; ++y)
        {
            for (int x = 0; x < m_resX; ++x)
            {
                var l = GetHeight(x - 1, y + 0);
                var r = GetHeight(x + 1, y + 0);
                var u = GetHeight(x + 0, y + 1);
                var d = GetHeight(x + 0, y - 1);

                int i = x + y * m_resX;
                var normal = new Vector3(l - r, 2, d - u).normalized;
                colors[i] = Color.Lerp(new Color(1, 0, 0, 0), new Color(0, 0, 1, 0), normal.y > 0.85f ? 0 : 1);
            }
        }

        const int chunkSize = 32;
        if (m_resX < chunkSize || m_resY < chunkSize)
            throw new System.Exception("Minimum Res is " + chunkSize);

        int divisionsX = m_resX / chunkSize;
        int divisionsY = m_resY / chunkSize;

        m_tempVerts = new List<Vector3>(chunkSize * chunkSize);
        m_tempUV = new List<Vector2>(chunkSize * chunkSize);
        m_tempNormals = new List<Vector3>(chunkSize * chunkSize);
        m_tempTriangles = new List<int>(chunkSize * chunkSize * 6);

        m_splatMap = new Texture2D(m_resX, m_resY);
        m_splatMap.SetPixels(colors);
        m_splatMap.Apply();

        m_material = Object.Instantiate(material);
        SetMaterialProps();

        for (int x = 0; x < divisionsX; ++x)
        {
            for (int y = 0; y < divisionsY; ++y)
            {
                GenerateChunk(m_material, x * chunkSize, y * chunkSize, chunkSize, chunkSize);
            }
        }

        transform.localScale = Vector3.one * m_scale;

        m_tempVerts = null;
        m_tempUV = null;
        m_tempNormals = null;
        m_tempTriangles = null;
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; ++i)
            DestroyImmediate(transform.GetChild(0).gameObject);

        if (m_material != null)
            DestroyImmediate(m_material);

        if (m_splatMap != null)
            DestroyImmediate(m_splatMap);

        m_heightMap = null;

        Resources.UnloadUnusedAssets();
    }

    private float GetHeight(int x, int y)
    {
        if(x < 0)
            x = 0;
        if(x >= m_resX)
            x = m_resX-1;
        if(y < 0)
            y = 0;
        if(y >= m_resY)
            y = m_resY-1;
        return m_heightMap[x + y * m_resX];
    }

    private void GetXY(out int x, out int y, Vector3 pos)
    {
        x = (int)pos.x;
        y = (int)pos.z;

        if (x < 0)
            x = 0;
        if (x >= m_resX)
            x = m_resX - 1;
        if (y < 0)
            y = 0;
        if (y >= m_resY)
            y = m_resY - 1;
    }

    private void GenerateChunk(Material mat, int ox, int oy, int resX, int resY)
    {
        var root = new GameObject($"Chunk_{ox}_{oy}");
        root.transform.parent = m_chunkParent;
        root.transform.localPosition = new Vector3(ox, 0, oy);
        root.transform.localScale = Vector3.one;
        root.isStatic = true;

        var lodGroup = root.AddComponent<LODGroup>();
        var lods = new LOD[4];

        int stride = 1;
        for (int i = 0; i < lods.Length; ++i)
        {
            var lod = GenerateMesh(mat, ox, oy, resX, resY, stride);
            stride *= 2;
            lod.transform.parent = root.transform;
            lod.transform.localPosition = Vector3.zero;
            lod.transform.localScale = Vector3.one;
            lods[i].renderers = new Renderer[] { lod.GetComponent<Renderer>() };
            lods[i].screenRelativeTransitionHeight = 1.0f / (stride);
        }

        lodGroup.SetLODs(lods);
    }

    private GameObject GenerateMesh(Material mat, int ox, int oy, int resX, int resY, int stride)
    {
        var mesh = new Mesh();

        int vertsX = resX / stride;
        int vertsY = resY / stride;
        if (ox + resX < m_resX)
            vertsX += 1;
        if (oy + resY < m_resY)
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
                var uv = new Vector2((float)(ox + xx) / m_resX, (float)(oy + yy) / m_resY);
                //if (x % 2 == 1 && x > 0 && y > 0 && x < vertsX-1 && y < vertsY-1)
                //{
                //    m_tempVerts.Add(new Vector3(
                //        xx,
                //        .5f * (GetHeight(ox + xx, oy + yy) + GetHeight(ox + xx, oy + yy + stride)),
                //        yy + .5f * stride));
                //    uv.y += 0.5f * stride / m_resY;
                //
                //    var l1 = GetHeight(ox + xx - stride, oy + yy + stride);
                //    var r1 = GetHeight(ox + xx + stride, oy + yy + stride);
                //    var u1 = GetHeight(ox + xx, oy + yy + stride + stride);
                //    var d1 = GetHeight(ox + xx, oy + yy - stride + stride);
                //    var l2 = GetHeight(ox + xx - stride, oy + yy + stride);
                //    var r2 = GetHeight(ox + xx + stride, oy + yy + stride);
                //    var u2 = GetHeight(ox + xx, oy + yy + stride + stride);
                //    var d2 = GetHeight(ox + xx, oy + yy - stride + stride);
                //
                //    var n1 = new Vector3(l1 - r1, 2 * stride, d1 - u1).normalized;
                //    var n2 = new Vector3(l2 - r2, 2 * stride, d2 - u2).normalized;
                //    m_tempNormals.Add(((n1 + n2) * .5f).normalized);
                //}
                //else
                {
                    m_tempVerts.Add(new Vector3(xx, GetHeight(ox + xx, oy + yy), yy));
                    var l = GetHeight(ox + xx - stride, oy + yy);
                    var r = GetHeight(ox + xx + stride, oy + yy);
                    var u = GetHeight(ox + xx, oy + yy + stride);
                    var d = GetHeight(ox + xx, oy + yy - stride);

                    m_tempNormals.Add(new Vector3(l - r, 2 * stride, d - u).normalized);
                }

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

        var go = new GameObject("TerrainRenderer_"+stride);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.isStatic = true;

        var mr = go.AddComponent<MeshRenderer>();
        var mf = go.AddComponent<MeshFilter>();

        mr.sharedMaterial = mat;
        mf.sharedMesh = mesh;

        return go;
    }

    private void OnEnable()
    {
        // Properties aren't saved to the material, must set them again
        SetMaterialProps();
    }

    private void SetMaterialProps()
    {
        if (m_material != null)
        {
            var st1 = detail1ST;
            var st2 = detail2ST;
            var st3 = detail3ST;
            var st4 = detail4ST;
            st1.x *= (float)m_resX / 512;
            st1.y *= (float)m_resY / 512;
            st2.x *= (float)m_resX / 512;
            st2.y *= (float)m_resY / 512;
            st3.x *= (float)m_resX / 512;
            st3.y *= (float)m_resY / 512;
            st4.x *= (float)m_resX / 512;
            st4.y *= (float)m_resY / 512;

            m_material.SetTexture("_MainTex", m_splatMap);
            m_material.SetTexture("_Control", m_splatMap);
            m_material.SetTexture("_Splat0", detail1);
            m_material.SetVector("_Splat0_ST", st1);
            m_material.SetTexture("_Splat1", detail2);
            m_material.SetVector("_Splat1_ST", st2);
            m_material.SetTexture("_Splat2", detail3);
            m_material.SetVector("_Splat2_ST", st3);
            m_material.SetTexture("_Splat3", detail4);
            m_material.SetVector("_Splat3_ST", st4);
        }
    }


    [SerializeField]
    private int m_resX = 256;
    [SerializeField]
    private int m_resY = 256;
    [SerializeField]
    private float m_scale = 2.0f;

    [SerializeField]
    private float m_heightFactor;
    [SerializeField]
    private float m_hillFactor;
    [SerializeField]
    private int m_noiseOctaves;
    [SerializeField]
    private Vector2 m_noiseScale = Vector2.one;
    [SerializeField]
    private float m_roadWidth;
    [SerializeField]
    private float m_roadEdgeWidth;
    [SerializeField]
    private float m_roadSmooth;
    [SerializeField]
    private int m_roadPointCount;
    [SerializeField]
    private RoadCurveMode m_roadCurveMode;
    [SerializeField]
    private float m_roadCurveFactor = 0.5f;
    [SerializeField]
    private float m_roadHeightMin = -1;
    [SerializeField]
    private float m_roadHeightMax = 1;
    [SerializeField]
    private float m_roadCurveDamp;
    [SerializeField]
    private int m_roadCurveNoiseOctaves = 4;
    [SerializeField]
    private float m_roadCurveNoiseScale = 1.0f;
    [SerializeField]
    private RoadType m_roadType;
    [SerializeField]
    private int m_seed;
    [SerializeField]
    private Texture2D m_splatMap;
    [SerializeField]
    private Material m_material;

    private static List<Vector3> m_tempVerts;
    private static List<Vector2> m_tempUV;
    private static List<Vector3> m_tempNormals;
    private static List<int> m_tempTriangles;
    private Transform m_chunkParent;
    private float[] m_heightMap;
}
