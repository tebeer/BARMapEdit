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

        using (var sd7File = new ArchiveFile(ctx.assetPath))
        {
            var mapInfoEntry = sd7File.GetEntry(MapInfoLuaFileName);

            var mapInfoLua = mapInfoEntry.ExtractAsString();

            Debug.Log(mapInfoLua);

            var mapInfoScript = new Script();
            DynValue root = null;

            var VFS = new Table(mapInfoScript);
            VFS["DirList"] = (System.Func<string, string, List<string>>)VFSDirList;

            mapInfoScript.Globals["VFS"] = VFS;

            m_dummy = new Table(mapInfoScript);
            mapInfoScript.Globals["getfenv"] = (System.Func<Table>)getfenv;

            //mapInfoScript.Options.DebugPrint = (str) => Debug.Log(str);
            //mapInfoScript.Options.UseLuaErrorLocations = false;
            try
            {
                root = mapInfoScript.DoString(mapInfoLua);
            }
            catch (ScriptRuntimeException ex)
            {
                Debug.LogError("Lua error: " + ex.DecoratedMessage);
            }

            var mapInfoTable = root.Table;

            var mapFileName = mapInfoTable.Get("mapfile").String;

            var mapFileEntry = sd7File.GetEntry(mapFileName);
            using (var memoryStream = new MemoryStream())
            {
                mapFileEntry.Extract(memoryStream);
                memoryStream.Position = 0;
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    data = SMFUnity.LoadSMF(reader);
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

        }

        var map = SMFUnity.CreateMapObject(data.header, data, data.tileIndices, tiles);

        SMFImporter.AddMapObject(ctx, map);
    }
}