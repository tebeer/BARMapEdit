using UnityEngine;
using SevenZipExtractor;
using System.IO;
using System.Text;
using MoonSharp.Interpreter;
using System.Collections.Generic;

public static class TableExtensions
{
    public static DynValue Get2(this Table table, string name)
    {
        var value = table.Get(name);
        if (value != null && value.Type != DataType.Nil)
            return value;
        return table.Get(name.ToLower());
    }
}

public static class SevenZipExtensions
{
    public static Entry GetEntry(this ArchiveFile archiveFile, string name)
    {
        name = name.ToLower().Replace('\\', '/');
        foreach (var entry in archiveFile.Entries)
            if (entry.FileName.ToLower().Replace('\\', '/') == name)
                return entry;
        return null;
    }

    public static string ExtractAsString(this Entry entry)
    {
        if (entry == null)
            throw new System.Exception("null reference");
        using (var memoryStream = new MemoryStream())
        {
            entry.Extract(memoryStream);
            return Encoding.UTF8.GetString(memoryStream.GetBuffer());
        }
    }

    public static byte[] ExtractAsBytes(this Entry entry)
    {
        if (entry == null)
            throw new System.Exception("null reference");
        using (var memoryStream = new MemoryStream())
        {
            entry.Extract(memoryStream);
            return memoryStream.ToArray();
        }
    }
}

public class MapData
{
    public GameObject mapGameObject;
    public SMFData smfData;
    public Table mapInfoTable;
    public byte[][] tiles;
    public MapTextures textures;
}

public struct MapTextures
{
    public Texture2D detailTex;
    public Texture2D detailNormalTex;
    public Texture2D specularTex;
    public Texture2D splatDistrTex;
    public Texture2D splatDetailNormalTex1;
    public Texture2D splatDetailNormalTex2;
    public Texture2D splatDetailNormalTex3;
    public Texture2D splatDetailNormalTex4;
    public Vector4 scales;
    public Vector4 mults;
}

public class VFS : System.IDisposable
{
    public Script Script { get { return m_script; } }

    public VFS(ArchiveFile archive)
    {
        m_archive = archive;
        m_script = new Script();

        var vfsTable = new Table(m_script);
        m_script.Globals["VFS"] = vfsTable;
        vfsTable["DirList"] = (System.Func<string, string, List<string>>)DirList;
        vfsTable["LoadFile"] = (System.Func<string, string>)LoadFile;
        vfsTable["Include"] = (System.Func<string, DynValue>)Include;

        var springTable = new Table(m_script);
        m_script.Globals["Spring"] = springTable;
        springTable["Echo"] = (System.Action<string>)Debug.Log;

        System.Func<Table> getfenv = () => new Table(m_script);
        m_script.Globals["getfenv"] = getfenv;

    }

    public string LoadFile(string fname)
    {
        var entry = m_archive.GetEntry(fname);
        if (entry != null)
            return entry.ExtractAsString();

        fname = Path.Combine(Application.streamingAssetsPath, fname);
        if (File.Exists(fname))
            return File.ReadAllText(fname);

        return null;
    }

    public DynValue Include(string fname)
    {
        var lua = LoadFile(fname);
        if (lua == null)
            return null;

        try
        {
            return m_script.DoString(lua, null, fname);
        }
        catch (ScriptRuntimeException ex)
        {
            Debug.LogError("Lua error: " + ex.DecoratedMessage);
            return null;
        }
    }

    public byte[] LoadBytes(string fname)
    {
        var entry = m_archive.GetEntry(fname);
        if (entry != null)
            return entry.ExtractAsBytes();

        fname = Path.Combine(Application.streamingAssetsPath, fname);
        if (File.Exists(fname))
            return File.ReadAllBytes(fname);

        return null;
    }

    public List<string> DirList(string dir, string ext)
    {
        return new List<string>();
    }

    public void Dispose()
    {
        m_archive.Dispose();
        m_archive = null;
    }

    private ArchiveFile m_archive;
    private Script m_script;
}

public static class SD7Unity
{
    public static MapData LoadSD7(string path)
    {
        var mapData = new MapData();

        using (var vfs = new VFS(new ArchiveFile(path)))
        {
            var mapFilePath = Path.Combine("maps", Path.ChangeExtension(Path.GetFileName(path), "smf"));
            var mapConfigPath = Path.ChangeExtension(mapFilePath, "smd");

            var mapTable = new Table(vfs.Script);
            mapTable.Set("configFile", DynValue.NewString(mapConfigPath));
            vfs.Script.Globals["Map"] = mapTable;

            DynValue root = vfs.Include(MapInfoLuaFileName);
            if(root == null)
                root = vfs.Include(Path.Combine("maphelper/", MapInfoLuaFileName));
            if(root == null)
                throw new System.Exception(MapInfoLuaFileName + " not found");

            mapData.mapInfoTable = root.Table;

            var mapFilePathInMapInfo = mapData.mapInfoTable.Get2("mapfile").String;
            if (mapFilePathInMapInfo != null)
                mapFilePath = mapFilePathInMapInfo;

            using (BinaryReader reader = new BinaryReader(new MemoryStream(vfs.LoadBytes(mapFilePath))))
            {
                mapData.smfData = SMFUnity.LoadSMF(reader);

                var smfTable = mapData.mapInfoTable.Get2("smf").Table;

                mapData.smfData.header.minHeight = (float)smfTable.Get2("minheight").CastToNumber().Value;
                mapData.smfData.header.maxHeight = (float)smfTable.Get2("maxheight").CastToNumber().Value;

                mapData.smfData.heightMap = SMFUnity.LoadHeightMap(reader, mapData.smfData.header);
            }

            var dir = Path.GetDirectoryName(mapFilePath);

            mapData.tiles = SMFUnity.LoadTileFiles(mapData.smfData, (name) =>
            {
                var tileFilePath = Path.Combine(dir, name);
                return new BinaryReader(new MemoryStream(vfs.LoadBytes(tileFilePath)));
            });

            var resources = mapData.mapInfoTable.Get("resources").Table;

            foreach (var p in resources.Pairs)
                Debug.Log(p.Key.String + " " + p.Value.Type + " " + p.Value.String);

            mapData.textures.detailTex = LoadTexture(vfs, resources.Get2("detailTex").String);
            mapData.textures.detailNormalTex = LoadTexture(vfs, resources.Get2("detailNormalTex").String);
            mapData.textures.specularTex = LoadTexture(vfs, resources.Get2("specularTex").String);
            mapData.textures.splatDistrTex = LoadTexture(vfs, resources.Get2("splatDistrTex").String);
            mapData.textures.splatDetailNormalTex1 = LoadTexture(vfs, resources.Get2("splatDetailNormalTex1").String);
            mapData.textures.splatDetailNormalTex2 = LoadTexture(vfs, resources.Get2("splatDetailNormalTex2").String);
            mapData.textures.splatDetailNormalTex3 = LoadTexture(vfs, resources.Get2("splatDetailNormalTex3").String);
            mapData.textures.splatDetailNormalTex4 = LoadTexture(vfs, resources.Get2("splatDetailNormalTex4").String);

            var splats = mapData.mapInfoTable.Get2("splats").Table;
            mapData.textures.scales = splats.GetVector4("texScales");
            mapData.textures.mults = splats.GetVector4("texMults");
        }

        //foreach (var kv in mapData.mapInfoTable.Keys)
        //    Debug.Log(kv.Type + " " + kv.String);

        mapData.mapGameObject = SMFUnity.CreateMapObject(mapData.smfData.header, mapData.smfData, mapData.smfData.tileIndices, mapData.tiles, mapData.textures);

        var atmosphereTable = mapData.mapInfoTable.Get2("atmosphere").Table;
        var sunColor = atmosphereTable.GetColor("suncolor");

        var lightingTable = mapData.mapInfoTable.Get("lighting").Table;
        var sunDir = -lightingTable.GetVector3("sundir");

        var groundAmbientColor = lightingTable.GetColor("groundambientcolor");
        RenderSettings.ambientGroundColor = groundAmbientColor;

        var groundDiffuseColor = lightingTable.GetColor("grounddiffusecolor");
        Shader.SetGlobalColor("_GroundDiffuseColor", groundDiffuseColor);

        CreateSun(mapData, sunColor, sunDir);

        var teamsTable = mapData.mapInfoTable.Get2("teams").Table;
        foreach (var i in teamsTable.Pairs)
        {
            var sp = i.Value.Table.Get("startpos");
            var x = sp.Table.Get2("x").Number;
            var z = sp.Table.Get2("z").Number;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.parent = mapData.mapGameObject.transform;

            var pos = new Vector3((float)x, 0, (float)z);
            pos.y = SMFUnity.GetHeight(mapData.smfData, pos.x, pos.z);
            pos.z = -pos.z;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(50, 50, 50);
        }

        return mapData;
    }

    private static Vector3 GetVector3(this Table table, string name)
    {
        var value = table.Get2(name);
        if (value.Type == DataType.String)
        {
            var split = value.String.Split(' ');
            return new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
        }
        return value.Table.ToVector3();
    }

    private static Vector4 GetVector4(this Table table, string name)
    {
        var value = table.Get2(name);
        if (value.Type == DataType.String)
        {
            var split = value.String.Split(' ');
            return new Vector4(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
        }
        return value.Table.ToVector4();
    }

    private static Color GetColor(this Table table, string name)
    {
        var value = table.Get2(name);
        if (value.Type == DataType.String)
        {
            var split = value.String.Split(' ');
            return new Color(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), split.Length > 3 ? float.Parse(split[3]) : 1.0f);
        }
        return value.Table.ToColor();
    }

    private static Vector3 ToVector3(this Table table)
    {
        var values = table.ToFloatArray();
        return new Vector3(values[0], values[1], values[2]);
    }

    private static Vector4 ToVector4(this Table table)
    {
        var values = table.ToFloatArray();
        return new Vector4(values[0], values[1], values[2], values[3]);
    }

    private static Color ToColor(this Table table)
    {
        var values = table.ToFloatArray();
        var color = new Color(values[0], values[1], values[2]);
        if (values.Length >= 4)
            color.a = values[3];
        return color;
    }

    private static float[] ToFloatArray(this Table table)
    {
        int count = 0;
        foreach (var v in table.Values)
            count++;
        var values = new float[count];
        count = 0;
        foreach (var v in table.Values)
            values[count++] = (float)v.Number;
        return values;
    }

    private static GameObject CreateSun(MapData mapData, Color color, Vector3 dir)
    {
        var go = new GameObject("Sun");
        var light = go.AddComponent<Light>();

        light.type = LightType.Directional;
        light.color = color;
        light.shadows = LightShadows.Hard;

        go.transform.parent = mapData.mapGameObject.transform;
        go.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, dir.y, -dir.z));

        return go;
    }

    private static Texture2D LoadTexture(VFS vfs, string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        var bytes = vfs.LoadBytes("maps/" + name);

        if (bytes == null)
            throw new System.Exception("Texture not found: " + name);

        Texture2D tex;

        var ext = Path.GetExtension(name).ToLower();
        if (ext == ".dds")
            tex = TextureUtil.LoadDDSTexture(bytes, name);
        else if (ext == ".tga")
            tex = TextureUtil.LoadTGATexture(bytes);
        else if (ext == ".bmp")
            tex = TextureUtil.LoadBMPTexture(bytes);
        else
            tex = TextureUtil.LoadSupportedTexture(bytes);

        tex.name = name;
        tex.mipMapBias = -1.0f;
        return tex;
    }

    const string MapInfoLuaFileName = "mapinfo.lua";
}
