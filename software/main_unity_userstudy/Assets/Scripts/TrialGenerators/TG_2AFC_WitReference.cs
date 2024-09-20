using UnityEngine;

public class TG_2AFC_WithReference : TrialGeneratorBase
{
    protected bool swapped;

    public TG_2AFC_WithReference(Dataset dataset) : base(dataset)
    {
        base.CameraLayout = new Vector2Int(1, 3);
        base.setting = "2AFC_SideBySide_WithReference";
        base.answeringMethod = AnsweringMethod.ChooseOne;
        base.labelsPrefabPath = PrefabsPaths.labels2AFCWithReference;
    }

    protected override void GenerateAndLayoutMeshes(Stimulus stimulus)
    {
        swapped = (UnityEngine.Random.value > 0.5f);
        string firstCameraLabel = "m1", secondCameraLabel = "m3";
        if (swapped)
        {
            firstCameraLabel = "m3";
            secondCameraLabel = "m1";
        }

        GameObject referenceSource = LoadMesh(stimulus, ModelVersion.Reference);
        Transform referenceInstance = InstantiateLoadedResource(stimulus, referenceSource, "reference", "m2");
        currentTrialObjects.Add(referenceInstance.parent.gameObject);

        GameObject distortedASource = LoadMesh(stimulus, ModelVersion.DistortedA);
        Transform distortedAInstance = InstantiateLoadedResource(stimulus, distortedASource, "distortionA", firstCameraLabel, referenceInstance);
        currentTrialObjects.Add(distortedAInstance.parent.gameObject);

        GameObject distortedBSource = LoadMesh(stimulus, ModelVersion.DistortedB);
        Transform distortedBInstance = InstantiateLoadedResource(stimulus, distortedBSource, "distortionB", secondCameraLabel, referenceInstance);
        currentTrialObjects.Add(distortedBInstance.parent.gameObject);
    }

    public override void KeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            UpdateCurrentAnswerAndTiming(0);
            bool isNextTrialAvailable = NextTrial();
            if (!isNextTrialAvailable)
                NextScene();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            UpdateCurrentAnswerAndTiming(1);
            bool isNextTrialAvailable = NextTrial();
            if (!isNextTrialAvailable)
                NextScene();
        }
    }

    public override void UpdateCurrentAnswerAndTiming(float rating)
    {
        Debug.Log("Swapped on saving: " + swapped.ToString());
        currentRating = (int)rating;
        Debug.Log("Rating before change: " + currentRating.ToString());
        if (swapped)
        {
            currentRating = 1 - (int)rating;
            Debug.Log("Rating after change: " + currentRating.ToString());
        }
        trials[currentTrialIndex].rating = currentRating;
        UpdateCurrentTime();
    }

    public override void CreateInstructions()
    {
        InstantiateInstruction();
        base.CreateInstructions();
    }

    public virtual void InstantiateInstruction()
    {
        instructionsObject = GameObject.Instantiate(Resources.Load(PrefabsPaths.instructions2AFCWithReference) as GameObject);
    }

    public override bool NextTrial(bool iterate = true, int iterator = 1)
    {
        //If training mode active display notifcation and stay on the same trial
        if(trainingMode)
        {
            // if (currentRating == 1)
            // {
            //     StaticMethods.Notify(PrefabsPaths.wrongAnswerNotification, ref notification);
            //     return true;
            // }
        }

        //Remove any potentially existing notification from screen
        if (notification != null)
            GameObject.Destroy(notification);

        return base.NextTrial(iterate, iterator);
    }
}
