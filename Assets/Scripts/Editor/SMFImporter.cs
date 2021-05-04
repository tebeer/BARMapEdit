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

        AddMapObject(ctx, map);
    }

    public static void AddMapObject(AssetImportContext ctx, GameObject map)
    {
        AddRecursive(ctx, map);
        ctx.SetMainObject(map);

        var objects = new HashSet<Object>();
        foreach (var mr in map.GetComponentsInChildren<MeshRenderer>())
        {
            objects.Add(mr.sharedMaterial);
            objects.Add(mr.sharedMaterial.GetTexture("_Map"));
            objects.Add(mr.sharedMaterial.GetTexture("_Normal"));
        }

        foreach (var o in objects)
        {
            if(o != null)
                ctx.AddObjectToAsset(o.name, o);
        }
    }

    private static void AddRecursive(AssetImportContext ctx, GameObject go)
    {
        ctx.AddObjectToAsset(go.name, go);
        var mf = go.GetComponent<MeshFilter>();
        if(mf != null)
            ctx.AddObjectToAsset(mf.sharedMesh.name, mf.sharedMesh);
        for (int i = 0; i < go.transform.childCount; ++i)
            AddRecursive(ctx, go.transform.GetChild(i).gameObject);
    }

}