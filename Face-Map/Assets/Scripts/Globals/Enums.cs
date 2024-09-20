using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ModelVersion
{
    Reference,
    DistortedA,
    DistortedB
}

public enum ExperimentLayout
{
    RatingSideBySide,
    RatingSideBySideWithWorstCase,
    TwoAFCNoReference,
    TwoAFCWithReference,
    TwoAFCFlickerNoReference,
    ArtifactPreview,
    AutoDebug
    //Staircase2AFC
}

public enum AnsweringMethod
{
    Rating,
    ChooseOne,
    FlickerChooseOne,
    NoAnswer
}

public enum RotationScheme
{
    XYZRotation,
    OnlyUpAxisRotation
}