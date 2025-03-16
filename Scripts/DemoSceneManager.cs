using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoSceneManager : MonoBehaviour
{
    // This static variable is set before loading the DemoScene.
    public static string targetScene = "";

    void Start()
    {
        StartCoroutine(WaitAndLoadTargetScene());
    }

    IEnumerator WaitAndLoadTargetScene()
    {
        // Wait for 4 seconds in the DemoScene.
        yield return new WaitForSeconds(4f);
        if (!string.IsNullOrEmpty(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogError("Target scene not set! Cannot transition.");
        }
    }
}
