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
        if (m_mapData != null)
        {
            var pivot = cameraControls.pivot.transform.position;
            pivot.y = SMFUnity.GetHeight(m_mapData.smfData, pivot.x, -pivot.z);
            cameraControls.pivot.transform.position = pivot;
        }
    }

    void OnLayout()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if(ImGui.BeginMenu("File"))
            {
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

        if (m_mapData != null)
        {
            if (ImGui.Begin("Lighting"))
            {
                ImGuiEx.Color("GroundAmbientColor", ref m_mapData.lighting.groundAmbientColor);
                //ImGui.DragFloat3("sunDir", ref m_mapData.lighting.sunDir);
                //ImGui.InputFloat3("sunDir", ref m_mapData.lighting.sunDir);
                if(ImGui.SliderFloat3("sunDir", ref m_mapData.lighting.sunDir, -1.0f, 1.0f))
                    m_mapData.sunGO.transform.rotation = Quaternion.LookRotation(new Vector3(m_mapData.lighting.sunDir.x, m_mapData.lighting.sunDir.y, -m_mapData.lighting.sunDir.z));
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

        Console();
    }

    private void Console()
    {
        //ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Console", ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            ImGui.TextUnformatted(m_consoleText);
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
    private MapData m_mapData;

    private string m_barExePath;
    private string m_consoleText = "";
}
