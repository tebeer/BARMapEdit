using UnityEngine;
using UnityEngine.UI;
using ImGuiNET;

public class MapEditorGUI : MonoBehaviour
{
    public CameraControls cameraControls;

    void OnEnable()
    {
        ImGuiUn.Layout += OnLayout;
    }

    void OnDisable()
    {
        ImGuiUn.Layout -= OnLayout;
    }

    void OnLayout()
    {
        ImGui.SetNextWindowSize(new Vector2(Screen.width, Screen.height));
        ImGui.SetNextWindowPos(new Vector2(0, 0));
        if(ImGui.Begin("Editor", ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar))
        {
            if(ImGui.BeginMenuBar())
            {
                if(ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load Map"))
                        OnMenuLoadMap();
                    if (ImGui.MenuItem("Save"))
                        OnMenuSaveMap();
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }
            ImGui.End();
        }
    }

    private void OnMenuLoadMap()
    {
        var paths = SFB.StandaloneFileBrowser.OpenFilePanel("Open File", "", "sd7", false);

        if (paths.Length == 0)
            return;

        if (m_mapData != null && m_mapData.mapGameObject != null)
            Destroy(m_mapData.mapGameObject);

        m_mapData = SD7Unity.LoadSD7(paths[0]);

        float maxHeight = float.MinValue;

        var hmap = m_mapData.smfData.heightMap;

        for (int i = 0; i < hmap.Length; ++i)
            if (hmap[i] > maxHeight)
                maxHeight = hmap[i];

        var resX = m_mapData.smfData.resX;
        var resY = m_mapData.smfData.resY;
        var scale = m_mapData.smfData.scale;

        cameraControls.pivot.transform.position = new Vector3(resX * scale / 2, maxHeight, -resY * scale / 2);
        cameraControls.transform.localPosition = new Vector3(0, 0, -resX * scale / 2);
    }

    private void OnMenuSaveMap()
    {
    }

    void Start()
    {

        //loadImageButton.onClick.AddListener(() =>
        //{
        //    var paths = SFB.StandaloneFileBrowser.OpenFilePanel("Open File", "", "tga", false);
        //
        //    if (paths.Length == 0)
        //        return;
        //
        //    var bytes = System.IO.File.ReadAllBytes(paths[0]);
        //
        //    var tex = new Texture2D(2, 2);
        //    tex.LoadImage(bytes);
        //
        //    Debug.Log(bytes.Length);
        //    Debug.Log(tex.width + " " + tex.height);
        //
        //});
    }

    private void Update()
    {
        if (m_mapData != null)
        {
            var pivot = cameraControls.pivot.transform.position;
            pivot.y = SMFUnity.GetHeight(m_mapData.smfData, pivot.x, -pivot.z);
            cameraControls.pivot.transform.position = pivot;
        }
    }

    private MapData m_mapData;
}
