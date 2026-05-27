using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class Map10GroundMeshCombiner
{
    private const string ScenePath = "Assets/Scenes/map10.unity";
    private const string OutputFolder = "Assets/GameData/Generated/Map10CombinedGround";
    private const string CombinedRootName = "CombinedGround";
    private const float ChunkSize = 60f;

    [MenuItem("DinoGrow/Maps/Combine Map10 Ground Meshes")]
    public static void CombineGroundMeshes()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            Debug.LogError("[Map10GroundMeshCombiner] Ground layer was not found.");
            return;
        }

        RemoveExistingCombinedRoot();
        EnsureOutputFolder();

        var root = new GameObject(CombinedRootName);
        var groups = new Dictionary<ChunkKey, List<CombineInstance>>();
        var originalRenderers = new List<MeshRenderer>();

        foreach (var sceneRoot in scene.GetRootGameObjects())
        {
            CollectGroundMeshes(sceneRoot, groundLayer, groups, originalRenderers);
        }

        if (groups.Count == 0)
        {
            Object.DestroyImmediate(root);
            Debug.LogWarning("[Map10GroundMeshCombiner] No enabled Ground mesh renderers were found.");
            return;
        }

        var combinedVertexCount = 0L;
        foreach (var group in groups)
        {
            var mesh = new Mesh
            {
                name = $"Map10_Ground_Chunk_{group.Key.X}_{group.Key.Z}",
                indexFormat = IndexFormat.UInt32
            };

            mesh.CombineMeshes(group.Value.ToArray(), true, true, false);
            mesh.RecalculateBounds();
            combinedVertexCount += mesh.vertexCount;

            AssetDatabase.CreateAsset(mesh, $"{OutputFolder}/{mesh.name}.asset");

            var combinedObject = new GameObject(mesh.name);
            combinedObject.layer = groundLayer;
            combinedObject.isStatic = true;
            combinedObject.transform.SetParent(root.transform, false);
            combinedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var combinedRenderer = combinedObject.AddComponent<MeshRenderer>();
            combinedRenderer.sharedMaterial = group.Key.Material;
            combinedRenderer.shadowCastingMode = ShadowCastingMode.Off;
            combinedRenderer.receiveShadows = true;
        }

        foreach (var renderer in originalRenderers)
        {
            renderer.enabled = false;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"[Map10GroundMeshCombiner] Combined {originalRenderers.Count} Ground renderers into {groups.Count} meshes. " +
            $"Chunk size: {ChunkSize}. " +
            $"Combined vertices: {combinedVertexCount}. Original renderers disabled; colliders kept.");
    }

    private static void CollectGroundMeshes(
        GameObject sceneRoot,
        int groundLayer,
        Dictionary<ChunkKey, List<CombineInstance>> groups,
        List<MeshRenderer> originalRenderers)
    {
        foreach (var meshFilter in sceneRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter == null
                || meshFilter.sharedMesh == null
                || meshFilter.gameObject.layer != groundLayer
                || meshFilter.transform.root.name == CombinedRootName)
            {
                continue;
            }

            var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null
                || meshRenderer.sharedMaterials == null
                || meshRenderer.sharedMaterials.Length == 0)
            {
                continue;
            }

            var mesh = meshFilter.sharedMesh;
            for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                var materialIndex = Mathf.Min(subMeshIndex, meshRenderer.sharedMaterials.Length - 1);
                var material = meshRenderer.sharedMaterials[materialIndex];
                if (material == null)
                {
                    continue;
                }

                var key = new ChunkKey(
                    Mathf.FloorToInt(meshRenderer.bounds.center.x / ChunkSize),
                    Mathf.FloorToInt(meshRenderer.bounds.center.z / ChunkSize),
                    material);

                if (!groups.TryGetValue(key, out var instances))
                {
                    instances = new List<CombineInstance>();
                    groups.Add(key, instances);
                }

                instances.Add(new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = subMeshIndex,
                    transform = meshFilter.transform.localToWorldMatrix
                });
            }

            originalRenderers.Add(meshRenderer);
        }
    }

    private static void RemoveExistingCombinedRoot()
    {
        var existing = GameObject.Find(CombinedRootName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        if (AssetDatabase.IsValidFolder(OutputFolder))
        {
            foreach (var guid in AssetDatabase.FindAssets("Map10_Ground_", new[] { OutputFolder }))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
    }

    private static void EnsureOutputFolder()
    {
        EnsureFolder("Assets", "GameData");
        EnsureFolder("Assets/GameData", "Generated");
        EnsureFolder("Assets/GameData/Generated", "Map10CombinedGround");
    }

    private static void EnsureFolder(string parent, string child)
    {
        var path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private readonly struct ChunkKey
    {
        public ChunkKey(int x, int z, Material material)
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
