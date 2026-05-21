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
        [SerializeField] private Vector3 positionDamping = new Vector3(0.12f, 0.12f, 0.12f);
        [SerializeField] private Vector2 aimDamping = new Vector2(0.08f, 0.08f);
        [SerializeField] private bool lockCursorOnPlay = true;
        [SerializeField] private bool createStableTarget = true;
        [SerializeField] private Vector3 stableTargetLocalPosition = new Vector3(0f, 1.4f, 0f);

        private CinemachineCamera cinemachineCamera;
        private CinemachineFollow follow;
        private CinemachineRotationComposer rotationComposer;
        private Transform stableTarget;
        private float yaw;
        private float pitch = 20f;

        private void Awake()
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
            follow = GetComponent<CinemachineFollow>();
            rotationComposer = GetComponent<CinemachineRotationComposer>();
            EnsureStableTarget();
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

        private void LateUpdate()
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

        private void EnsureStableTarget()
        {
            if (!createStableTarget || cinemachineCamera == null)
            {
                return;
            }

            var target = cinemachineCamera.Target;
            var currentTrackingTarget = target.TrackingTarget;
            if (currentTrackingTarget == null || currentTrackingTarget.parent == null)
            {
                return;
            }

            var targetRoot = currentTrackingTarget.parent;
            stableTarget = targetRoot.Find("PlayerCameraTarget");
            if (stableTarget == null)
            {
                var targetObject = new GameObject("PlayerCameraTarget");
                stableTarget = targetObject.transform;
                stableTarget.SetParent(targetRoot, false);
            }

            stableTarget.localPosition = stableTargetLocalPosition;
            stableTarget.localRotation = Quaternion.identity;

            target.TrackingTarget = stableTarget;
            target.LookAtTarget = stableTarget;
            cinemachineCamera.Target = target;
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
            settings.PositionDamping = positionDamping;
            settings.RotationDamping = Vector3.zero;
            settings.QuaternionDamping = 0f;
            follow.TrackerSettings = settings;

            if (rotationComposer != null)
            {
                rotationComposer.Damping = aimDamping;
            }
        }
    }
}
