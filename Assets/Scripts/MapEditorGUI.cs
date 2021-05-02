using UnityEngine;
using UnityEngine.UI;
using MoonSharp.Interpreter;

public class MapEditorGUI : MonoBehaviour
{
    public Button loadButton;

    void Start()
    {
        loadButton.onClick.AddListener(() =>
        {
            var paths = SFB.StandaloneFileBrowser.OpenFilePanel("Open File", "", "", false);
            //S3OUnity.CreateGameObject(paths[0]);
            var scriptText = System.IO.File.ReadAllText(paths[0]);
            //Debug.Log(script);
            var root = Script.RunString(scriptText);
            var mapinfo = root.Table;
            Debug.Log(mapinfo.Get("name").String);

            var resources = mapinfo.Get("resources").Table;
            foreach (var key in resources.Keys)
            {
                Debug.Log(key.Type + " " + key.String);
            }
            //Debug.Log();//(resources.Get("detailTex").String);
        });

    }

    void Update()
    {
        
    }
}
