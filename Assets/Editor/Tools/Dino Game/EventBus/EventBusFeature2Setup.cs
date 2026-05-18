using DinoGrow.Infrastructure.Events;
using UnityEditor;
using UnityEngine;

namespace Dino.Editor.Tools
{
    public static class EventBusFeature2Setup
    {
        [MenuItem("Tools/Dino Game/Feature 2/Create EventBus Test Object")]
        public static void CreateEventBusTestObject()
        {
            const string objectName = "EventBusTest";
            var testObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(testObject, "Create EventBus Test Object");

            if (testObject.GetComponent<EventBusSubscriberExample>() == null)
            {
                Undo.AddComponent<EventBusSubscriberExample>(testObject);
            }

            Selection.activeGameObject = testObject;
            EditorGUIUtility.PingObject(testObject);
            EditorUtility.SetDirty(testObject);

            Debug.Log("EventBusTest 오브젝트 준비 완료. Play를 눌러 EventBus 구독 로그를 확인하세요.", testObject);
        }
    }
}
