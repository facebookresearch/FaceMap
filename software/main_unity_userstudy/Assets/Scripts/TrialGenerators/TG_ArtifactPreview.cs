using UnityEngine;

public class TG_ArtifactPreview : TrialGeneratorBase
{
    public override bool Shuffle { get { return false; } set { } }

    public TG_ArtifactPreview(Dataset dataset) : base(dataset)
    {
        base.CameraLayout = new Vector2Int(1, 2);
        base.setting = "artifact_preview";
        base.answeringMethod = AnsweringMethod.NoAnswer;
        base.labelsPrefabPath = PrefabsPaths.labelsArtifactPreview;
    }

    protected override void GenerateAndLayoutMeshes(Stimulus stimulus)
    {
        GameObject referenceSource = LoadMesh(stimulus, ModelVersion.Reference);
        Transform referenceInstance = InstantiateLoadedResource(stimulus, referenceSource, "reference", "m1");
        currentTrialObjects.Add(referenceInstance.parent.gameObject);

        GameObject distortedSource = LoadMesh(stimulus, ModelVersion.DistortedA);
        Transform distortedInstance = InstantiateLoadedResource(stimulus, distortedSource, "distorted", "m2", referenceInstance);
        currentTrialObjects.Add(distortedInstance.parent.gameObject);
    }

    public override void KeyboardInput()
    {
        bool isNextTrialAvailable;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            isNextTrialAvailable = NextTrial(true, -1);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            isNextTrialAvailable = NextTrial(true, 1);
        }
        if (Input.GetKeyDown(KeyCode.Space))
            NextScene();
    }

    protected override void SaveResults()
    {

    }

    public override void UpdateCurrentAnswerAndTiming(float rating)
    {

    }

    public override bool NextTrial(bool iterate = true, int iterator = 1)
    {
        bool isNextTrialAvailable = true;

        if (iterate)
        {
            currentTrialNumber = currentTrialNumber + iterator;
            DestroyAllCurrentGameObjects();
        }

        if (currentTrialNumber >= trialIndices.Count)
            currentTrialNumber = 0;
        else if (currentTrialNumber < 0)
            currentTrialNumber = trialIndices.Count - 1;

        GenerateTrial();

        return isNextTrialAvailable;
    }

    public override void CreateInstructions()
    {
        instructionsObject = GameObject.Instantiate(Resources.Load(PrefabsPaths.instructionsArtifactPreview) as GameObject);
        base.CreateInstructions();
    }
}