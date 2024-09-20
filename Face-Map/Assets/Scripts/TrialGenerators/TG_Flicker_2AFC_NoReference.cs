using UnityEngine;

public class TG_2AFC_Flicker_NoReference : TrialGeneratorBase
{
    private int activeMeshIndex;
    private int activeLabelIndex;

    public TG_2AFC_Flicker_NoReference(Dataset dataset) : base(dataset)
    {
        base.CameraLayout = new Vector2Int(1, 1);
        base.setting = "2AFC_Flicker_NoReference";
        base.answeringMethod = AnsweringMethod.FlickerChooseOne;
        base.labelsPrefabPath = PrefabsPaths.labels2AFCFlickerNoReference;
    }

    protected override void GenerateAndLayoutMeshes(Stimulus stimulus)
    {
        activeMeshIndex = System.Convert.ToInt32(UnityEngine.Random.value > 0.5f);
        activeLabelIndex = 0;

        string firstCameraLabel = "m1";

        GameObject distortedASource = LoadMesh(stimulus, ModelVersion.DistortedA);
        Transform distortedAInstance = InstantiateLoadedResource(stimulus, distortedASource, "distortionA", firstCameraLabel);
        currentTrialObjects.Add(distortedAInstance.parent.gameObject);

        GameObject distortedBSource = LoadMesh(stimulus, ModelVersion.DistortedB);
        Transform distortedBInstance = InstantiateLoadedResource(stimulus, distortedBSource, "distortionB", firstCameraLabel, distortedAInstance);
        currentTrialObjects.Add(distortedBInstance.parent.gameObject);
        
        ChangeActiveStateOfMeshes();
        ChangeActiveStateOfLabels();
    }

    private void SwapActive()
    {
        activeMeshIndex = 1 - activeMeshIndex;
        activeLabelIndex = 1 - activeLabelIndex;

        DisplayBlankScreenFlicker();

        ChangeActiveStateOfMeshes();
        ChangeActiveStateOfLabels();
    }

    private void ChangeActiveStateOfMeshes()
    {
        Debug.Log("Currently active: " + currentTrialObjects[activeMeshIndex].name);

        Renderer[] rends;

        //Active object
        rends = currentTrialObjects[activeMeshIndex].GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in rends)
        {
            renderer.enabled = true;
        }

        //Inactive object
        rends = currentTrialObjects[1-activeMeshIndex].GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in rends)
        {
            renderer.enabled = false;
        }
    }

    private void ChangeActiveStateOfLabels()
    {
        labelsCanvas.transform.GetChild(activeLabelIndex).gameObject.SetActive(true);
        labelsCanvas.transform.GetChild(1-activeLabelIndex).gameObject.SetActive(false);
    }

    public override void KeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            SwapActive();
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            UpdateCurrentAnswerAndTiming(activeMeshIndex);
            bool isNextTrialAvailable = NextTrial();
            if (!isNextTrialAvailable)
                NextScene();
        }
    }

    public override void UpdateCurrentAnswerAndTiming(float rating)
    {
        currentRating = (int)rating;
        trials[currentTrialIndex].rating = currentRating;
        UpdateCurrentTime();
    }

    public override void CreateInstructions()
    {
        instructionsObject = GameObject.Instantiate(Resources.Load(PrefabsPaths.instructions2AFCFlickerNoReference) as GameObject);
        base.CreateInstructions();
    }

    public override bool NextTrial(bool iterate = true, int iterator = 1)
    {
        //If training mode active display notifcation and stay on the same trial
        if (trainingMode)
        {
            if (currentRating == 1)
            {
                StaticMethods.Notify(PrefabsPaths.wrongAnswerNotification, ref notification);
                return true;
            }
        }

        //Remove any potentially existing notification from screen
        if (notification != null)
            GameObject.Destroy(notification);

        return base.NextTrial(iterate, iterator);
    }

    protected void DisplayBlankScreenFlicker()
    {
        if (blankScreenTimeForFlicker < 0.0001f)
            return;

        GameObject canvas = GameObject.Instantiate(Resources.Load(PrefabsPaths.blankScreenCanvas) as GameObject);
        canvas.GetComponent<Canvas>().sortingOrder = -1; // changing sorting order for blanks creen so it covers only the mesh without other UI elements
        canvas.GetComponent<DestroyAfterTimePeriod>().time = blankScreenTimeForFlicker;
    }
}
