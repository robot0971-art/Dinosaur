using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Map7SceneUtility
{
    private const string Map7ScenePath = "Assets/Scenes/map7.unity";

    [MenuItem("DinoGrow/Maps/Prepare Map7 Scene")]
    public static void PrepareMap7Scene()
    {
        var scene = EditorSceneManager.OpenScene(Map7ScenePath, OpenSceneMode.Single);
        var groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            Debug.LogError("Ground layer is missing. Add a Ground layer before preparing map7.");
            return;
        }

        var groundCount = 0;
        var spawnCount = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                if (target.name.StartsWith("DesertGround_") || target.name == "Ground_Desert")
                {
                    target.gameObject.layer = groundLayer;
                    groundCount++;
                }

                if (target.GetComponent<DesertEnemySpawnData>() != null)
                {
                    spawnCount++;
                }
            }
        }

        AddSceneToBuildSettings(Map7ScenePath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"map7 prepared. Ground objects: {groundCount}, enemy spawn markers: {spawnCount}");
    }

    [MenuItem("DinoGrow/Maps/Validate Map7 Scene")]
    public static void ValidateMap7Scene()
    {
        var scene = EditorSceneManager.OpenScene(Map7ScenePath, OpenSceneMode.Single);
        var rootObjects = scene.GetRootGameObjects();
        var objectCount = rootObjects.Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
        var groundCount = rootObjects.Sum(root => root.GetComponentsInChildren<Transform>(true)
            .Count(target => target.name.StartsWith("DesertGround_") || target.name == "Ground_Desert"));
        var spawnCount = rootObjects.Sum(root => root.GetComponentsInChildren<DesertEnemySpawnData>(true).Length);
        var hasPlayer = rootObjects.Any(root => root.GetComponentInChildren<DinoGrow.Gameplay.Player.PlayerDinoController>(true) != null);
        var hasEnemySpawner = rootObjects.Any(root => root.GetComponentInChildren<DinoGrow.Gameplay.Enemy.EnemySpawner>(true) != null);

        Debug.Log(
            $"map7 validation: objects={objectCount}, ground={groundCount}, enemySpawnMarkers={spawnCount}, " +
            $"hasPlayer={hasPlayer}, hasEnemySpawner={hasEnemySpawner}");
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(scene => scene.path == scenePath))
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
