using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Collections.Generic;

[ScriptedImporter(1, "smf")]
public class SMFImporter : ScriptedImporter
{
    public float m_Scale = 1;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        var map = SMFUnity.Load(ctx.assetPath);
        AddRecursive(ctx, map);
        ctx.SetMainObject(map);

        var materials = new HashSet<Material>();
        foreach (var mr in map.GetComponentsInChildren<MeshRenderer>())
            materials.Add(mr.sharedMaterial);

        foreach (var m in materials)
        {
            ctx.AddObjectToAsset(m.name, m);
            var mapTex = m.GetTexture("_Map");
            ctx.AddObjectToAsset(mapTex.name, mapTex);
        }
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