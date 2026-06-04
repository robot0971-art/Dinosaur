using DinoGrow.Camera;
using DinoGrow.Gameplay.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace DinoGrow.Gameplay.Stage
{
    internal static class StageInitialMapDisabler
    {
        public static void DisableExistingSceneMapRoots(
            MonoBehaviour owner,
            bool disableSceneMaps,
            bool disableSceneNavMeshSurfaces,
            string mapBoundaryRootName)
        {
            if (!disableSceneMaps || owner == null)
            {
                return;
            }

            var activeScene = owner.gameObject.scene;
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            foreach (var root in activeScene.GetRootGameObjects())
            {
                DisableExistingNavMeshSurfaces(root, disableSceneNavMeshSurfaces);

                if (LooksLikeMapRoot(root, mapBoundaryRootName))
                {
                    root.SetActive(false);
                    continue;
                }

                if (ShouldKeepMainSceneRoot(root, owner.gameObject))
                {
                    continue;
                }
            }
        }

        private static void DisableExistingNavMeshSurfaces(GameObject root, bool disableSceneNavMeshSurfaces)
        {
            if (!disableSceneNavMeshSurfaces || root == null)
            {
                return;
            }

            foreach (var surface in root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true))
            {
                surface.enabled = false;
            }
        }

        private static bool ShouldKeepMainSceneRoot(GameObject root, GameObject ownerObject)
        {
            if (root == null || root == ownerObject || root.GetComponentInChildren<PlayerDinoController>(true) != null)
            {
                return true;
            }

            return root.GetComponentInChildren<Canvas>(true) != null
                || root.GetComponentInChildren<EventSystem>(true) != null
                || root.GetComponentInChildren<UnityEngine.Camera>(true) != null
                || root.GetComponentInChildren<Light>(true) != null;
        }

        private static bool LooksLikeMapRoot(GameObject root, string mapBoundaryRootName)
        {
            if (root == null)
            {
                return false;
            }

            if (root.GetComponentInChildren<PlayerDinoController>(true) != null
                || root.GetComponentInChildren<Canvas>(true) != null
                || root.GetComponentInChildren<EventSystem>(true) != null)
            {
                return false;
            }

            if (root.name.Contains("Map") || root.name.Contains("Ground") || root.name.Contains("Environment"))
            {
                return true;
            }

            if (root.GetComponentInChildren<EnvironmentSettingsController>(true) != null
                || root.GetComponentInChildren<Unity.AI.Navigation.NavMeshSurface>(true) != null
                || StageSceneObjectUtility.FindChildByName(root.transform, "PlayerStartPoint") != null
                || StageSceneObjectUtility.FindChildByName(root.transform, mapBoundaryRootName) != null)
            {
                return true;
            }

            var groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0)
            {
                return false;
            }

            var groundObjectCount = 0;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.layer != groundLayer)
                {
                    continue;
                }

                groundObjectCount++;
                if (groundObjectCount >= 8)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
