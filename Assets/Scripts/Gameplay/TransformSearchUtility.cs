using UnityEngine;

namespace DinoGrow.Gameplay
{
    internal static class TransformSearchUtility
    {
        public static Transform FindChildByName(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

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
    }
}
