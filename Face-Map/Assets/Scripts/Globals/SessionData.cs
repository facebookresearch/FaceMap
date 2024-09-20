using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using UnityEditor;

public class SessionData
{
    // Dropbox access token                     PLEASE MODIFY IT BEFORE RUNNING THE STUDY
    static private string dropboxAccessToken = "";

    // Session parameters
    static private string userID = "";
    static private string sessionID = "";

    // Stimuli files variables
    static private string stimuliListDirectory = "";
    static private string stimuliListPrefix = "";
    static private string stimuliListFilename = "";
    static private string stimuliListExtension = ".json";

    // DropBox directories
    private static string resultDBDirectoryPath = "Default";
    private static string feedbackDBDirectoryPath = "Feedback_Default";

    // Result filename variables
    private static string resultsDirectory = "ASAPStimuliLists/results";
    static private string subsetname = "";
    static private string resultFilename = "";
    static private string resultFileExtension = ".json";
    static private string feebackFileExtension = ".txt";
    static private string feebackFilePrefix= "feedback_";

    // Additional global experiment modifiers
    static private bool resume = false;
    static private bool lockedUserInput = false;

    public static string UserID { get => userID; set => userID = value; }
    public static string ResultFilename { get => resultFilename; set => resultFilename = value; }
    public static string Subsetname { get => subsetname; set => subsetname = value; }
    public static bool Resume { get => resume; set => resume = value; }
    public static bool LockedUserInput { get => lockedUserInput; set => lockedUserInput = value; }
    public static string SessionID { get => sessionID; set => sessionID = value; }
    public static string StimuliListPrefix { get => stimuliListPrefix; set => stimuliListPrefix = value; }
    public static string StimuliListFilename { get => stimuliListFilename; set => stimuliListFilename = value; }
    public static string StimuliListDirectory { get => stimuliListDirectory; set => stimuliListDirectory = value; }
    public static string ResultsDirectory { get => resultsDirectory; set => resultsDirectory = value; }
    public static string ResultDBDirectoryPath { get => resultDBDirectoryPath; set => resultDBDirectoryPath = value; }
    public static string FeedbackDBDirectoryPath { get => feedbackDBDirectoryPath; set => feedbackDBDirectoryPath = value; }
    public static string DropboxAccessToken { get => dropboxAccessToken; set => dropboxAccessToken = value; }


    /// <summary>
    /// Generates filepath pointing at the result file on local drive
    /// </summary>
    public static string GetResultFilepath()
    {
        return Path.Combine(SessionData.ResultsDirectory, resultFilename + resultFileExtension);
    }

    /// <summary>
    /// Generates filepath pointing at the feedback file on local drive
    /// </summary>
    public static string GetFeedbackFilepath()
    {
        return Path.Combine(SessionData.ResultsDirectory, feebackFilePrefix + resultFilename + feebackFileExtension);
    }

    /// <summary>
    /// Loads stimuli list file specified in ExperipentLogic inspector view
    /// </summary>
    public static TextAsset GetStimuliListFile()
    {

        Thread.Sleep(100);

        string trialsFilepath = stimuliListDirectory + stimuliListFilename;
        Debug.Log(trialsFilepath);
        TextAsset textAsset = Resources.Load<TextAsset>(trialsFilepath);
        return textAsset;
    }


    /// <summary>
    /// Loads stimuli list file specified in ExperipentLogic inspector view, out of Resources folder
    /// </summary>
    // public static TextAsset GetAssetStimuliListFile()
    // {

    //     Thread.Sleep(100);

    //     string trialsFilepath = stimuliListDirectory + stimuliListFilename;
    //     Debug.Log(trialsFilepath);
    //     TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(trialsFilepath);
    //     return textAsset;
    // }

    /// <summary>
    /// Loads stimuli list file specified in ExperipentLogic inspector view, out of Asset folder
    /// </summary>
    public static string GetASAPStimuliListFile()
    {
        string fileContent = "";
        while (true) {
            try
            {
                Thread.Sleep(100);
                string filepath = stimuliListDirectory + stimuliListFilename + stimuliListExtension;
                Debug.Log("ASAP stimuli list file" + filepath);
                fileContent = System.IO.File.ReadAllText(filepath);
                return fileContent;
            } catch(IOException) {
                Debug.Log("Keep Trying");
            };
        }
    }


}
