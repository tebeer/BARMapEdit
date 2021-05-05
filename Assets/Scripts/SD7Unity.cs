using UnityEngine;
using UnityEditor.AssetImporters;
using SevenZipExtractor;
using System.IO;
using System.Text;
using MoonSharp.Interpreter;
using System.Collections.Generic;

public static class SevenZipExtensions
{
    public static Entry GetEntry(this ArchiveFile archiveFile, string name)
    {
        foreach (var entry in archiveFile.Entries)
            if (entry.FileName.Replace('\\', '/') == name.Replace('\\', '/'))
                return entry;
        return null;
    }

    public static string ExtractAsString(this Entry entry)
    {
        using (var memoryStream = new MemoryStream())
        {
            entry.Extract(memoryStream);
            return Encoding.UTF8.GetString(memoryStream.GetBuffer());
        }
    }
}

public class MapData
{
    public GameObject mapGameObject;
    public SMFData smfData;
    public Script mapInfoScript;
    public Table mapInfoTable;
}

public struct MapTextures
{
    public Texture2D detailNormalTex;
    public Texture2D splatDistrTex;
    public Texture2D splatDetailNormalTex1;
    public Texture2D splatDetailNormalTex2;
    public Texture2D splatDetailNormalTex3;
    public Texture2D splatDetailNormalTex4;
}

public static class SD7Unity
{
    public static MapData LoadSD7(string path)
    {
        var mapData = new MapData();

        byte[][] tiles;
        MapTextures textures;

        using (var sd7File = new ArchiveFile(path))
        {
            var mapInfoEntry = sd7File.GetEntry(MapInfoLuaFileName);

            var mapInfoLua = mapInfoEntry.ExtractAsString();

            mapData.mapInfoScript = new Script();
            DynValue root = null;

            var VFS = new Table(mapData.mapInfoScript);
            VFS["DirList"] = (System.Func<string, string, List<string>>)VFSDirList;

            mapData.mapInfoScript.Globals["VFS"] = VFS;

            m_dummy = new Table(mapData.mapInfoScript);
            mapData.mapInfoScript.Globals["getfenv"] = (System.Func<Table>)getfenv;

            try
            {
                root = mapData.mapInfoScript.DoString(mapInfoLua);
            }
            catch (ScriptRuntimeException ex)
            {
                Debug.LogError("Lua error: " + ex.DecoratedMessage);
            }

            mapData.mapInfoTable = root.Table;

            var mapFileName = mapData.mapInfoTable.Get("mapfile").String;

            var mapFileEntry = sd7File.GetEntry(mapFileName);
            using (var memoryStream = new MemoryStream())
            {
                mapFileEntry.Extract(memoryStream);
                memoryStream.Position = 0;
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    mapData.smfData = SMFUnity.LoadSMF(reader);

                    var smfTable = mapData.mapInfoTable.Get("smf").Table;

                    mapData.smfData.header.minHeight = (float)smfTable.Get("minheight").Number;
                    mapData.smfData.header.maxHeight = (float)smfTable.Get("maxheight").Number;

                    mapData.smfData.heightMap = SMFUnity.LoadHeightMap(reader, mapData.smfData.header);
                }
            }

            var dir = Path.GetDirectoryName(mapFileName);

            tiles = SMFUnity.LoadTileFiles(mapData.smfData, (name) =>
            {
                var tileFilePath = Path.Combine(dir, name);
                var tileFileEntry = sd7File.GetEntry(tileFilePath);

                var memoryStream = new MemoryStream();
                tileFileEntry.Extract(memoryStream);
                memoryStream.Position = 0;

                return new BinaryReader(memoryStream);
            });

            var resources = mapData.mapInfoTable.Get("resources").Table;

            textures.detailNormalTex = LoadTexture(sd7File, resources.Get("detailnormaltex").String, false);
            textures.splatDistrTex = LoadTexture(sd7File, resources.Get("splatdistrtex").String, true);
            textures.splatDetailNormalTex1 = LoadTexture(sd7File, resources.Get("splatdetailnormaltex1").String, false);
            textures.splatDetailNormalTex2 = LoadTexture(sd7File, resources.Get("splatdetailnormaltex2").String, false);
            textures.splatDetailNormalTex3 = LoadTexture(sd7File, resources.Get("splatdetailnormaltex3").String, false);
            textures.splatDetailNormalTex4 = LoadTexture(sd7File, resources.Get("splatdetailnormaltex4").String, false);
        }

        foreach (var kv in mapData.mapInfoTable.Keys)
            Debug.Log(kv.Type + " " + kv.String);

        mapData.mapGameObject = SMFUnity.CreateMapObject(mapData.smfData.header, mapData.smfData, mapData.smfData.tileIndices, tiles, textures);

        var atmosphereTable = mapData.mapInfoTable.Get("atmosphere").Table;
        var sunColor = atmosphereTable.GetColor("suncolor");

        var lightingTable = mapData.mapInfoTable.Get("lighting").Table;
        var sunDir = -lightingTable.GetVector3("sundir");

        var groundAmbientColor = lightingTable.GetColor("groundambientcolor");
        RenderSettings.ambientGroundColor = groundAmbientColor;

        var groundDiffuseColor = lightingTable.GetColor("grounddiffusecolor");
        Shader.SetGlobalColor("_GroundDiffuseColor", groundDiffuseColor);

        CreateSun(mapData, sunColor, sunDir);

        return mapData;
    }

    private static Vector3 GetVector3(this Table table, string name)
    {
        return table.Get(name).Table.ToVector3();
    }

    private static Color GetColor(this Table table, string name)
    {
        return table.Get(name).Table.ToColor();
    }

    private static Vector3 ToVector3(this Table table)
    {
        var values = table.ToFloatArray();
        return new Vector3(values[0], values[1], values[2]);
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

    private static Texture2D LoadTexture(ArchiveFile sd7File, string name, bool alpha)
    {
        var entry = sd7File.GetEntry("maps/" + name);

        Texture2D tex;
        byte[] bytes;

        //Debug.Log("LoadTexture " + name);

        using (var memoryStream = new MemoryStream())
        {
            entry.Extract(memoryStream);
            bytes = memoryStream.ToArray();
        }

        var ext = Path.GetExtension(name).ToLower();
        if (ext == ".dds")
            tex = LoadDDSTexture(bytes, alpha);
        else if (ext == ".tga")
            tex = LoadTGATexture(bytes, alpha);
        else
            tex = LoadSupportedTexture(bytes, alpha);

        tex.name = name;
        return tex;
    }

    private static Texture2D LoadSupportedTexture(byte[] bytes, bool alpha)
    {
        Texture2D tex = new Texture2D(2, 2);// alpha ? TextureFormat.DXT5: TextureFormat.DXT1, true);
        if (!tex.LoadImage(bytes))
            return null;
        return tex;
    }

    private static Texture2D LoadDDSTexture(byte[] bytes, bool alpha)
    {
        byte ddsSizeCheck = bytes[4];
        if (ddsSizeCheck != 124)
            throw new System.Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

        int height = bytes[13] * 256 + bytes[12];
        int width = bytes[17] * 256 + bytes[16];

        int DDS_HEADER_SIZE = 128;
        byte[] dxtBytes = new byte[bytes.Length - DDS_HEADER_SIZE];
        System.Buffer.BlockCopy(bytes, DDS_HEADER_SIZE, dxtBytes, 0, bytes.Length - DDS_HEADER_SIZE);

        int mipmapCount = bytes[28];

        //int flags = bytes[80];
        //int fourCC = bytes[84];
        //int dxt1Size = width * height / 2;
        //Debug.Log(fourCC + " " + mipmapCount + " " + dxt1Size + " " + dxtBytes.Length);

        Texture2D texture = new Texture2D(width, height, alpha ? TextureFormat.DXT5 : TextureFormat.DXT1, Mathf.Max(mipmapCount, 1), false);
        texture.LoadRawTextureData(dxtBytes);
        texture.Apply();

        return texture;
    }

    public static Texture2D LoadTGATexture(byte[] bytes, bool usealpha)
    {
        using (BinaryReader r = new BinaryReader(new MemoryStream(bytes)))
        {
            // Skip some header info we don't care about.
            // Even if we did care, we have to move the stream seek point to the beginning,
            // as the previous method in the workflow left it at the end.
            r.BaseStream.Seek(12, SeekOrigin.Begin);

            short width = r.ReadInt16();
            short height = r.ReadInt16();
            int bitDepth = r.ReadByte();

            // Skip a byte of header information we don't care about.
            r.BaseStream.Seek(1, SeekOrigin.Current);

            Texture2D tex = new Texture2D(width, height);
            Color32[] pulledColors = new Color32[width * height];

            if (bitDepth == 32)
            {
                for (int i = 0; i < width * height; i++)
                {
                    byte red = r.ReadByte();
                    byte green = r.ReadByte();
                    byte blue = r.ReadByte();
                    byte alpha = r.ReadByte();

                    pulledColors[i] = new Color32(blue, green, red, alpha);
                }
            }
            else if (bitDepth == 24)
            {
                for (int i = 0; i < width * height; i++)
                {
                    byte red = r.ReadByte();
                    byte green = r.ReadByte();
                    byte blue = r.ReadByte();

                    pulledColors[i] = new Color32(blue, green, red, 1);
                }
            }
            else
            {
                throw new System.Exception("TGA texture had non 32/24 bit depth.");
            }

            tex.SetPixels32(pulledColors);
            tex.Apply();
            return tex;
        }
    }

    const string MapInfoLuaFileName = "mapinfo.lua";

    private static List<string> VFSDirList(string dir, string ext)
    {
        return new List<string>();
    }

    private static Table getfenv()
    {
        return m_dummy;
    }

    private static Table m_dummy;

}
