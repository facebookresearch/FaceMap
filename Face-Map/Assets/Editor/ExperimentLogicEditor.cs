using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ExperimentLogic))]
[CanEditMultipleObjects]
public class ExperimentLogicEditor : Editor
{
    SerializedProperty datasetFile;
    SerializedProperty loadTrialsBasedOnPID;

    SerializedProperty cameraPrefab;
    SerializedProperty overrideObjectMaterial;

    SerializedProperty experimentLayout;
    SerializedProperty trainingMode;
    SerializedProperty shuffleTrials;

    SerializedProperty normalizeMeshSize;
    SerializedProperty intialMeshRotation;

    SerializedProperty rotationAllowed;
    SerializedProperty zoomAllowed;
    SerializedProperty translationAllowed;
    SerializedProperty autoRotationEnabled;
    SerializedProperty rotationScheme;

    SerializedProperty minimumTimePerTrial;
    SerializedProperty softCapTrialDuration;
    SerializedProperty blankScreenTime;

    SerializedProperty dropboxResultsDirectory;
    SerializedProperty dropboxFeedbackDirectory;

    SerializedProperty blankScreenTimeForFlicker;

    void OnEnable()
    {
        datasetFile = serializedObject.FindProperty("datasetFile");
        loadTrialsBasedOnPID = serializedObject.FindProperty("loadTrialsBasedOnPID");

        cameraPrefab = serializedObject.FindProperty("cameraPrefab");
        overrideObjectMaterial = serializedObject.FindProperty("overrideObjectMaterial");

        experimentLayout = serializedObject.FindProperty("experimentLayout");
        trainingMode = serializedObject.FindProperty("trainingMode");
        shuffleTrials = serializedObject.FindProperty("shuffleTrials");
        normalizeMeshSize = serializedObject.FindProperty("normalizeMeshSize");
        intialMeshRotation = serializedObject.FindProperty("intialMeshRotation");

        rotationAllowed = serializedObject.FindProperty("rotationAllowed");
        zoomAllowed = serializedObject.FindProperty("zoomAllowed");
        translationAllowed = serializedObject.FindProperty("translationAllowed");
        autoRotationEnabled = serializedObject.FindProperty("autoRotationEnabled");
        rotationScheme = serializedObject.FindProperty("rotationScheme");

        minimumTimePerTrial = serializedObject.FindProperty("minimumTimePerTrial");
        softCapTrialDuration = serializedObject.FindProperty("softCapTrialDuration");
        blankScreenTime = serializedObject.FindProperty("blankScreenTime");

        dropboxResultsDirectory = serializedObject.FindProperty("dropboxResultsDirectory");
        dropboxFeedbackDirectory = serializedObject.FindProperty("dropboxFeedbackDirectory");

        blankScreenTimeForFlicker = serializedObject.FindProperty("blankScreenTimeForFlicker");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Trials list");
        EditorGUILayout.PropertyField(datasetFile);
        EditorGUILayout.PropertyField(loadTrialsBasedOnPID);
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Required prefabs");
        EditorGUILayout.PropertyField(cameraPrefab);
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Experiment Settings");
        EditorGUILayout.PropertyField(experimentLayout);
        EditorGUILayout.PropertyField(trainingMode);
        EditorGUILayout.PropertyField(shuffleTrials);
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Mesh Options");
        EditorGUILayout.PropertyField(normalizeMeshSize);
        EditorGUILayout.PropertyField(intialMeshRotation);
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Interaction modifiers");
        EditorGUILayout.PropertyField(rotationScheme);
        EditorGUILayout.PropertyField(rotationAllowed);
        EditorGUILayout.PropertyField(translationAllowed);
        EditorGUILayout.PropertyField(zoomAllowed);
        EditorGUILayout.PropertyField(autoRotationEnabled);
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Trial timing");
        EditorGUILayout.PropertyField(minimumTimePerTrial);
        EditorGUILayout.PropertyField(softCapTrialDuration);
        EditorGUILayout.PropertyField(blankScreenTime);
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Dropbox settings");
        EditorGUILayout.PropertyField(dropboxResultsDirectory);
        EditorGUILayout.PropertyField(dropboxFeedbackDirectory);
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Optional parameters");
        EditorGUILayout.PropertyField(overrideObjectMaterial);
        EditorGUILayout.PropertyField(blankScreenTimeForFlicker);
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        serializedObject.ApplyModifiedProperties();
    }
}