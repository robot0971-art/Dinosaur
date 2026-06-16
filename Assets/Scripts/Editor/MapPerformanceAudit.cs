using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapPerformanceAudit
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/map4.unity",
        "Assets/Scenes/map7.unity",
        "Assets/Scenes/map10.unity"
    };

    [MenuItem("DinoGrow/Maps/Audit Random Map Performance")]
    public static void AuditRandomMapPerformance()
    {
        var originalScenePath = EditorSceneManager.GetActiveScene().path;
        var lines = new System.Collections.Generic.List<string>();
        foreach (var scenePath in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            LogSceneReport(scene, lines);
        }

        if (!string.IsNullOrWhiteSpace(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        const string outputPath = "Assets/GameData/Generated/MapPerformanceAudit.txt";
        File.WriteAllLines(outputPath, lines);
        AssetDatabase.ImportAsset(outputPath);
        Debug.Log($"[MapPerformanceAudit] Wrote report to {outputPath}");
    }

    private static void LogSceneReport(Scene scene, System.Collections.Generic.List<string> lines)
    {
        var roots = scene.GetRootGameObjects();
        var transforms = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var renderers = roots.SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
        var activeRenderers = renderers.Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy).ToArray();
        var meshFilters = roots.SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true)).ToArray();
        var activeMeshFilters = meshFilters.Where(filter => filter.gameObject.activeInHierarchy).ToArray();
        var colliders = roots.SelectMany(root => root.GetComponentsInChildren<Collider>(true)).ToArray();
        var activeColliders = colliders.Where(collider => collider.enabled && collider.gameObject.activeInHierarchy).ToArray();
        var cameras = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray();
        var activeCameras = cameras.Where(camera => camera.enabled && camera.gameObject.activeInHierarchy).ToArray();
        var missingScripts = roots.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
        var groundChunks = transforms.Count(transform => transform.name.StartsWith("GroundChunk_") || transform.name.StartsWith("Map10_Ground_Chunk_"));
        var groundBackupObjects = transforms.Count(transform => transform.root.name == "GroundSourceBackup");
        var mapBoundaries = transforms.Count(transform => transform.name == "MapBoundary");

        var activeVertexCount = 0L;
        var activeTriangleCount = 0L;
        foreach (var filter in activeMeshFilters)
        {
            if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>()?.enabled != true)
            {
                continue;
            }

            activeVertexCount += filter.sharedMesh.vertexCount;
            activeTriangleCount += filter.sharedMesh.triangles.Length / 3;
        }

        var summary =
            $"[MapPerformanceAudit] {scene.path} | " +
            $"objects={transforms.Length}, activeObjects={transforms.Count(t => t.gameObject.activeInHierarchy)}, " +
            $"meshRenderers={renderers.Length}, activeMeshRenderers={activeRenderers.Length}, " +
            $"activeVerts={activeVertexCount:N0}, activeTris={activeTriangleCount:N0}, " +
            $"colliders={colliders.Length}, activeColliders={activeColliders.Length}, " +
            $"cameras={cameras.Length}, activeCameras={activeCameras.Length}, " +
            $"groundChunks={groundChunks}, groundSourceBackupObjects={groundBackupObjects}, mapBoundaries={mapBoundaries}, " +
            $"missingScripts={missingScripts}";
        Debug.Log(summary);
        lines.Add(summary);

        foreach (var heavy in activeMeshFilters
            .Where(filter => filter.sharedMesh != null && filter.GetComponent<MeshRenderer>()?.enabled == true)
            .OrderByDescending(filter => filter.sharedMesh.vertexCount)
            .Take(8))
        {
            lines.Add(
                $"[MapPerformanceAudit:Heavy] {scene.path} | {GetPath(heavy.transform)} | " +
                $"mesh={heavy.sharedMesh.name}, verts={heavy.sharedMesh.vertexCount:N0}, tris={heavy.sharedMesh.triangles.Length / 3:N0}");
        }
    }

    private static string GetPath(Transform target)
    {
        var path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = $"{target.name}/{path}";
        }

        return path;
    }
}
