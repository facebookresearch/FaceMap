using UnityEngine;

public class TG_2AFC_NoReference : TG_2AFC_WithReference
{
    public TG_2AFC_NoReference(Dataset dataset) : base(dataset)
    {
        base.CameraLayout = new Vector2Int(1, 2);
        base.setting = "2AFC_SideBySide_NoReference";
        base.answeringMethod = AnsweringMethod.ChooseOne;
        base.labelsPrefabPath = PrefabsPaths.labels2AFCNoReference;
    }

    protected override void GenerateAndLayoutMeshes(Stimulus stimulus)
    {
        swapped = (UnityEngine.Random.value > 0.5f);
        string firstCameraLabel = "m1", secondCameraLabel = "m2";
        if (swapped)
        {
            firstCameraLabel = "m2";
            secondCameraLabel = "m1";
        }

        GameObject distortedASource = LoadMesh(stimulus, ModelVersion.DistortedA);
        Transform distortedAInstance = InstantiateLoadedResource(stimulus, distortedASource, "distortionA", firstCameraLabel);
        currentTrialObjects.Add(distortedAInstance.parent.gameObject);

        GameObject distortedBSource = LoadMesh(stimulus, ModelVersion.DistortedB);
        Transform distortedBInstance = InstantiateLoadedResource(stimulus, distortedBSource, "distortionB", secondCameraLabel, distortedAInstance);
        currentTrialObjects.Add(distortedBInstance.parent.gameObject);
    }

    public override void InstantiateInstruction()
    {
        instructionsObject = GameObject.Instantiate(Resources.Load(PrefabsPaths.instructions2AFCNoReference) as GameObject);
    }
}