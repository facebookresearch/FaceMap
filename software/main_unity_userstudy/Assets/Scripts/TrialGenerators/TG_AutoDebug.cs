/// <summary>
/// This class automatically iterates through all available trials and gives answer 0.
/// It can be used to check whether all required meshes were imported correctly.
/// If all required meshes are present script will automatically changes scene to the next one declared in the Build Settings (Unity).
/// If any of required meshes is missing lunched session will throw an error and get stuck.
/// </summary>
public class TG_AutoDebug : TG_2AFC_WithReference
{
    public TG_AutoDebug(Dataset dataset) : base(dataset)
    {
        base.setting = "auto_debug";
    }

    public override void GenerateTrial()
    {
        base.GenerateTrial();

        trials[currentTrialIndex].rating = 0;
        UpdateCurrentTime();

        bool isNextTrialAvailable = NextTrial();
        if (!isNextTrialAvailable)
            NextScene();
    }
}
