using UnityEngine;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "sd7")]
public class SD7Importer : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var mapData = SD7Unity.LoadSD7(ctx.assetPath);
        SMFImporter.AddMapObject(ctx, mapData.mapGameObject);
    }
}