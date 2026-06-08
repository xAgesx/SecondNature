using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class IntroCutsceneSetup
{
    [MenuItem("SecondNature/Setup Intro Cutscene", false, 0)]
    private static void Setup()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            EditorUtility.DisplayDialog("Error", "No Camera.main found.", "OK");
            return;
        }

        if (GameObject.Find("IntroCutscene") != null)
        {
            if (!EditorUtility.DisplayDialog("Overwrite?", "IntroCutscene already exists. Recreate?", "Yes", "Cancel"))
                return;
            var existing = GameObject.Find("IntroCutscene");
            if (existing != null) Undo.DestroyObjectImmediate(existing);
        }

        Undo.SetCurrentGroupName("Setup Intro Cutscene");
        int group = Undo.GetCurrentGroup();

        GameObject root = new GameObject("IntroCutscene");
        Undo.RegisterCreatedObjectUndo(root, "Create IntroCutscene");

        var cutscene = root.AddComponent<IntroCutscene>();
        cutscene.sfxSource = root.AddComponent<AudioSource>();
        cutscene.sfxSource.spatialBlend = 0f;
        cutscene.sfxSource.playOnAwake = false;

        var so = new SerializedObject(cutscene);
        so.FindProperty("fireAlarmClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Fire alarm sound.mp3");
        so.FindProperty("headHitClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Boy Screaming Sound Effect.mp3");
        so.ApplyModifiedProperties();

        UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        Selection.activeGameObject = root;
        Undo.CollapseUndoOperations(group);

        Debug.Log("[IntroCutsceneSetup] Created. Script auto-finds GoHere* waypoints at runtime. No manual assignment needed.");
    }

    [MenuItem("SecondNature/Setup Intro Cutscene", true)]
    private static bool Validate() => Camera.main != null;
}
