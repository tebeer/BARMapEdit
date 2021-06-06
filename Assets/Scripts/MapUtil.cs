using UnityEngine;

public static class MapUtil
{
    public static void CreateTeamGameObjects(ref MapEditorData mapData, SD7Data sd7Data)
    {
        for (int i = 0; i < sd7Data.teams.Length; ++i)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.parent = mapData.mapGameObject.transform;

            go.transform.position = sd7Data.teams[i];
            go.transform.localScale = new Vector3(50, 50, 50);
        }
    }

    public static MapEditorData CreateMapGameObjects(SD7Data sd7Data)
    {
        MapEditorData data = default;
        data.mapGameObject = SMFUnity.CreateMapObject(sd7Data.smfData, sd7Data.textures);
        CreateSun(ref data, sd7Data);
        CreateTeamGameObjects(ref data, sd7Data);

        return data;
    }

    public static void CreateSun(ref MapEditorData mapData, SD7Data sd7Data)
    {
        mapData.sunGO = new GameObject("Sun");
        var light = mapData.sunGO.AddComponent<Light>();

        light.type = LightType.Directional;
        light.color = sd7Data.atmosphere.sunColor;
        light.shadows = LightShadows.Hard;

        var sunDir = sd7Data.lighting.sunDir;

        mapData.sunGO.transform.parent = mapData.mapGameObject.transform;
        mapData.sunGO.transform.rotation = Quaternion.LookRotation(new Vector3(sunDir.x, sunDir.y, -sunDir.z));
    }
}
