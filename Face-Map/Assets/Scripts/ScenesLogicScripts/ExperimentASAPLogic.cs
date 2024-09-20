using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using UnityEngine.Networking;

using UnityEditor;
using System.Threading;
using System.Text;

public class ExperimentASAPLogic : MonoBehaviour
{

    [Tooltip("File containing description of all trials.")]
    public string datasetFile;
    // public TextAsset datasetFile;
    [Tooltip("If true, ignores the file specified in Dataset File field and loads dataset file based on participant and session IDs.")]
    public bool loadTrialsBasedOnPID;

    [Tooltip("Camera prefab that will be used for rendering of the stimuli.")]
    public GameObject cameraPrefab;
    [Tooltip("(optional) If set, the original material of the object will be replaced with the specified one.")]
    public Material overrideObjectMaterial;

    [Tooltip("Specifies the experimental procedure, which decides on how the stimuli is presented to the participant and the way of answering.")]
    public ExperimentLayout experimentLayout;
    [Tooltip("When enabled no results will be saved. Additionally, feedback about the correctness of the answer will be displayed for some experimental layouts.")]
    public bool trainingMode;
    [Tooltip("When enabled, all trials will be shuffled and displayed to participants in random order.")]
    public bool shuffleTrials;
    [Tooltip("When enabled, normalizes and recenters the meshes. The Maximum size of the object is equal to 1 unit.")]
    public bool normalizeMeshSize;
    [Tooltip("Allows setting initial rotation for XYZ axes globally for all objects.")]
    public Vector3 intialMeshRotation;

    [Tooltip("Enables rotation of the objects.")]
    public bool rotationAllowed;
    [Tooltip("Enables zooming of the objects.")]
    public bool zoomAllowed;
    [Tooltip("Enables translation of the objects.")]
    public bool translationAllowed;
    [Tooltip("Auto Rotation Enabled - Enables auto rotation of the objects. Objects start to rotate automatically when no manual interaction is detected.")]
    public bool autoRotationEnabled;
    [Tooltip("Allows to rotate objects around only Y axis or all axes.")]
    public RotationScheme rotationScheme;

    [Tooltip("Specifies minimum time participant needs to spend in a trial, before he is allowed to give an answer.")]
    public float minimumTimePerTrial = 5;
    [Tooltip("Sets initial timer value.")]
    public float softCapTrialDuration = 30;
    [Tooltip("Specifies how long the blank screen is displayed between the trials.")]
    public float blankScreenTime = 2;

    [Tooltip("Name of the directory in DropBox App location where result files will be uploaded. If not specified, all results will be saved in a directory named Default.")]
    public string dropboxResultsDirectory;
    [Tooltip("Name of the directory in DropBox App location where feedback files will be uploaded. If not specified, feedback files will be saved in a directory named Feedback_Default.")]
    public string dropboxFeedbackDirectory;

    [Tooltip("if the Experiment Layout is set to 2AFCFlickerNoReference, then this parameter will determine how long the blank screen will be displayed when the participant changes between meshes.")]
    public float blankScreenTimeForFlicker;

    private Dataset dataset;
    private TrialGeneratorASAP trialGenerator;


    // Start is called before the first frame update
    void Start()
    {
        // ASAP_Matlab_Init();
        // // Initiate ASAP via run matlab function in cmd, wait 5-10 sec for loading matlab
        // // suppose only 1 session and not resume
        // StartCoroutine(WaitMatlab());
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        trialGenerator.Update();

        if (Input.GetKeyDown(KeyCode.Escape))
            StaticMethods.QuitPrompt();
        if (Input.GetKeyDown(KeyCode.Q))
            StaticMethods.QuitFinalPrompt();
    }

    /// <summary>
    /// Main function that initiates instance of the experiment.
    /// Method sets the output filenames reads the dataset and creates trial generator.
    /// </summary>
    private void Init()
    {
        Debug.Log("[FM][INFO] ExperimentASAP Init!");
        // SetDropBoxDirectories();

        string stimuli_file = SessionData.StimuliListDirectory+SessionData.StimuliListFilename + ".json";
        Debug.Log("[FM][INFO] Stimuli Path is "+ stimuli_file);
        // string sem = stimuli_file + "_sem.bak";
        // Debug.Log("[FM][INFO] Semaphore is "+sem);

        while(true)
        {
            Thread.Sleep(80);
            if(File.Exists(stimuli_file))
            {
                Thread.Sleep(80);
                break;
            }
        }

        if (loadTrialsBasedOnPID)
            datasetFile = SessionData.GetASAPStimuliListFile();
        Debug.Log("[FM] [INFO] loadTrialsBasedOnPID " + loadTrialsBasedOnPID);
        Debug.Log("[FM] [INFO] datasetFile " + datasetFile);

        SetSubsetName();
        SetResultFilename();
        CheckForResume();
        ReadDataset();

        CreateAndInitializeTrialGenerator();
    }


    /// <summary>
    /// Checks whether given session was started by participant and sets the resume flag accordingly.
    /// If Resume flag is set to true, the results will be read and only unfinished trials will be dispalyed to participant.
    /// </summary>
    private void CheckForResume()
    {
        if (File.Exists(SessionData.GetResultFilepath()))
        {
            SessionData.Resume = true;
            Debug.Log("Experiment will be continued");
        }
        else
        {
            SessionData.Resume = false;
            Debug.Log("Experiment will start from scratch");
        }
    }

    private void SetResultFilename()
    {
        Debug.Log("Subsetname: " + SessionData.Subsetname);

        Hash128 hash = new Hash128();
        string timeNow = System.DateTime.Now.Ticks.ToString();
        hash.Append(timeNow);
        if (SessionData.UserID.Length == 0)
            SessionData.UserID = hash.ToString();
        Debug.Log("User ID:" + SessionData.UserID);

        SessionData.ResultFilename = SessionData.UserID + '_' + SessionData.SessionID + '_' + SessionData.Subsetname;
        Debug.Log("ResultFilename form prefs: " + SessionData.ResultFilename);
    }

    /// <summary>
    /// Stores stimuli list filename. It is appended to the result file as a suffix when files are saved.
    /// </summary>
    protected void SetSubsetName()
    {
        SessionData.Subsetname = SessionData.StimuliListFilename;
        Debug.Log("SessionData.Subsetname: " + SessionData.Subsetname);
    }

    /// <summary>
    /// Creates trial generator based on the choice from the drop-down list in the inspector and initializes it.
    /// Modify this method to add new experiment layouts.
    /// </summary>
    private void CreateAndInitializeTrialGenerator()
    {
        if (dataset == null)
            Debug.LogError("Dataset needs to be read before creating TrialGenerator");

        trialGenerator = new TG_2AFC_WitReference_ASAP(dataset);

        SetTrialGeneratorParameters();
        trialGenerator.Initialize();
    }


    /// <summary>
    /// Passes all options from ExperimentLogic interface to TrialGenerator instance.
    /// </summary>
    private void SetTrialGeneratorParameters()
    {
        trialGenerator.trainingMode = trainingMode;

        trialGenerator.autoRotationEnabled = autoRotationEnabled;
        trialGenerator.rotationAllowed = rotationAllowed;
        trialGenerator.zoomAllowed = zoomAllowed;
        trialGenerator.translationAllowed = translationAllowed;
        trialGenerator.rotationScheme = rotationScheme;

        trialGenerator.Shuffle = shuffleTrials; // set to no because of ASAP
        trialGenerator.NormalizeMeshesHeight = normalizeMeshSize;
        trialGenerator.IntialMeshRotation = intialMeshRotation;
        trialGenerator.minimumTrialDuration = minimumTimePerTrial;
        trialGenerator.blankScreenTime = blankScreenTime;
        trialGenerator.softCapTrialDuration = softCapTrialDuration;
        trialGenerator.CameraPrefab = cameraPrefab;

        // trialGenerator.SetOverrideMaterial(overrideObjectMaterial);
        trialGenerator.blankScreenTimeForFlicker = blankScreenTimeForFlicker;
    }

    /// <summary>
    /// Parses dataset passed to the datasetFile field and stores it in the instance of Dataset class.
    /// </summary>
    private void ReadDataset()
    {
        datasetFile = SessionData.GetASAPStimuliListFile();
        dataset = JsonUtility.FromJson<Dataset>(datasetFile);
        Debug.Log("Dataset continue loaded. Number of stimuli with duplicated setting: " + dataset.stimuli.Count);
        dataset.stimuli.RemoveAt(0);
        Debug.Log("Dataset continue loaded. Number of stimuli: " + dataset.stimuli.Count);
        Debug.Log("Number of comparisons in this session: " + dataset.stimuli[0].batch);
    }

    /// <summary>
    /// Prints in console all file pairs reference <-----> distorted.
    /// </summary>
    private void DebugDataset()
    {
        foreach (Stimulus stimulus in dataset.stimuli)
        {
            Debug.Log("Reference: " + stimulus.reference + "   <----->   Distorted: " + stimulus.distortionA);
        }
    }

    /// <summary>
    /// Creates set of cameras required for chosen trial generator and assignes layers to them.
    /// Layer system is used to cull selected objects from the camera view.
    /// Created cameras split view in the horizontal axis.
    /// </summary>
    private void CreateCameras()
    {
        if (trialGenerator == null)
            Debug.LogError("TrialGenerator needed to establish the camera layout");

        GameObject cameras = new GameObject("Cameras");

        int rows = trialGenerator.CameraLayout.x;
        int cols = trialGenerator.CameraLayout.y;

        int counter = 1;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                string layerName = "m" + counter.ToString();
                GameObject camera = GameObject.Instantiate(cameraPrefab);
                Camera cameraScript = camera.GetComponent<Camera>();
                cameraScript.cullingMask = 1 << LayerMask.NameToLayer(layerName);
                cameraScript.rect = new Rect((float)j / cols, (float)i / rows, 1f / cols, 1f / rows);

                camera.transform.SetParent(cameras.transform, true);

                counter++;
            }
        }
    }

    /// <summary>
    /// Updates the answer and timing from beginning of the trial for active trial generator
    /// </summary>
    public void UpdateCurrentAnswerAndTiming(float answer)
    {
        trialGenerator.UpdateCurrentAnswerAndTiming(answer);
    }

}
