using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuManager : MonoBehaviour
{
    public string electricCarSceneName = "Araba"; 
    public string dcEngineSceneName = "BasicScene"; 
    public void loadElectricCarScene()
    {
        if (!string.IsNullOrEmpty(electricCarSceneName))
        {
            SceneManager.LoadScene(electricCarSceneName);
        }
    }
    public void loadDcEngineScene()
    {
        if (!string.IsNullOrEmpty(dcEngineSceneName))
        {
            SceneManager.LoadScene(dcEngineSceneName);
        }
    }
    public void quitApplication()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}