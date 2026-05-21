using UnityEngine;
using UnityEditor;

public class QuickDrop : EditorWindow
{
    [MenuItem("DinoGrow/Drop to Ground %d")]
    public static void DropObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("땅에 맞출 오브젝트를 먼저 선택해 주세요!");
            return;
        }

        foreach (GameObject obj in selectedObjects)
        {
            Vector3 rayStart = obj.transform.position;
            rayStart.y += 50f;

            RaycastHit hit;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f))
            {
                Undo.RecordObject(obj.transform, "Drop to Ground");

                MeshFilter meshFilter = obj.GetComponentInChildren<MeshFilter>();
                float pivotOffset = 0f;

                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    pivotOffset = meshFilter.sharedMesh.bounds.min.y * obj.transform.lossyScale.y;
                }

                Vector3 targetPosition = hit.point;
                targetPosition.y -= pivotOffset;

                obj.transform.position = targetPosition;
            }
        }
        Debug.Log($"{selectedObjects.Length}개의 오브젝트가 울퉁불퉁한 땅 표면에 칼같이 맞춰졌습니다!");
    }

    [MenuItem("DinoGrow/Drop All to Ground")]
    public static void DropAllObjects()
    {
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag("EditorOnly");

        int count = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Tree") || obj.name.Contains("Rock") ||
                obj.name.Contains("Grass") || obj.name.Contains("Bush") ||
                obj.name.Contains("Flower") || obj.name.Contains("Stump"))
            {
                Vector3 rayStart = obj.transform.position;
                rayStart.y += 50f;

                RaycastHit hit;
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f))
                {
                    Undo.RecordObject(obj.transform, "Drop to Ground");

                    MeshFilter meshFilter = obj.GetComponentInChildren<MeshFilter>();
                    float pivotOffset = 0f;

                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        pivotOffset = meshFilter.sharedMesh.bounds.min.y * obj.transform.lossyScale.y;
                    }

                    Vector3 targetPosition = hit.point;
                    targetPosition.y -= pivotOffset;

                    obj.transform.position = targetPosition;
                    count++;
                }
            }
        }

        Debug.Log($"모든 오브젝트 {count}개가 울퉁불퉁한 땅 표면에 맞춰졌습니다!");
    }
}