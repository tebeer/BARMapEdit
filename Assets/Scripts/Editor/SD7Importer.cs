using UnityEngine;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "sd7")]
public class SD7Importer : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var sd7Data = SD7Unity.LoadSD7(ctx.assetPath);
        var data = MapUtil.CreateMapGameObjects(sd7Data);
        SMFImporter.AddMapObject(ctx, data.mapGameObject);
    }
}