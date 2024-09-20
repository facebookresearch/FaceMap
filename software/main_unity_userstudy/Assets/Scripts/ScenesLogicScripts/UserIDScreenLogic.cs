using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserIDScreenLogic : MonoBehaviour
{
    public UnityEngine.UI.Text userIDField;
    public UnityEngine.UI.Text sessionIDField;

    public string stimuliListDirectory = "";
    public string stimuliListPrefix = "";

    public int numberOfParticipants = 30;
    public int numberOfSessions = 3;

    public Vector2Int participandIDBounds = new Vector2Int(1,30);

    private GameObject notification = null;

    // Start is called before the first frame update
    void Start()
    {
        SessionData.StimuliListDirectory = stimuliListDirectory;
        SessionData.StimuliListPrefix = stimuliListPrefix;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
            StaticMethods.QuitPrompt();
    }


    /// <summary>
    /// Checks whether data provided by the participant (Participant and Session ID) has correct format.
    /// If all requirements are fullfilled the scene is switched to the next one declared in the Build Settings (Unity).
    /// </summary>
    public void ContinueButtonClick()
    {
        string wrongPIDmessage = "Wrong PARTICIPANT ID format.\nProvide ID from range: " + participandIDBounds.x.ToString("D3") + " - " + participandIDBounds.y.ToString("D3") + ".";
        string wrongSIDmessage = "Wrong SESSION ID format.\nProvide ID from range: 1 - " + numberOfSessions.ToString() + ".";

        //////User ID chunk
        ///

        if(userIDField.text.Length != 3)
        {
            NotifyCustomMessage(message: wrongPIDmessage);
            return;
        }

        if (Int32.TryParse(userIDField.text, out int resp))
        {
            if(resp < participandIDBounds.x || resp > participandIDBounds.y)
            {
                NotifyCustomMessage(message: wrongPIDmessage);
                return;
            }
            else
                SessionData.UserID = resp.ToString("D3");
        }
        else
        {
            NotifyCustomMessage(message: wrongPIDmessage);
            return;
        }

        //////Session ID chunk
        if (Int32.TryParse(sessionIDField.text, out int ress))
        {
            if (ress < 1 || ress > numberOfSessions)
            {
                NotifyCustomMessage(wrongSIDmessage);
                return;
            }
            else
                SessionData.SessionID = ress.ToString();
        }
        else
        {
            NotifyCustomMessage(wrongSIDmessage);
            return;
        }


        //////When all criterions are fulfilled continue
        ///
        SetTrialsFilepth();

        ASAP_Matlab_Init();
        // Initiate ASAP via run matlab function in cmd, wait 5-10 sec for loading matlab
        // suppose only 1 session and not resume
        // StartCoroutine(WaitMatlab());

        StaticMethods.NextScene();

    }

    private void SetTrialsFilepth()
    {
        // int fileID = (userID - 1) * numberOfSessions + sessionId;

        SessionData.StimuliListFilename = stimuliListPrefix + SessionData.UserID + '_' + SessionData.SessionID;
        // SessionData.StimuliListFilename = stimuliListPrefix + fileID.ToString();
        Debug.Log(SessionData.StimuliListFilename);
    }


    /// <summary>
    /// Displays the notification prepared beforehand as a prefab.
    /// </summary>
    /// <param name="prefabPath"></param>
    private void Notify(string prefabPath)
    {
        if (notification != null)
            GameObject.Destroy(notification);
        notification = GameObject.Instantiate(Resources.Load(prefabPath) as GameObject);
    }


    /// <summary>
    /// Displays prompt with a message passed as an argument.
    /// </summary>
    /// <param name="message"></param>
    private void NotifyCustomMessage(string message)
    {
        if (notification != null)
            GameObject.Destroy(notification);
        notification = GameObject.Instantiate(Resources.Load(PrefabsPaths.customPrompt) as GameObject);
        notification.GetComponent<CustomPrompt>().ChangeText(message);
    }

        /// <summary>
    /// run matlab asap code, input userID, sessionID, the code will generate/update stimuli file and
    /// track the change of trails file (results)
    /// </summary>
    private void ASAP_Matlab_Init()
    {
        // workdir and filepath related params can be set as public variables which can be set in untiy GUI
        string userID = SessionData.UserID;
        string session = SessionData.SessionID;


        //Get the path of the Game data folder
        var m_Path = Application.streamingAssetsPath;

        //Output the Game data path to the console
        string strCmdText = String.Format("-nosplash -nodesktop -minimize -r \"ASAP_Head_Demo6(\'{0}\',\'{1}\'); exit\" ",
                userID, session);

        RunCmd("matlab", strCmdText, m_Path + "/matlab_scripts");
        Debug.Log("Run: matlab " + strCmdText);
    }

    IEnumerator WaitMatlab(float seconds=10)
    {
        //Print the time of when the function is first called.
        // Debug.Log("Started Coroutine at timestamp : " + Time.time);

        //yield on a new YieldInstruction that waits for 20 seconds.
        yield return new WaitForSecondsRealtime(seconds);

        //After we have waited 5 seconds print the time again.
        // Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }

    private static System.Diagnostics.Process CreateCmdProcess(string cmd, string args, string workingDir = "")
    {
        var en = System.Text.UTF8Encoding.UTF8;
        if (Application.platform == RuntimePlatform.WindowsEditor)
            en = System.Text.Encoding.GetEncoding("gb2312");

        var pStartInfo = new System.Diagnostics.ProcessStartInfo(cmd);
        pStartInfo.Arguments = args;
        pStartInfo.CreateNoWindow = false;
        pStartInfo.UseShellExecute = false;
        pStartInfo.RedirectStandardError = true;
        pStartInfo.RedirectStandardInput = true;
        pStartInfo.RedirectStandardOutput = true;
        pStartInfo.StandardErrorEncoding = en;
        pStartInfo.StandardOutputEncoding = en;
        if (!string.IsNullOrEmpty(workingDir))
            pStartInfo.WorkingDirectory = workingDir;
        return System.Diagnostics.Process.Start(pStartInfo);
    }

    public static string RunCmdNoErr(string cmd, string args, string workingDri = "")
    {
        var p = CreateCmdProcess(cmd, args, workingDri);
        var res = p.StandardOutput.ReadToEnd();
        p.Close();
        return res;
    }

    public static string RunCmdNoErr(string cmd, string args, string[] input, string workingDri = "")
    {
        var p = CreateCmdProcess(cmd, args, workingDri);
        if (input != null && input.Length > 0)
        {
            for (int i = 0; i < input.Length; i++)
                p.StandardInput.WriteLine(input[i]);
        }
        var res = p.StandardOutput.ReadToEnd();
        p.Close();
        return res;
    }

    public static string[] RunCmd(string cmd, string args, string workingDir = "")
    {
        string[] res = new string[2];
        var p = CreateCmdProcess(cmd, args, workingDir);
        res[0] = p.StandardOutput.ReadToEnd();
        res[1] = p.StandardError.ReadToEnd();
        // #if !UNITY_IOS
        // res[2] = p.ExitCode.ToString();
        // #endif
        p.Close();
        return res;
    }

    public static void OpenFolderInExplorer(string absPath)
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
            RunCmdNoErr("explorer.exe", absPath);
        else if (Application.platform == RuntimePlatform.OSXEditor)
            RunCmdNoErr("open", absPath.Replace("\\", "/"));
    }
}
