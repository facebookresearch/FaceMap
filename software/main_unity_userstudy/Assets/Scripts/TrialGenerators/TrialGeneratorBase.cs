using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TrialGeneratorBase
{
    // Mode modifiers
    public bool trainingMode;

    // object manipulation
    public bool rotationAllowed;
    public bool autoRotationEnabled = true;
    public bool zoomAllowed = true;
    public bool translationAllowed = true;
    public RotationScheme rotationScheme;

    protected Dataset dataset;
    protected List<Stimulus> stimuli;
    protected List<Trial> trials;
    protected List<int> trialIndices;
    protected Vector2Int cameraLayout;
    protected string setting = "default";
    protected string labelsPrefabPath = "";
    private Material overrrideMaterial;

    protected int currentTrialNumber = 0;
    protected int currentTrialIndex = 0;
    protected List<GameObject> currentTrialObjects;
    protected int currentRating = 5;
    protected float currentTrialStartingTime;

    public float minimumTrialDuration;
    public float softCapTrialDuration;
    public float blankScreenTime;

    public float blankScreenTimeForFlicker;

    protected AnsweringMethod answeringMethod;

    // UI objects
    protected GameObject instructionsObject;
    protected GameObject notification = null;
    protected GameObject timer;
    protected GameObject labelsCanvas;
    protected GameObject progressCanvas;
    public bool rotationStartAlternate = false;

    // helpers
    protected bool inputLocked = true;
    private bool shuffle = true;
    private bool normalizeMeshesHeight = true;
    private Vector3 initialMeshRotation;

    string filepath = "";

    protected int trialOffset = 0;
    protected int nAllTrials = 0;

    private GameObject cameraPrefab;

    // Containers
    protected GameObject camerasContainer;
    protected GameObject stimuliContainer;
    protected GameObject uiElementsContainer;


    /// <summary>
    /// Trial generator constructor.
    /// </summary>
    /// <param name="datatset"></param>
    public TrialGeneratorBase(Dataset datatset)
    {
        dataset = datatset;
        stimuli = new List<Stimulus>(datatset.stimuli);

        cameraLayout = new Vector2Int(1, 1);
        currentTrialObjects = new List<GameObject>();
    }



    public void Initialize()
    {
        PopulateTrials();

        InitializeEmptyContainers();
        CreateCameras();
        CreateUIElements();

        //Very first trial is started here.
        GenerateTrial();
    }

    /// <summary>
    /// Creates all UI Elements: isntructions, labels, progress info and timer.
    /// </summary>
    private void CreateUIElements()
    {
        CreateInstructions();
        CreateLabels();
        CreateProgressCanvas();
        CreateTimer();
    }


    /// <summary>
    /// Method is called at every frame.
    /// </summary>
    public virtual void Update()
    {
        UpdateTimer();

        if(!SessionData.LockedUserInput)
        {
            if (HasAnswerKeyBeenPressed() && GetTrialDuration() <= minimumTrialDuration)
                StaticMethods.Notify(PrefabsPaths.tooFastPrompt, ref notification);
            else
                KeyboardInput();
        }
    }

    private bool HasAnswerKeyBeenPressed()
    {
        bool flag = false;

        switch (answeringMethod)
        {
            case AnsweringMethod.Rating:
                if (Input.GetKeyDown(KeyCode.Space))
                    flag = true;
                break;
            case AnsweringMethod.ChooseOne:
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
                    flag = true;
                break;
            case AnsweringMethod.FlickerChooseOne:
                if (Input.GetKeyDown(KeyCode.Space))
                    flag = true;
                break;
            case AnsweringMethod.NoAnswer:
                break;
            default:
                break;
        }

        return flag;
    }

    /// <summary>
    /// Handles keyboard input. Does not include objects manipulation.
    /// </summary>
    public virtual void KeyboardInput()
    {

    }

    /// <summary>
    /// Switches current scene to the next one declared in Build Settings (Unity).
    /// If there are no more scenes in the Build Setting the app is closed.
    /// </summary>
    protected void NextScene()
    {
        StaticMethods.NextScene();
    }


    /// <summary>
    /// Reads or creates trials from scratch. Shuffles the trials if such option was chosen in the inspector view of ExperimentLogic class.
    /// </summary>
    protected void PopulateTrials()
    {
        if (SessionData.Resume)
            ContinueTrialsSavedToFile();
        else
            CreateTrialsFromScratch();

        if (shuffle)
            trialIndices.Shuffle();
    }

    /// <summary>
    /// Creates trial list used in the experiment from scratch.
    /// </summary>
    private void CreateTrialsFromScratch()
    {
        trials = new List<Trial>();

        foreach (Stimulus stimulus in dataset.stimuli)
        {
            Trial trial = new Trial();
            trial.userID = SessionData.UserID;
            trial.rating = -1;
            trial.stimulus = stimulus;
            trials.Add(trial);
        }

        trialIndices = new List<int>();
        trialIndices.AddRange(System.Linq.Enumerable.Range(0, trials.Count));

        nAllTrials = dataset.stimuli.Count;
    }

    /// <summary>
    /// Read previously saved trial list. Method is used to continue previously started session.
    /// </summary>
    private void ContinueTrialsSavedToFile()
    {
        nAllTrials = dataset.stimuli.Count;

        string resultsFileContent = System.IO.File.ReadAllText(SessionData.GetResultFilepath());
        TrialCollection trialCollection = JsonUtility.FromJson<TrialCollection>(resultsFileContent);
        trials = trialCollection.trials;
        Debug.Log("TrialCollection loaded. Number of stimuli: " + trials.Count);

        trialIndices = new List<int>();

        foreach (Trial trial in trials)
        {
            if(trial.rating == -1)
            {
                trialIndices.Add(trials.IndexOf(trial));
            }
        }

        trialOffset = nAllTrials - trialIndices.Count;

        Debug.Log("Number of trials to continue: " + trialIndices.Count);
        if (trialIndices.Count == 0)
            StaticMethods.NextScene();
    }

    /// <summary>
    /// Saves results to JSON file
    /// </summary>
    protected virtual void SaveResults()
    {
        // No files are saved in training mode
        if (trainingMode)
            return;

        Debug.Log("SavingResults");

        if(trials[currentTrialIndex].rating == -1)
            UpdateCurrentAnswerAndTiming(currentRating);

        string resultsDirectory = SessionData.ResultsDirectory;  //TODO replace string

        System.IO.Directory.CreateDirectory(resultsDirectory);
        TrialCollection trialsCollection = new TrialCollection();
        trialsCollection.trials = trials;

        string json = JsonUtility.ToJson(trialsCollection, true);
        System.IO.File.WriteAllText(SessionData.GetResultFilepath(), json);

    }


    /// <summary>
    /// Starts next trial from trial list if it is available and returns true. If no more trials available returns false.
    /// If iterate is set to true, iterates trial list by a number specified in iteartor parameter.
    /// </summary>
    public virtual bool NextTrial(bool iterate=true, int iterator = 1)
    {
        bool isNextTrialAvailable;

        rotationStartAlternate = !rotationStartAlternate;
        Debug.Log("Switching Rotation Alternate: "+  rotationStartAlternate);

        
        if(iterate)
        {
            SaveResults();
            //Debug.Log("Iterating and destryoing objects");
            currentTrialNumber = currentTrialNumber + iterator;
            DestroyAllCurrentGameObjects();
        }

        if (currentTrialNumber < trialIndices.Count)
        {
            isNextTrialAvailable = true;
            GenerateTrial();
        }
        else
            isNextTrialAvailable = false;

        return isNextTrialAvailable;
    }

    /// <summary>
    /// Geneartes new trial and displays blank screen. Stores all generated meshes in a container.
    /// </summary>
    public virtual void GenerateTrial()
    {
        // Blank screen at the beginnign of every trial
        DisplayBlankScreen();
        // Update counter at the top of the screen
        UpdateProgressInfo();
        

        currentTrialIndex = trialIndices[currentTrialNumber];
        currentTrialStartingTime = Time.time + blankScreenTime;

        Stimulus stimulus = trials[currentTrialIndex].stimulus;

        Debug.Log("Creating new trial with setting: " + setting);
        Debug.Log("Entered creation process. Mesh A: " + stimulus.distortionA + " Mesh B: " + stimulus.distortionB);

        GenerateAndLayoutMeshes(stimulus);


        foreach (GameObject gameObject in currentTrialObjects)
        {
            AddObjectToContainer(stimuliContainer, gameObject);
        }
    }


    /// <summary>
    /// Instantiates and layouts the meshes according to the settings set in inherited classes.
    /// </summary>
    protected virtual void GenerateAndLayoutMeshes(Stimulus stimulus)
    {

    }


    /// <summary>
    /// Creates Timer UI element.
    /// </summary>
    public void CreateTimer()
    {
        Debug.Log("Creating timer");
        timer = GameObject.Instantiate(Resources.Load(PrefabsPaths.timerCanvas) as GameObject);
        TimerBehaviour timerBehaviour = timer.GetComponent<TimerBehaviour>();
        timerBehaviour.blankScreenTime = blankScreenTime;
        timerBehaviour.trialMinTimeSpent = minimumTrialDuration;
        timerBehaviour.trialSoftCap = softCapTrialDuration;

        AddObjectToContainer(uiElementsContainer, timer);
    }

    /// <summary>
    /// Create cameras with layout specified in inherited classes.
    /// </summary>
    private void CreateCameras()
    {
        int rows = CameraLayout.x;
        int cols = CameraLayout.y;

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

                AddObjectToContainer(camerasContainer, camera);

                counter++;
            }
        }
    }

    /// <summary>
    /// Updates value of the timer.
    /// </summary>
    private void UpdateTimer()
    {
        timer.GetComponent<TimerBehaviour>().UpdateTimer(GetTrialDuration());
    }

    /// <summary>
    /// Generates blanck screen which disappears after given time.
    /// </summary>
    protected void DisplayBlankScreen()
    {
        GameObject canvas = GameObject.Instantiate(Resources.Load(PrefabsPaths.blankScreenCanvas) as GameObject);
        canvas.GetComponent<DestroyAfterTimePeriod>().time = blankScreenTime;
    }

    /// <summary>
    /// Updates iformation on how many trials have been completed.
    /// </summary>
    public virtual void UpdateProgressInfo()
    {
        GameObject progresObject = GameObject.Find("Progress");
        UpdateTrialNumberInformation infoScript = progresObject.GetComponent<UpdateTrialNumberInformation>();
        infoScript.UpdateInfo(GetNumberOfTrials());
    }


    /// <summary>
    /// Load mesh from resources. Request is based on a given stimulus and model version.
    /// </summary>
    protected GameObject LoadMesh(Stimulus stimulus, ModelVersion modelVersion)
    {
        string path = stimulus.directory;
        string file = "";

        switch (modelVersion)
        {
            case ModelVersion.Reference:
                file = stimulus.reference;
                break;
            case ModelVersion.DistortedA:
                file = stimulus.distortionA;
                break;
            case ModelVersion.DistortedB:
                file = stimulus.distortionB;
                break;
            default:
                Debug.LogError("Model version not defined");
                break;
        }

        Debug.Log(file);

        path = System.IO.Path.Combine(path, file);
        path = System.IO.Path.ChangeExtension(path, null); // strip extension for Resources.Load

        Debug.Log("Loading object: " + path);
        GameObject gameObject = Resources.Load<GameObject>(path);
        return gameObject;
    }

    /// <summary>
    /// Instantiates in the scene a mesh laoded from resources and passes all required parameters for object interaction script.
    /// Normalizes and changes intiial rotation of the mesh if these options were specified in inspector view of Experiment Logic script.
    /// </summary>
    protected Transform InstantiateLoadedResource(Stimulus stimulus, GameObject gameObject, string wrapperName, string layer, Transform transform = null)
    {
        //instantiate object
        GameObject instance = GameObject.Instantiate(gameObject);

        //compute bounds of all object components together
        Renderer[] rends = instance.GetComponentsInChildren<Renderer>();
        Vector3 min = new Vector3();
        Vector3 max = new Vector3();
        foreach (Renderer renderer in rends)
        {
            max = Vector3.Max(max, renderer.bounds.max);
            min = Vector3.Min(min, renderer.bounds.min);
        }
        Vector3 size = max-min;
        Vector3 offset = (max + min) / 2;

        //override material
        if(overrrideMaterial != null)
        {
            Debug.Log("Material Overriding ");
            foreach (Renderer renderer in rends)
            {
                renderer.material = overrrideMaterial;
            }
        }

        //transform object
        if (transform == null)
        {
            //scale according to the trial info
            if (stimulus.init_scale == 0)
                stimulus.init_scale = 1;
            instance.transform.localScale *= stimulus.init_scale;

            //rotate accorinf to initial rotation from ExperimentLogic inspector view
            instance.transform.Rotate(initialMeshRotation);

            //rotate according to the trial info
            instance.transform.Rotate(Vector3.forward, stimulus.init_rotation_z, Space.World); //optional rotation for upside down faces

            //normalize mesh and recenter it if normalization enabled
            if (normalizeMeshesHeight)
            {
                float maxSize = Mathf.Max(Mathf.Max(size.x, size.y, size.z));
                float normalizationScale = 1.0f / maxSize;
                instance.transform.localScale *= normalizationScale; // normalize the size
                instance.transform.Translate(-offset * normalizationScale * stimulus.init_scale); // remove the offset
            }
        }
        else
        {
            instance.transform.position = transform.position;
            instance.transform.localScale = transform.localScale;
            instance.transform.rotation = transform.rotation;
        }

        GameObject wrapper = new GameObject(wrapperName);
        instance.transform.SetParent(wrapper.transform, true);

        ObjectInteraction objectInteraction = wrapper.AddComponent<ObjectInteraction>();
        objectInteraction.rotateLeft = rotationStartAlternate;
        objectInteraction.totalRotateY = initialMeshRotation.y;
        objectInteraction.zoomAllowed = zoomAllowed;
        objectInteraction.translationAllowed = translationAllowed;
        objectInteraction.autoRatioationEnabled = autoRotationEnabled;
        objectInteraction.rotationAllowed = rotationAllowed;
        objectInteraction.onlyUPAxisRotation = Convert.ToBoolean((int) rotationScheme);
        ChangeLayerHierarchy(wrapper, layer);

        return instance.transform;
    }


    /// <summary>
    /// Changes layer for a hierarchy of objects to a given layerName.
    /// Layer system is used to layout meshes. They are all spawned in the same place, but layer system allows to hide some objects form certain cameras.
    /// </summary>
    void ChangeLayerHierarchy(GameObject gameObject, string layerName)
    {
        gameObject.layer = LayerMask.NameToLayer(layerName);
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            Transform child = gameObject.transform.GetChild(i);
            ChangeLayerHierarchy(child.gameObject, layerName);
        }
    }


    /// <summary>
    /// Destroys all objects fro current trial.
    /// </summary>
    public void DestroyAllCurrentGameObjects()
    {
        foreach (GameObject gameObject in currentTrialObjects)
        {
            GameObject.Destroy(gameObject);
        }
        currentTrialObjects.Clear();
    }

    /// <summary>
    /// Sets override material which is sset later on to instantiated objects.
    /// </summary>
    public void SetOverrideMaterial(Material overrideMaterial)
    {
        overrrideMaterial = overrideMaterial;
    }

    /// <summary>
    /// Updates answers and timing for current trial.
    /// </summary>
    public virtual void UpdateCurrentAnswerAndTiming(float answer)
    {
        currentRating = (int)answer;
        trials[currentTrialIndex].rating = currentRating;
        UpdateCurrentTime();
    }

    /// <summary>
    /// Updates timing for current trial.
    /// </summary>
    public virtual void UpdateCurrentTime()
    {
        trials[currentTrialIndex].time = Time.time - currentTrialStartingTime;
    }

    /// <summary>
    /// Returns 2d Vector containing number of current trial and number of all trials.
    /// </summary>
    public Vector2Int GetNumberOfTrials()
    {
        return new Vector2Int(currentTrialNumber + 1 + trialOffset, nAllTrials);
    }

    /// <summary>
    /// Creates labels for displayed meshes
    /// </summary>
    protected void CreateLabels()
    {
        if (labelsCanvas != null)
            GameObject.Destroy(labelsCanvas);
        labelsCanvas = GameObject.Instantiate(Resources.Load(labelsPrefabPath) as GameObject);
        AddObjectToContainer(uiElementsContainer, labelsCanvas);
    }

    /// <summary>
    /// Creates UI elements do display progress info
    /// </summary>
    protected void CreateProgressCanvas()
    {
        if (progressCanvas != null)
            GameObject.Destroy(progressCanvas);
        progressCanvas = GameObject.Instantiate(Resources.Load(PrefabsPaths.progressCanvas) as GameObject);
        AddObjectToContainer(uiElementsContainer, progressCanvas);
    }

    /// <summary>
    /// Creates interaction guidelines.
    /// Modifis them according to what interactions are allowed in inspector view of Experiment Logic.
    /// </summary>
    public virtual void CreateInstructions()
    {
        UnityEngine.UI.Text textObject = instructionsObject.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>();
        if (rotationAllowed)
            textObject.text = "[Right Mouse Button] - rotate\n" + textObject.text;
        if (zoomAllowed)
            textObject.text = "[Scroll Wheel] - zoom\n" + textObject.text;
        if (translationAllowed)
            textObject.text = "[Middle Mouse Button] - move\n" + textObject.text;

        AddObjectToContainer(uiElementsContainer, instructionsObject);
    }

    /// <summary>
    /// Returns current duration of a trial.
    /// </summary>
    protected float GetTrialDuration()
    {
        float duration = Time.time - currentTrialStartingTime;
        return duration;
    }

    /// <summary>
    /// Destorys notification if any is currently displayed.
    /// </summary>
    protected void DestoryNotifications()
    {
        if (notification != null)
            GameObject.Destroy(notification);
    }

    /// <summary>
    /// Creates empty containers used to store different elements.
    /// </summary>
    private void InitializeEmptyContainers()
    {
        camerasContainer = new GameObject("Cameras");
        stimuliContainer = new GameObject("Stimuli");
        uiElementsContainer = new GameObject("UI Elements");
    }

    /// <summary>
    /// Adds object to given container.
    /// </summary>
    protected void AddObjectToContainer(GameObject container, GameObject element)
    {
        element.transform.SetParent(container.transform, true);
    }

    public Vector2Int CameraLayout { get => cameraLayout; set => cameraLayout = value; }
    protected Material OverrrideMaterial { get => overrrideMaterial; set => overrrideMaterial = value; }
    public AnsweringMethod AnsweringMethod { get => answeringMethod; }
    public virtual bool Shuffle { get => shuffle; set => shuffle = value; }
    public string Filepath { get => filepath; set => filepath = value; }
    public GameObject CameraPrefab { get => cameraPrefab; set => cameraPrefab = value; }
    public bool NormalizeMeshesHeight { get => normalizeMeshesHeight; set => normalizeMeshesHeight = value; }
    public Vector3 IntialMeshRotation { get => initialMeshRotation; set => initialMeshRotation = value; }
}
