using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading;

public class TG_StairCase : TG_2AFC_WithReference
{
    System.DateTime lastWriteTime = System.DateTime.MinValue;
    List<Stimulus> stimuli_all;
    List<List<Stimulus>> stimuli_lists;

    int iter = 1;

    public TG_StairCase(Dataset dataset) : base( dataset)
    {
        stimuli_all = new List<Stimulus>(stimuli);
        stimuli_lists = new List<List<Stimulus>>();
        SplitAllStimuliToLists();

        int trialn = WaitForAndReadInstructionFromMatlab();
        Debug.Log(trialn);
        currentTrialNumber = trialn;
    }

    private void SplitAllStimuliToLists()
    {
        //// Get list of uniqe types
        HashSet<string> uniqeTypes = new HashSet<string>();
        foreach (Stimulus stimulus in stimuli_all)
        {
            string variant = GetDistortionTypeFromName(stimulus);
            uniqeTypes.Add(variant);
        }
        List<string> uniqueTypesL = uniqeTypes.ToList<string>();

        //// Get lists os stimuli
        var lists = stimuli_all.GroupBy(x => GetDistortionTypeFromName(x));
        Debug.Log("Groups: " + lists);
        foreach (var list in lists)
        {
            stimuli_lists.Add(list.ToList());
        }

        //// Debug
        ///
        foreach (List<Stimulus> stimuli in stimuli_lists)
        {
            Debug.Log("Count: " + stimuli.Count + "   Example: " + stimuli[0].distortionB);
        }
    }

    private string GetDistortionTypeFromName(Stimulus stimulus)
    {
        string[] parts = stimulus.distortionB.Split('-');
        string[] type_parts = parts[1].Split('_');
        string variant = "";

        if (type_parts.Length == 2)
            variant = type_parts[0];
        else
            variant = type_parts[0] + "_" + type_parts[1];
        return variant;
    }

    public override bool Shuffle { get { return false; } set { } }

    public override void UpdateCurrentAnswerAndTiming(float rating)
    {
        base.UpdateCurrentAnswerAndTiming(rating);
        WriteResponse(currentRating);
    }

    public override bool NextTrial(bool iterate = true, int iterator = 1)
    {
        Debug.Log("NExt trial in");
        iter++;

        int trialn = WaitForAndReadInstructionFromMatlab();
        Debug.Log(trialn);
        currentTrialNumber = trialn;

        //Debug.Log("empty");
        DestroyAllCurrentGameObjects();
        GenerateTrial();
        return true;
    }

    private void WriteResponse(int response)
    {
        System.IO.File.WriteAllText("tmp.txt", response.ToString());
        Debug.Log("Response written to TMP");
    }

    /// <summary>
    /// This method is used to communicate with Matlab when Staircase Procedure Layout is selected.
    /// </summary>
    /// <returns></returns>
    private int WaitForAndReadInstructionFromMatlab()
    {
        string filename = "resp.txt";

        string instruction;
        int index = 0;
        int list_index = 0;

        //while (System.IO.File.GetLastWriteTime(filename) == lastWriteTime)
        while (!System.IO.File.Exists(filename))
        {
            Debug.Log("Waiting");
        }

        Thread.Sleep(200);

        while (System.IO.File.Exists(filename))
        {
            Debug.Log("Trying to read");
            try
            {
                Debug.Log("Reading File");
                instruction = System.IO.File.ReadAllText(filename);
                Debug.Log("File Read");
                string[] instructions = instruction.Split(' ');
                index = int.Parse(instructions[0]);
                list_index = int.Parse(instructions[1])-1;
                //Debug.Log("List index: " + list_index + "   trial index: " + index);
                stimuli = stimuli_lists[list_index];
                lastWriteTime = System.IO.File.GetLastWriteTime(filename);
                System.IO.File.Delete(filename);
                continue;
            }
            catch
            {
                Debug.Log("Retrying");
            }
        }

        if (index == -1)
            StaticMethods.CloseApp();

        //System.IO.File.Delete(filename);
        return index;
    }
    protected override void SaveResults()
    {

    }

    public override void UpdateProgressInfo()
    {
        GameObject progresObject = GameObject.Find("Progress");
        UpdateTrialNumberInformation infoScript = progresObject.GetComponent<UpdateTrialNumberInformation>();
        //infoScript.UpdateInfo(new Vector2Int(GetNumberOfTrials().x-1, 300));
        infoScript.UpdateInfo(new Vector2Int(iter, 300));
    }

    public override void CreateInstructions()
    {
        instructionsObject = GameObject.Instantiate(Resources.Load(PrefabsPaths.instructionsStaircase) as GameObject);
        //base.CreateInstructions();
    }
}
