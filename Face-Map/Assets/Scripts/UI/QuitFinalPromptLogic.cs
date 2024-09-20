using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class QuitFinalPromptLogic : MonoBehaviour
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
        Debug.Log("Quit Final <<<<");
        int sess = int.Parse(SessionData.SessionID);
        string expfile = SessionData.ResultsDirectory + $"/{SessionData.UserID}_{sess}_ASAP.csv";
        Debug.Log($"Quit Final {expfile}");
        string target_file = SessionData.ResultsDirectory + $"/head{sess}_Experiment_History.csv";
        Debug.Log($"Quit to save {target_file}");
        File.Copy(expfile, target_file, true);
        StaticMethods.CloseApp();
    }

    public void Continue()
    {
        Time.timeScale = 1;
        SessionData.LockedUserInput = false;
        GameObject.Destroy(transform.gameObject);
    }
}
