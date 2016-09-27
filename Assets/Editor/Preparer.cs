using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class Preparer
{
    static void PrepareSimulation()
    {
        SceneNameGetter.ParseXmlConfig(Application.dataPath + "/config.xml");
        if (SceneNameGetter.Mode == "generation")
        {
            EditorSceneManager.OpenScene("Assets/_Scenes/main.unity", OpenSceneMode.Single);
            SceneGenerator.Start(SceneNameGetter.MapSize);
            EditorApplication.Exit(0);
        }
        else
	    {
            EditorSceneManager.OpenScene(string.Format("Assets/Scenes/{0}.unity", SceneNameGetter.SceneName), OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
            
        }

    }

    static void CloseAfterImporting()
    {
        EditorApplication.Exit(0);
    }
}
