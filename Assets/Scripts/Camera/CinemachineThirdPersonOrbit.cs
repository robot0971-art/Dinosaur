using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoGrow.Camera
{
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachineFollow))]
    public sealed class CinemachineThirdPersonOrbit : MonoBehaviour
    {
        [SerializeField] private float distance = 6f;
        [SerializeField] private float height = 3f;
        [SerializeField] private float mouseSensitivity = 0.2f;
        [SerializeField] private float minPitch = -15f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private bool lockCursorOnPlay = true;

        private CinemachineFollow follow;
        private float yaw;
        private float pitch = 20f;

        private void Awake()
        {
            follow = GetComponent<CinemachineFollow>();
            ApplyOffset();
        }

        private void OnEnable()
        {
            if (!lockCursorOnPlay || !Application.isPlaying)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (!lockCursorOnPlay || !Application.isPlaying)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var delta = mouse.delta.ReadValue();
            yaw += delta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minPitch, maxPitch);

            ApplyOffset();
        }

        private void ApplyOffset()
        {
            if (follow == null)
            {
                return;
            }

            var orbit = Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distance);
            follow.FollowOffset = orbit + Vector3.up * height;

            var settings = follow.TrackerSettings;
            settings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            settings.PositionDamping = Vector3.zero;
            settings.RotationDamping = Vector3.zero;
            settings.QuaternionDamping = 0f;
            follow.TrackerSettings = settings;
        }
    }
}
