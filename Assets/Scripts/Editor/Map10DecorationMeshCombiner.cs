using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class Map10DecorationMeshCombiner
{
    private const string ScenePath = "Assets/Scenes/map10.unity";
    private const string OutputFolder = "Assets/GameData/Generated/Map10CombinedDecoration";
    private const string CombinedRootName = "CombinedDecoration";
    private const string AutoRunMarkerPath = "Assets/GameData/Generated/Map10CombinedDecoration/autorun.trigger";
    private const float ChunkSize = 80f;

    [InitializeOnLoadMethod]
    private static void AutoRunIfRequested()
    {
        if (AssetDatabase.LoadMainAssetAtPath(AutoRunMarkerPath) == null)
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadMainAssetAtPath(AutoRunMarkerPath) == null)
            {
                return;
            }

            AssetDatabase.DeleteAsset(AutoRunMarkerPath);
            CombineDecorationMeshes();
        };
    }

    [MenuItem("DinoGrow/Maps/Combine Map10 Decoration Meshes")]
    public static void CombineDecorationMeshes()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var decorationLayer = LayerMask.NameToLayer("Decoration");
        if (decorationLayer < 0)
        {
            Debug.LogError("[Map10DecorationMeshCombiner] Decoration layer was not found.");
            return;
        }

        RemoveExistingCombinedRoot();
        EnsureOutputFolder();

        var root = new GameObject(CombinedRootName)
        {
            isStatic = true,
            layer = decorationLayer
        };

        var groups = new Dictionary<ChunkKey, List<CombineInstance>>();
        var originalRenderers = new List<MeshRenderer>();
        var originalColliders = new List<Collider>();

        foreach (var sceneRoot in scene.GetRootGameObjects())
        {
            CollectDecorationMeshes(sceneRoot, decorationLayer, groups, originalRenderers, originalColliders);
        }

        if (groups.Count == 0)
        {
            Object.DestroyImmediate(root);
            Debug.LogWarning("[Map10DecorationMeshCombiner] No enabled Decoration mesh renderers were found.");
            return;
        }

        var combinedVertexCount = 0L;
        foreach (var group in groups)
        {
            var mesh = new Mesh
            {
                name = $"Map10_Decoration_Chunk_{group.Key.X}_{group.Key.Z}_{group.Key.Material.GetInstanceID()}",
                indexFormat = IndexFormat.UInt32
            };

            mesh.CombineMeshes(group.Value.ToArray(), true, true, false);
            mesh.RecalculateBounds();
            combinedVertexCount += mesh.vertexCount;

            AssetDatabase.CreateAsset(mesh, $"{OutputFolder}/{mesh.name}.asset");

            var combinedObject = new GameObject(mesh.name)
            {
                isStatic = true,
                layer = decorationLayer
            };
            combinedObject.transform.SetParent(root.transform, false);
            combinedObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            var combinedRenderer = combinedObject.AddComponent<MeshRenderer>();
            combinedRenderer.sharedMaterial = group.Key.Material;
            combinedRenderer.shadowCastingMode = ShadowCastingMode.On;
            combinedRenderer.receiveShadows = true;
        }

        foreach (var renderer in originalRenderers)
        {
            renderer.enabled = false;
        }

        foreach (var collider in originalColliders)
        {
            collider.enabled = false;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"[Map10DecorationMeshCombiner] Combined {originalRenderers.Count} Decoration renderers into {groups.Count} meshes. " +
            $"Chunk size: {ChunkSize}. " +
            $"Combined vertices: {combinedVertexCount}. Disabled {originalColliders.Count} Decoration colliders.");
    }

    private static void CollectDecorationMeshes(
        GameObject sceneRoot,
        int decorationLayer,
        Dictionary<ChunkKey, List<CombineInstance>> groups,
        List<MeshRenderer> originalRenderers,
        List<Collider> originalColliders)
    {
        foreach (var collider in sceneRoot.GetComponentsInChildren<Collider>(true))
        {
            if (collider != null
                && collider.enabled
                && IsDecorationObject(collider.transform, decorationLayer))
            {
                originalColliders.Add(collider);
            }
        }

        foreach (var meshFilter in sceneRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter == null
                || meshFilter.sharedMesh == null
                || !IsDecorationObject(meshFilter.transform, decorationLayer))
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

    private static bool IsDecorationObject(Transform transform, int decorationLayer)
    {
        for (var current = transform; current != null; current = current.parent)
        {
            if (current.name == CombinedRootName)
            {
                return false;
            }

            if (current.gameObject.layer == decorationLayer)
            {
                return true;
            }
        }

        return false;
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
            foreach (var guid in AssetDatabase.FindAssets("Map10_Decoration_", new[] { OutputFolder }))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
    }

    private static void EnsureOutputFolder()
    {
        EnsureFolder("Assets", "GameData");
        EnsureFolder("Assets/GameData", "Generated");
        EnsureFolder("Assets/GameData/Generated", "Map10CombinedDecoration");
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
