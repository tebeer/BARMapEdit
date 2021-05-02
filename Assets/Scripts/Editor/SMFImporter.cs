using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;

[ScriptedImporter(1, "smf")]
public class SMFImporter : ScriptedImporter
{
    public float m_Scale = 1;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        var map = SMFUnity.Load(ctx.assetPath);
        AddRecursive(ctx, map);
        ctx.SetMainObject(map);

        var mr = map.GetComponentInChildren<MeshRenderer>();
        ctx.AddObjectToAsset("material", mr.sharedMaterial);

        var mapTex = mr.sharedMaterial.GetTexture("_Map");
        ctx.AddObjectToAsset("_Map", mapTex);
    }

    private void AddRecursive(AssetImportContext ctx, GameObject go)
    {
        ctx.AddObjectToAsset(go.name, go);
        var mf = go.GetComponent<MeshFilter>();
        if(mf != null)
            ctx.AddObjectToAsset(mf.sharedMesh.name, mf.sharedMesh);
        for (int i = 0; i < go.transform.childCount; ++i)
            AddRecursive(ctx, go.transform.GetChild(i).gameObject);
    }

}