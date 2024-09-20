using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitPromptLogic : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 0;
        SessionData.LockedUserInput = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Quit()
    {
        StaticMethods.CloseApp();
    }

    public void Continue()
    {
        Time.timeScale = 1;
        SessionData.LockedUserInput = false;
        GameObject.Destroy(transform.gameObject);
    }
}
