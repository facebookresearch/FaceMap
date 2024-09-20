using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticMethods
{
    private static GameObject quitPrompt = null;
    private static GameObject quitFinalPrompt = null;

    /// <summary>
    /// Switches current scene to the next one declared in Build Settings (Unity).
    /// If there are no more scenes in the Build Setting the app is closed.
    /// </summary>
    public static void NextScene()
    {
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex >= UnityEngine.SceneManagement.SceneManager.sceneCount - 1)
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex + 1);
        else
        {
            Debug.Log("No more scenes. Closing app.");
            CloseApp();
        }
    }

    /// <summary>
    /// Creates notification object from specified prefab and assigns it to given reference of the gamObject.
    /// </summary>
    public static void Notify(string notificationPrefabPath, ref GameObject notificationObject)
    {
        if(notificationObject != null)
        {
            GameObject.Destroy(notificationObject);
            notificationObject = null;
        }
        notificationObject = GameObject.Instantiate(Resources.Load(notificationPrefabPath) as GameObject);
    }


    /// <summary>
    /// Closes application. Works for both built app and app run in editor.
    /// </summary>
    public static void CloseApp()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
            Application.Quit();
    }

    /// <summary>
    /// Displays prompt asking participant whether he wants to quit the app.
    /// </summary>
    public static void QuitPrompt()
    {
        Notify(PrefabsPaths.quitPrompt, ref quitPrompt);
        //return GameObject.Instantiate(Resources.Load(Variables.quitPrompt) as GameObject);
    }
    public static void QuitFinalPrompt()
    {
        Notify(PrefabsPaths.quitFinalPrompt, ref quitFinalPrompt);
    }
}
