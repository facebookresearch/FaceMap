using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading;
using System;



public class TG_SideBySide_Rating_WithWorstCase : TG_SideBySide_Rating
{
    public TG_SideBySide_Rating_WithWorstCase(Dataset dataset) : base(dataset)
    {
        base.CameraLayout = new Vector2Int(1, 3);
        base.setting = "Rating_WithWorstCase";
        base.answeringMethod = AnsweringMethod.Rating;
        base.labelsPrefabPath = PrefabsPaths.labelsTriplets;
    }

    protected override void GenerateAndLayoutMeshes(Stimulus stimulus)
    {
        GameObject referenceSource = LoadMesh(stimulus, ModelVersion.Reference);
        Transform referenceInstance = InstantiateLoadedResource(stimulus, referenceSource, "reference", "m1");
        currentTrialObjects.Add(referenceInstance.parent.gameObject);

        GameObject distortedSource = LoadMesh(stimulus, ModelVersion.DistortedA);
        Transform distortedInstance = InstantiateLoadedResource(stimulus, distortedSource, "distorted", "m2", referenceInstance);
        currentTrialObjects.Add(distortedInstance.parent.gameObject);

        GameObject worstSource = LoadMesh(stimulus, ModelVersion.DistortedB);
        Transform worstInstance = InstantiateLoadedResource(stimulus, worstSource, "worst_case", "m3", referenceInstance);
        currentTrialObjects.Add(worstInstance.parent.gameObject);
    }
}
