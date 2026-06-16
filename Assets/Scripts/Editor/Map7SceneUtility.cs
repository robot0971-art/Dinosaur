using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine;

public static class Map7SceneUtility
{
    private const string Map4ScenePath = "Assets/Scenes/map4.unity";
    private const string Map7ScenePath = "Assets/Scenes/map7.unity";
    private const string GroundChunkRootName = "GroundChunks";
    private const string GroundSourceBackupRootName = "GroundSourceBackup";
    private const string Map4GroundChunkAssetFolder = "Assets/GameData/Generated/Map4GroundChunks";
    private const string GroundChunkAssetFolder = "Assets/GameData/Generated/Map7GroundChunks";
    private const float GroundChunkSize = 120f;

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

    [MenuItem("DinoGrow/Maps/Bake Map7 Ground Layer Chunks")]
    public static void BakeMap7GroundLayerChunks()
    {
        BakeGroundLayerChunks(
            Map7ScenePath,
            GroundChunkAssetFolder,
            "map7",
            "Map7",
            IsMap7GroundName);
    }

    [MenuItem("DinoGrow/Maps/Bake Map4 Ground Layer Chunks")]
    public static void BakeMap4GroundLayerChunks()
    {
        BakeGroundLayerChunks(
            Map4ScenePath,
            Map4GroundChunkAssetFolder,
            "map4",
            "Map4",
            IsMap4GroundName);
    }

    private static void BakeGroundLayerChunks(
        string scenePath,
        string assetFolder,
        string mapLabel,
        string meshNamePrefix,
        System.Func<Transform, bool> isGroundTransform)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            Debug.LogError($"Ground layer is missing. Add a Ground layer before baking {mapLabel} ground chunks.");
            return;
        }

        PrepareAssetFolder(assetFolder);
        AssetDatabase.DeleteAsset(assetFolder);
        PrepareAssetFolder(assetFolder);
        DeleteExistingRoot(GroundChunkRootName);

        var chunkRoot = new GameObject(GroundChunkRootName);
        var backupRoot = GameObject.Find(GroundSourceBackupRootName);
        if (backupRoot == null)
        {
            backupRoot = new GameObject(GroundSourceBackupRootName);
        }

        RestoreGroundSourceObjects(scene, groundLayer, isGroundTransform);

        var renderers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true))
            .Where(renderer => IsBakeableGroundRenderer(renderer, groundLayer, isGroundTransform))
            .ToList();

        if (renderers.Count == 0)
        {
            Debug.LogWarning($"No active Ground layer MeshRenderers were found for {mapLabel} ground chunk baking.");
            return;
        }

        var groups = new Dictionary<GroundChunkKey, List<MeshRenderer>>();
        foreach (var targetRenderer in renderers)
        {
            var key = GetChunkKey(targetRenderer.bounds.center, targetRenderer.sharedMaterial);
            if (!groups.TryGetValue(key, out var group))
            {
                group = new List<MeshRenderer>();
                groups.Add(key, group);
            }

            group.Add(targetRenderer);
        }

        var chunkCount = 0;
        var sourceCount = 0;
        foreach (var pair in groups)
        {
            var key = pair.Key;
            var group = pair.Value;
            var combines = new List<CombineInstance>(group.Count);

            foreach (var targetRenderer in group)
            {
                var filter = targetRenderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                combines.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    subMeshIndex = 0,
                    transform = filter.transform.localToWorldMatrix
                });
            }

            if (combines.Count == 0)
            {
                continue;
            }

            var mesh = new Mesh
            {
                name = $"{meshNamePrefix}_GroundChunk_{key.X}_{key.Z}_{chunkCount}",
                indexFormat = IndexFormat.UInt32
            };
            mesh.CombineMeshes(combines.ToArray(), true, true, false);
            mesh.RecalculateBounds();

            var chunkObject = new GameObject($"GroundChunk_{key.X}_{key.Z}_{chunkCount}");
            chunkObject.transform.SetParent(chunkRoot.transform, false);
            chunkObject.layer = groundLayer;
            chunkObject.isStatic = true;

            var meshPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{assetFolder}/{mesh.name}.asset");
            AssetDatabase.CreateAsset(mesh, meshPath);

            var chunkFilter = chunkObject.AddComponent<MeshFilter>();
            chunkFilter.sharedMesh = mesh;

            var chunkRenderer = chunkObject.AddComponent<MeshRenderer>();
            chunkRenderer.sharedMaterial = key.Material;
            chunkRenderer.shadowCastingMode = ShadowCastingMode.Off;
            chunkRenderer.receiveShadows = true;

            var chunkCollider = chunkObject.AddComponent<MeshCollider>();
            chunkCollider.sharedMesh = mesh;

            chunkCount++;
            sourceCount += group.Count;
        }

        foreach (var targetRenderer in renderers)
        {
            var sourceObject = targetRenderer.gameObject;
            sourceObject.SetActive(false);
            if (sourceObject.transform.parent == null || sourceObject.transform.parent.name == "Ground_Grassland")
            {
                sourceObject.transform.SetParent(backupRoot.transform, true);
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{mapLabel} ground chunks baked. Sources disabled: {sourceCount}, chunks created: {chunkCount}");
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

    private static bool IsBakeableGroundRenderer(
        MeshRenderer targetRenderer,
        int groundLayer,
        System.Func<Transform, bool> isGroundTransform)
    {
        if (targetRenderer == null
            || targetRenderer.gameObject.layer != groundLayer
            || !targetRenderer.gameObject.activeInHierarchy
            || targetRenderer.GetComponent<MeshFilter>()?.sharedMesh == null)
        {
            return false;
        }

        var name = targetRenderer.gameObject.name;
        if (name.StartsWith("GroundChunk_", System.StringComparison.Ordinal)
            || targetRenderer.GetComponentInParent<MeshCollider>()?.gameObject.name.StartsWith("GroundChunk_", System.StringComparison.Ordinal) == true)
        {
            return false;
        }

        return isGroundTransform(targetRenderer.transform);
    }

    private static void RestoreGroundSourceObjects(
        UnityEngine.SceneManagement.Scene scene,
        int groundLayer,
        System.Func<Transform, bool> isGroundTransform)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                if (target.gameObject.layer != groundLayer)
                {
                    continue;
                }

                if (isGroundTransform(target))
                {
                    target.gameObject.SetActive(true);
                }
            }
        }
    }

    private static GroundChunkKey GetChunkKey(Vector3 position, Material material)
    {
        return new GroundChunkKey(
            Mathf.FloorToInt(position.x / GroundChunkSize),
            Mathf.FloorToInt(position.z / GroundChunkSize),
            material);
    }

    private static bool IsMap7GroundName(Transform target)
    {
        return target.name.StartsWith("DesertGround_", System.StringComparison.Ordinal)
            || target.name == "Ground_Desert"
            || target.root.name.StartsWith("Ground_Desert", System.StringComparison.Ordinal);
    }

    private static bool IsMap4GroundName(Transform target)
    {
        return target.name.StartsWith("Meadow_", System.StringComparison.Ordinal)
            || target.name == "Ground_Grassland"
            || target.root.name.StartsWith("Ground_Grassland", System.StringComparison.Ordinal);
    }

    private static void PrepareAssetFolder(string folderPath)
    {
        var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            PrepareAssetFolder(parent);
        }

        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var folderName = Path.GetFileName(folderPath);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void DeleteExistingRoot(string rootName)
    {
        var existingRoot = GameObject.Find(rootName);
        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot);
        }
    }

    private readonly struct GroundChunkKey
    {
        public GroundChunkKey(int x, int z, Material material)
        {
            X = x;
            Z = z;
            Material = material;
        }

        public int X { get; }
        public int Z { get; }
        public Material Material { get; }
    }
}
