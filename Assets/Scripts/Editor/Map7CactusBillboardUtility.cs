using DinoGrow.Camera;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Map7CactusBillboardUtility
{
    private const string Map7ScenePath = "Assets/Scenes/map7.unity";

    [MenuItem("DinoGrow/Maps/Add Billboard To Map7 Cacti")]
    public static void AddBillboardToMap7Cacti()
    {
        var previousScenePath = SceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(Map7ScenePath, OpenSceneMode.Single);
        var addedCount = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                if (!IsCactusObject(target.gameObject.name))
                {
                    continue;
                }

                if (target.GetComponent<BillboardToCamera>() != null)
                {
                    continue;
                }

                Undo.AddComponent<BillboardToCamera>(target.gameObject);
                EditorUtility.SetDirty(target.gameObject);
                addedCount++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Added BillboardToCamera to {addedCount} map7 cactus objects.");

        if (!string.IsNullOrWhiteSpace(previousScenePath) && previousScenePath != Map7ScenePath)
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }
    }

    private static bool IsCactusObject(string objectName)
    {
        return objectName.StartsWith("Cactus_", System.StringComparison.Ordinal)
            || objectName == "cactus1"
            || objectName == "cactus2"
            || objectName == "cactus3"
            || objectName == "cactus3flower";
    }
}
