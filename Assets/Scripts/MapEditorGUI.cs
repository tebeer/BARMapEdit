using UnityEngine;
using ImGuiNET;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class ImGuiEx
{
    public static bool Color(string label, ref Color color)
    {
        Vector4 vec = color;
        bool ret = ImGui.ColorEdit4(label, ref vec);
        color = vec;
        return ret;
    }
}

public class MapEditorGUI : MonoBehaviour
{
    public CameraControls cameraControls;
    public Texture2D noTexture;

    private void OnEnable()
    {
        ImGuiUn.Layout += OnLayout;
    }

    private void OnDisable()
    {
        ImGuiUn.Layout -= OnLayout;
    }

    private void Start()
    {
        m_barExePath = PlayerPrefs.GetString("BARFolderPath", "");
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
        if (m_sd7Data != null)
        {
            var pivot = cameraControls.pivot.transform.position;
            pivot.y = SMFUnity.GetHeight(m_sd7Data.smfData, pivot.x, -pivot.z);
            cameraControls.pivot.transform.position = pivot;
        }
    }

    void OnLayout()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if(ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New Map"))
                    OnMenuNewMap();
                if (ImGui.MenuItem("Load Map"))
                    OnMenuLoadMap();
                if (ImGui.MenuItem("Save"))
                    OnMenuSaveMap();
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Run"))
            {
                if (ImGui.MenuItem("Locate BAR Executable"))
                    LocateExecutable();
                ImGui.Separator();
                if (ImGui.MenuItem("Test Map"))
                    RunSpring();
                ImGui.EndMenu();
            }
            ImGui.EndMenuBar();
        }
        ImGui.End();

        if (m_sd7Data != null)
            PropertiesWindow();

        if (m_previewTexture != null)
            PreviewTextureWindow();

        ConsoleWindow();
    }

    private void ConsoleWindow()
    {
        //ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Console", ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            ImGui.TextUnformatted(m_consoleText);
            ImGui.End();
        }
        //for (int i = 0; i < m_mapData.mapInfoScript.SourceCodeCount; ++i)
        //{
        //    var sourceCode = m_mapData.mapInfoScript.GetSourceCode(i);
        //
        //    string name = string.IsNullOrEmpty(sourceCode.Name) ? "<null>" : sourceCode.Name;
        //    //string code = string.IsNullOrEmpty(sourceCode.Code) ? "<null>" : sourceCode.Code;
        //    if (ImGui.Begin(name))
        //    {
        //        ImGui.Text("code");
        //        ImGui.End();
        //    }
        //}
    }

    private void PropertiesWindow()
    {
        //ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Properties", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
        {
            TextureEditor("detailTex", ref m_sd7Data.textures.detailTex);
            TextureEditor("detailNormalTex", ref m_sd7Data.textures.detailNormalTex);
            TextureEditor("specularTex", ref m_sd7Data.textures.specularTex);
            TextureEditor("splatDistrTex", ref m_sd7Data.textures.splatDistrTex);
            TextureEditor("splatDetailTex", ref m_sd7Data.textures.splatDetailTex);
            TextureEditor("splatDetailNormalTex1", ref m_sd7Data.textures.splatDetailNormalTex1);
            TextureEditor("splatDetailNormalTex2", ref m_sd7Data.textures.splatDetailNormalTex2);
            TextureEditor("splatDetailNormalTex3", ref m_sd7Data.textures.splatDetailNormalTex3);
            TextureEditor("splatDetailNormalTex4", ref m_sd7Data.textures.splatDetailNormalTex4);

            ImGuiEx.Color("groundAmbientColor", ref m_sd7Data.lighting.groundAmbientColor);
            //ImGui.DragFloat3("sunDir", ref m_mapData.lighting.sunDir);
            //ImGui.InputFloat3("sunDir", ref m_mapData.lighting.sunDir);
            if (ImGui.SliderFloat3("sunDir", ref m_sd7Data.lighting.sunDir, -1.0f, 1.0f))
            {
                m_mapEditorData.sunGO.transform.rotation = Quaternion.LookRotation(new Vector3(m_sd7Data.lighting.sunDir.x, m_sd7Data.lighting.sunDir.y, -m_sd7Data.lighting.sunDir.z));
            }


            ImGui.End();
        }
    }

    private static bool ImageButton(Texture tex, Vector2 size)
    {
        return ImGui.ImageButton((System.IntPtr)ImGuiUn.GetTextureId(tex), size);
    }

    private void TextureEditor(string label, ref Texture2D tex)
    {
        var shownTexture = tex;
        if (shownTexture == null)
            shownTexture = noTexture;

        if (ImageButton(shownTexture, new Vector2(32, 32)))
        {
            m_previewTexture = shownTexture;
        }

        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(label);
    }

    private void PreviewTextureWindow()
    {
        ImGui.SetNextWindowSizeConstraints(new Vector2(64, 64), new Vector2(512, 512));
        var flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.HorizontalScrollbar;
        bool open = true;
        if (ImGui.Begin(m_previewTexture.name, ref open, flags))
        {
            ImGuiUn.Image(m_previewTexture);
            ImGui.End();
            if (!open)
                m_previewTexture = null;
        }
    }

    private void OnMenuNewMap()
    {
        ClearMap();
    }

    private void OnMenuLoadMap()
    {
        var paths = SFB.StandaloneFileBrowser.OpenFilePanel("Open File", "", "sd7", false);

        if (paths.Length == 0)
            return;

        ClearMap();

        m_sd7Data = SD7Unity.LoadSD7(paths[0]);

        float maxHeight = float.MinValue;

        var hmap = m_sd7Data.smfData.heightMap;

        for (int i = 0; i < hmap.Length; ++i)
            if (hmap[i] > maxHeight)
                maxHeight = hmap[i];

        var resX = m_sd7Data.smfData.resX;
        var resY = m_sd7Data.smfData.resY;
        var scale = m_sd7Data.smfData.scale;

        cameraControls.pivot.transform.position = new Vector3(resX * scale / 2, maxHeight, -resY * scale / 2);
        cameraControls.transform.localPosition = new Vector3(0, 0, -resX * scale / 2);

        m_mapEditorData = MapUtil.CreateMapGameObjects(m_sd7Data);
    }

    private void OnMenuSaveMap()
    {
    }

    private void ClearMap()
    {
        if (m_sd7Data != null)
        {
            DestroyIfNotNull(m_sd7Data.textures.detailNormalTex);
            DestroyIfNotNull(m_sd7Data.textures.detailTex);
            DestroyIfNotNull(m_sd7Data.textures.detailNormalTex);
            DestroyIfNotNull(m_sd7Data.textures.specularTex);
            DestroyIfNotNull(m_sd7Data.textures.splatDistrTex);
            DestroyIfNotNull(m_sd7Data.textures.splatDetailTex);
            DestroyIfNotNull(m_sd7Data.textures.splatDetailNormalTex1);
            DestroyIfNotNull(m_sd7Data.textures.splatDetailNormalTex2);
            DestroyIfNotNull(m_sd7Data.textures.splatDetailNormalTex3);
            DestroyIfNotNull(m_sd7Data.textures.splatDetailNormalTex4);
            m_sd7Data = null;
        }

        if (m_mapEditorData.mapGameObject != null)
        {
            Destroy(m_mapEditorData.mapGameObject);
            m_mapEditorData = default;
        }
    }

    private void DestroyIfNotNull(Object obj)
    {
        if (obj != null)
            DestroyImmediate(obj);
    }

    private bool LocateExecutable()
    {
        var paths = SFB.StandaloneFileBrowser.OpenFilePanel("Locate BAR executable", "", "exe", false);
        if (paths.Length > 0)
        {
            m_barExePath = paths[0];
        }

        PlayerPrefs.SetString("BARFolderPath", m_barExePath);

        if (!File.Exists(m_barExePath))
        {
            m_barExePath = null;
            return false;
        }

        return true;
    }

    private void RunSpring()
    {
        var dataDir = Path.Combine(Path.GetDirectoryName(m_barExePath), "data");
        var scriptPath = Path.Combine(Application.temporaryCachePath, "script.txt");
        var enginePath = Path.Combine(dataDir, "engine");
        var springExePaths = Directory.GetFiles(enginePath, "spring.exe", SearchOption.AllDirectories);
        if (springExePaths.Length == 0)
            throw new System.Exception("spring.exe not found");
        var springExePath = springExePaths[0];

        string map = "Altair_Crossing_V4";
        string game = "Beyond All Reason test-16101-c3567c2";

        var script =
        $"[GAME]" +
        $"{{" +
        $"    [allyteam0]" +
        $"    {{" +
        $"        numallies=0;" +
        $"        startrectbottom=1;" +
        $"        startrectleft=0;" +
        $"        startrecttop=0;" +
        $"        startrectright=1;" +
        $"    }}" +
        $"    [team0]" +
        $"    {{" +
        $"        teamleader=0;" +
        $"        rgbcolor=0 0.32 1;" +
        $"        allyteam=0;" +
        $"        handicap=0;" +
        $"        side=Armada;" +
        $"    }}" +
        $"    [player0]" +
        $"    {{" +
        $"        name=BARMapEdit;" +
        $"        rank=0;" +
        $"        isfromdemo=0;" +
        $"        team=0;" +
        $"    }}" +
        $"    myplayername=BARMapEdit;" +
        $"    startpostype=2;" +
        $"    mapname={map};" +
        $"    gametype={game};" +
        $"    ishost=1;" +
        $"    numusers=4;" +
        $"    numplayers=1;" +
        $"    numplayers=1;" +
        $"    nohelperais=0;" +
        $"    recorddemo=0;" +
        $"}}";

        File.WriteAllText(scriptPath, script);

        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = springExePath,
                Arguments = $"-write-dir \"{dataDir}\" {scriptPath}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            }
        };
        
        proc.Start();

        var sb = new System.Text.StringBuilder();
        while (!proc.StandardOutput.EndOfStream)
        {
            sb.AppendLine(proc.StandardOutput.ReadLine());
        }

        m_consoleText = sb.ToString();
    }

    private SD7Data m_sd7Data;
    private MapEditorData m_mapEditorData;
    private Texture m_previewTexture;

    private string m_barExePath;
    private string m_consoleText = "";
}
