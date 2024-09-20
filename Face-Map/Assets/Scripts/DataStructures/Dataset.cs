using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Dataset
{
    public List<Stimulus> stimuli;
}

[Serializable]
public class Stimulus
{
    public string dataset = "Examples";
    public string directory = "ExampleDataset/ASAP_Meshes";
    public string basemesh = "head";
    public string reference = "head_ref.obj";
    public string distortionA = "head_P1_D1_1";
    public string distortionB = "head_P1_D1_2";
    public float init_rotation_z = 0F;
    public float init_scale = 1F;
    public int batch = 100;
    public int index = 0;
}


[Serializable]
public class TrialCollection
{
    public List<Trial> trials;
}

[Serializable]
public class Trial
{
    public string userID;
    public int rating;
    public float time;
    public bool rotateLeft = false;
    public Stimulus stimulus;
}
