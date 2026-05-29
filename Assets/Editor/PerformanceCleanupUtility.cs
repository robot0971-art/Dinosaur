using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PerformanceCleanupUtility
{
    [MenuItem("Tools/Dino Game/Performance/Cleanup Active Scene Missing Scripts")]
    public static void CleanupActiveSceneMissingScripts()
    {
        var scene = SceneManager.GetActiveScene();
        var removed = 0;
        var affectedObjects = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target.gameObject);
                if (count <= 0)
                {
                    continue;
                }

                affectedObjects++;
                removed += count;
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target.gameObject);
            }
        }

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[PerformanceCleanup] Removed {removed} missing script component(s) from {affectedObjects} object(s) in {scene.name}.");
        }
        else
        {
            Debug.Log($"[PerformanceCleanup] No missing script components found in {scene.name}.");
        }
    }

    [MenuItem("Tools/Dino Game/Performance/Cleanup Project Missing Scripts")]
    public static void CleanupProjectMissingScripts()
    {
        var totalRemoved = 0;
        var totalObjects = 0;

        foreach (var prefabGuid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(prefabGuid);
            var root = PrefabUtility.LoadPrefabContents(path);
            var removed = CleanupGameObjectTree(root, out var affectedObjects);
            if (removed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                totalRemoved += removed;
                totalObjects += affectedObjects;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        var activeScenePath = SceneManager.GetActiveScene().path;
        foreach (var sceneGuid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(sceneGuid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var removed = CleanupScene(scene, out var affectedObjects);
            if (removed > 0)
            {
                EditorSceneManager.SaveScene(scene);
                totalRemoved += removed;
                totalObjects += affectedObjects;
            }
        }

        if (!string.IsNullOrEmpty(activeScenePath))
        {
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PerformanceCleanup] Project cleanup removed {totalRemoved} missing script component(s) from {totalObjects} object(s).");
    }

    [MenuItem("Tools/Dino Game/Performance/Remove Map Enemy Spawn Markers")]
    public static void RemoveMapEnemySpawnMarkers()
    {
        var activeScenePath = SceneManager.GetActiveScene().path;
        var totalRemoved = 0;
        var mapScenes = new[]
        {
            "Assets/Scenes/map4.unity",
            "Assets/Scenes/map7.unity",
            "Assets/Scenes/map10.unity"
        };

        foreach (var path in mapScenes)
        {
            if (!System.IO.File.Exists(path))
            {
                continue;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var removed = RemoveSpawnMarkers(scene);
            if (removed <= 0)
            {
                continue;
            }

            totalRemoved += removed;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PerformanceCleanup] Removed {removed} enemy spawn marker object(s) from {scene.name}.");
        }

        if (!string.IsNullOrEmpty(activeScenePath))
        {
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"[PerformanceCleanup] Removed {totalRemoved} enemy spawn marker object(s) from map scenes.");
    }

    private static int CleanupScene(Scene scene, out int affectedObjects)
    {
        var removed = 0;
        affectedObjects = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            removed += CleanupGameObjectTree(root, out var rootAffectedObjects);
            affectedObjects += rootAffectedObjects;
        }

        return removed;
    }

    private static int CleanupGameObjectTree(GameObject root, out int affectedObjects)
    {
        var removed = 0;
        affectedObjects = 0;

        foreach (var target in root.GetComponentsInChildren<Transform>(true))
        {
            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target.gameObject);
            if (count <= 0)
            {
                continue;
            }

            affectedObjects++;
            removed += count;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target.gameObject);
        }

        return removed;
    }

    private static int RemoveSpawnMarkers(Scene scene)
    {
        var targets = new System.Collections.Generic.List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                if (target.name.StartsWith("EnemySpawn_Lv", System.StringComparison.Ordinal))
                {
                    targets.Add(target.gameObject);
                }
            }
        }

        foreach (var target in targets)
        {
            Object.DestroyImmediate(target);
        }

        return targets.Count;
    }
}
