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

[ScriptedImporter(1, "sd7")]
public class SD7Importer : ScriptedImporter
{
    public float m_Scale = 1;

    const string MapInfoLuaFileName = "mapinfo.lua";

    private List<string> VFSDirList(string dir, string ext)
    {
        return new List<string>();
    }

    private Table getfenv()
    {
        return m_dummy;
    }

    private Table m_dummy;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        SMFData data;
        byte[][] tiles;
        Script mapInfoScript;
        Table mapInfoTable;
        Texture2D normalMap;

        using (var sd7File = new ArchiveFile(ctx.assetPath))
        {
            var mapInfoEntry = sd7File.GetEntry(MapInfoLuaFileName);

            var mapInfoLua = mapInfoEntry.ExtractAsString();

            mapInfoScript = new Script();
            DynValue root = null;

            var VFS = new Table(mapInfoScript);
            VFS["DirList"] = (System.Func<string, string, List<string>>)VFSDirList;

            mapInfoScript.Globals["VFS"] = VFS;

            m_dummy = new Table(mapInfoScript);
            mapInfoScript.Globals["getfenv"] = (System.Func<Table>)getfenv;

            try
            {
                root = mapInfoScript.DoString(mapInfoLua);
            }
            catch (ScriptRuntimeException ex)
            {
                Debug.LogError("Lua error: " + ex.DecoratedMessage);
            }

            mapInfoTable = root.Table;

            var mapFileName = mapInfoTable.Get("mapfile").String;

            var mapFileEntry = sd7File.GetEntry(mapFileName);
            using (var memoryStream = new MemoryStream())
            {
                mapFileEntry.Extract(memoryStream);
                memoryStream.Position = 0;
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    data = SMFUnity.LoadSMF(reader);

                    var smfTable = mapInfoTable.Get("smf").Table;

                    data.header.minHeight = (float)smfTable.Get("minheight").Number;
                    data.header.maxHeight = (float)smfTable.Get("maxheight").Number;

                    data.heightMap = SMFUnity.LoadHeightMap(reader, data.header);
                }
            }

            var dir = Path.GetDirectoryName(mapFileName);

            tiles = SMFUnity.LoadTileFiles(data, (name) =>
            {
                var tileFilePath = Path.Combine(dir, name);
                var tileFileEntry = sd7File.GetEntry(tileFilePath);

                var memoryStream = new MemoryStream();
                tileFileEntry.Extract(memoryStream);
                memoryStream.Position = 0;

                return new BinaryReader(memoryStream);
            });

            var resources = mapInfoTable.Get("resources").Table;
            string normalTexName = resources.Get("detailnormaltex").String;

            normalMap = LoadDDSTexture(sd7File, normalTexName);
            normalMap.name = "detailnormaltex";
        }

        var map = SMFUnity.CreateMapObject(data.header, data, data.tileIndices, tiles, normalMap);

        SMFImporter.AddMapObject(ctx, map);
    }

    private Texture2D LoadDDSTexture(ArchiveFile sd7File, string name)
    {
        var entry = sd7File.GetEntry("maps/"+name);

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
            Debug.Log(mipmapCount);

            Texture2D texture = new Texture2D(width, height, TextureFormat.DXT1, Mathf.Max(mipmapCount, 1), false);
            texture.LoadRawTextureData(dxtBytes);
            texture.Apply();

            return texture;
        }
    }

}