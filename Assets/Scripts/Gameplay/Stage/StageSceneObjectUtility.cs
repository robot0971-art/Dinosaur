using System.Collections.Generic;
using DinoGrow.Camera;
using DinoGrow.Gameplay.Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoGrow.Gameplay.Stage
{
    internal static class StageSceneObjectUtility
    {
        public static void DisableMapSceneCameras(Scene mapScene)
        {
            if (!mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            foreach (var root in mapScene.GetRootGameObjects())
            {
                foreach (var targetCamera in root.GetComponentsInChildren<UnityEngine.Camera>(true))
                {
                    targetCamera.enabled = false;
                }

                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                {
                    listener.enabled = false;
                }
            }
        }

        public static void ApplyMapEnvironment(Scene mapScene)
        {
            if (!mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            foreach (var root in mapScene.GetRootGameObjects())
            {
                var environment = root.GetComponentInChildren<EnvironmentSettingsController>(true);
                if (environment == null)
                {
                    continue;
                }

                environment.Apply();
                return;
            }
        }

        public static void ConfigureMapBillboards(Scene mapScene, Transform cameraTransform)
        {
            if (cameraTransform == null || !mapScene.IsValid() || !mapScene.isLoaded)
            {
                return;
            }

            foreach (var root in mapScene.GetRootGameObjects())
            {
                foreach (var billboard in root.GetComponentsInChildren<BillboardToCamera>(true))
                {
                    billboard.SetTarget(cameraTransform);
                }
            }
        }

        public static Transform FindInScene(Scene scene, string targetName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var result = FindChildByName(root.transform, targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static List<Transform> FindAllInScene(Scene scene, string targetName)
        {
            var results = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                FindChildrenByName(root.transform, targetName, results);
            }

            return results;
        }

        public static Transform FindChildByName(Transform root, string targetName)
        {
            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var result = FindChildByName(root.GetChild(i), targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void FindChildrenByName(Transform root, string targetName, List<Transform> results)
        {
            if (IsMatchingSceneObjectName(root.name, targetName))
            {
                results.Add(root);
            }

            for (var i = 0; i < root.childCount; i++)
            {
                FindChildrenByName(root.GetChild(i), targetName, results);
            }
        }

        private static bool IsMatchingSceneObjectName(string objectName, string targetName)
        {
            return objectName == targetName
                || objectName.StartsWith(targetName + " (", System.StringComparison.Ordinal);
        }
    }
}
