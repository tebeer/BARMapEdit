using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;

[ScriptedImporter(1, "s3o")]
public class S3OImporter : ScriptedImporter
{
    public float m_Scale = 1;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        var rootPiece = S3OUnity.Load(ctx.assetPath);
        var rootGO = CreateGameObject(ctx, rootPiece, null);
        ctx.SetMainObject(rootGO);
    }

    private GameObject CreateGameObject(AssetImportContext ctx, S3OPiece piece, GameObject parent)
    {
        var gameObject = new GameObject(piece.name);
        if(parent != null)
            gameObject.transform.parent = parent.transform;

        var mr = gameObject.AddComponent<MeshRenderer>();
        var mf = gameObject.AddComponent<MeshFilter>();

        gameObject.transform.localPosition = new Vector3(piece.xoffset, piece.yoffset, piece.zoffset);

        mf.sharedMesh = S3OUnity.CreateMesh(piece);

        ctx.AddObjectToAsset(piece.name, gameObject);

        ctx.AddObjectToAsset(piece.name + "_mesh", mf.sharedMesh);

        for (int i = 0; i < piece.children.Length; ++i)
        {
            CreateGameObject(ctx, piece.children[i], gameObject);
        }

        return gameObject;
    }
}