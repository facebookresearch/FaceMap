using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading;
using System;

public class TG_SideBySide_Rating : TrialGeneratorBase
{
    protected GameObject ratingCanvas;

    public TG_SideBySide_Rating(Dataset dataset) : base(dataset)
    {
        base.CameraLayout = new Vector2Int(1, 2);
        base.setting = "side_by_side";
        base.answeringMethod = AnsweringMethod.Rating;
        base.labelsPrefabPath = PrefabsPaths.labelsRatingSideBySide;

        ratingCanvas = GameObject.Instantiate(Resources.Load(PrefabsPaths.ratingCanvas) as GameObject);
    }

    public override void KeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdateRating();
            bool isNextTrialAvailable = NextTrial();
            if (!isNextTrialAvailable)
                NextScene();
        }
    }

    public override void CreateInstructions()
    {
        instructionsObject = GameObject.Instantiate(Resources.Load(PrefabsPaths.instructionsRatingPrefab) as GameObject);
        base.CreateInstructions();
    }

    protected override void GenerateAndLayoutMeshes(Stimulus stimulus)
    {
        GameObject referenceSource = LoadMesh(stimulus, ModelVersion.Reference);
        Transform referenceInstance = InstantiateLoadedResource(stimulus, referenceSource, "reference", "m2");
        currentTrialObjects.Add(referenceInstance.parent.gameObject);

        GameObject distortedSource = LoadMesh(stimulus, ModelVersion.DistortedA);
        Transform distortedInstance = InstantiateLoadedResource(stimulus, distortedSource, "distorted", "m1", referenceInstance);
        currentTrialObjects.Add(distortedInstance.parent.gameObject);
    }

    private void UpdateRating()
    {
        Transform ratingTextObject = ratingCanvas.transform.Find("Rating");
        currentRating = int.Parse(ratingTextObject.gameObject.GetComponent<UnityEngine.UI.Text>().text);
    }
}