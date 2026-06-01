using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class InspectorSelectionRepairUtility
{
    [MenuItem("Tools/Dino Game/Editor/Repair Inspector Selection")]
    public static void RepairInspectorSelection()
    {
        Selection.activeObject = null;
        Selection.objects = new Object[0];
        ActiveEditorTracker.sharedTracker.ForceRebuild();
        InternalEditorUtility.RepaintAllViews();
        Debug.Log("[InspectorSelectionRepairUtility] Cleared selection and rebuilt inspector tracker.");
    }

    [MenuItem("Tools/Dino Game/Editor/Reopen Inspectors")]
    public static void ReopenInspectors()
    {
        Selection.activeObject = null;
        Selection.objects = new Object[0];
        DestroyBrokenEditors();

        var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        if (inspectorType != null)
        {
            foreach (var inspector in Resources.FindObjectsOfTypeAll(inspectorType))
            {
                if (inspector is EditorWindow window)
                {
                    window.Close();
                }
            }
        }

        ActiveEditorTracker.sharedTracker.ForceRebuild();
        InternalEditorUtility.RepaintAllViews();
        EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        Debug.Log("[InspectorSelectionRepairUtility] Closed stale inspectors and scheduled a fresh Inspector window.");
    }

    [MenuItem("Tools/Dino Game/Editor/Destroy Broken Inspector Editors")]
    public static void DestroyBrokenInspectorEditors()
    {
        Selection.activeObject = null;
        Selection.objects = new Object[0];
        var destroyed = DestroyBrokenEditors();
        ActiveEditorTracker.sharedTracker.ForceRebuild();
        InternalEditorUtility.RepaintAllViews();
        Debug.Log($"[InspectorSelectionRepairUtility] Destroyed {destroyed} broken editor instance(s).");
    }

    private static int DestroyBrokenEditors()
    {
        var destroyed = 0;
        foreach (var editor in Resources.FindObjectsOfTypeAll<Editor>())
        {
            if (editor == null || editor.target != null)
            {
                continue;
            }

            Object.DestroyImmediate(editor);
            destroyed++;
        }

        return destroyed;
    }
}
