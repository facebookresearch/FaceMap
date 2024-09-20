using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleScreenLogic : MonoBehaviour
{
    /// <summary>
    /// Allows to select which key is responsible for progressing to the next scene.
    /// </summary>
    public KeyCode keyToContinue = KeyCode.Return;

    /// <summary>
    /// If parameter is set to yes, application will be closed after pressing key responsible for progressing.
    /// </summary>
    public bool lastScene = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (SessionData.LockedUserInput)
            return;

        // Progressing to the next scene
        if (Input.GetKeyDown(keyToContinue))
        {
            if (!lastScene)
            {
                StaticMethods.NextScene();
            }
            else
            {
                StaticMethods.CloseApp();
            }
        }

        // Quiting the experiment
        if (Input.GetKey(KeyCode.Escape))
        {
            StaticMethods.QuitPrompt();
        }

    }
}
