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

public static class SD7Unity
{
    public static MapData LoadSD7(string path)
    {
        var mapData = new MapData();

        byte[][] tiles;
        Texture2D normalMap;

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
            string normalTexName = resources.Get("detailnormaltex").String;

            normalMap = LoadDDSTexture(sd7File, normalTexName);
            normalMap.name = "detailnormaltex";
        }

        foreach (var kv in mapData.mapInfoTable.Keys)
            Debug.Log(kv.Type + " " + kv.String);

        mapData.mapGameObject = SMFUnity.CreateMapObject(mapData.smfData.header, mapData.smfData, mapData.smfData.tileIndices, tiles, normalMap);

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

    private static Texture2D LoadDDSTexture(ArchiveFile sd7File, string name)
    {
        var entry = sd7File.GetEntry("maps/" + name);

        using (var memoryStream = new MemoryStream())
        {
            entry.Extract(memoryStream);

            var ddsBytes = memoryStream.GetBuffer();
            int ddsBytesLength = (int)memoryStream.Position;

            byte ddsSizeCheck = ddsBytes[4];
            if (ddsSizeCheck != 124)
                throw new System.Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

            int height = ddsBytes[13] * 256 + ddsBytes[12];
            int width = ddsBytes[17] * 256 + ddsBytes[16];

            int DDS_HEADER_SIZE = 128;
            byte[] dxtBytes = new byte[ddsBytesLength - DDS_HEADER_SIZE];
            System.Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytesLength - DDS_HEADER_SIZE);

            int mipmapCount = ddsBytes[24];

            Texture2D texture = new Texture2D(width, height, TextureFormat.DXT1, Mathf.Max(mipmapCount, 1), false);
            texture.LoadRawTextureData(dxtBytes);
            texture.Apply();

            return texture;
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
