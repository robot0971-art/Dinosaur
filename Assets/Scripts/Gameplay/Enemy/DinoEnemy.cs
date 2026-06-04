using System;
using System.Collections;
using System.Collections.Generic;
using DinoGrow.Core.Growth;
using DinoGrow.Gameplay;
using DinoGrow.Gameplay.Animation;
using DinoGrow.Infrastructure.DI;
using UnityEngine;
using VContainer;

namespace DinoGrow.Gameplay.Enemy
{
    [ExecuteAlways]
    public sealed class DinoEnemy : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private bool usePrototypeLevelMaterial;
        [SerializeField] private Color levelOneColor = new(0.25f, 0.95f, 0.35f);
        [SerializeField] private Color levelTwoColor = new(0.2f, 0.65f, 1f);
        [SerializeField] private Color levelThreeColor = new(1f, 0.35f, 0.2f);

        [Header("Level Text")]
        [SerializeField] private TextMesh levelText;
        [Tooltip("Vertical padding above the dinosaur render bounds.")]
        [SerializeField] private float levelTextHeightPadding = 0.5f;
        [Tooltip("Visible world-space size of the level text. Change this when the text should look bigger or smaller.")]
        [SerializeField] private float levelTextCharacterSize = 0.12f;
        [Tooltip("TextMesh font texture resolution. This mostly affects sharpness, not visible world-space size.")]
        [SerializeField] private int levelTextFontSize = 48;
        [SerializeField] private Color levelTextColor = Color.white;

        [Header("Level Text Colors")]
        [SerializeField] private bool usePlayerRelativeLevelTextColor = true;
        [SerializeField] private Color lowerLevelTextColor = new(0.3f, 1f, 0.35f);
        [SerializeField] private Color sameLevelTextColor = new(1f, 0.9f, 0.25f);
        [SerializeField] private Color higherLevelTextColor = new(1f, 0.25f, 0.2f);

        [Header("Death")]
        [SerializeField] private DinoAnimatorView animatorView;
        [SerializeField] private Transform mouthEffectOrigin;
        [SerializeField] private Vector3 mouthEffectFallbackOffset = new(0f, 1.05f, 0.75f);
        [SerializeField] private bool spawnDeathEffect;
        [SerializeField] private float deathDespawnDelay = 1.2f;

        private static Material[] prototypeMaterials;
        private static readonly List<DinoEnemy> ActiveEnemies = new();
        private Action<DinoEnemy> eatenHandler;
        private Action<DinoEnemy> despawnHandler;
        private DeathEffectService deathEffectService;
        private PlayerProgress playerProgress;
        private Transform cameraTransform;
        private bool createdLevelText;
        private bool isDying;
        private Coroutine deathRoutine;
        private RigidbodyConstraints constraintsBeforeDeath;
        private bool isKinematicBeforeDeath;
        private bool hasConstraintsBeforeDeath;
        private Vector3 deathPosition;
        private Quaternion deathRotation;
        private bool hasDeathPose;

        public int Level => level;
        public bool IsDying => isDying;
        public static IReadOnlyList<DinoEnemy> Active => ActiveEnemies;

        [Inject]
        public void Construct(
            PlayerProgress playerProgress,
            DeathEffectService deathEffectService,
            CameraReference cameraReference)
        {
            this.playerProgress = playerProgress;
            this.deathEffectService = deathEffectService;
            cameraTransform ??= cameraReference.Transform;
            ApplyLevelTextColor();
        }

        private void Awake()
        {
            if (animatorView == null)
            {
                animatorView = GetComponentInChildren<DinoAnimatorView>();
            }

            if (mouthEffectOrigin == null)
            {
                mouthEffectOrigin = TransformSearchUtility.FindChildByName(transform, "Head_end")
                    ?? TransformSearchUtility.FindChildByName(transform, "Head");
            }

            EnsureLevelText();
            RefreshLevelText();
            ApplyPrototypeMaterial();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                RegisterActiveEnemy();
                ResetRuntimeState();
                return;
            }

            if (!Application.isPlaying)
            {
                EnsureLevelText();
                RefreshLevelText();
                ApplyPrototypeMaterial();
            }
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (levelText == null)
            {
                ScheduleEditorLevelTextRefresh();
                return;
            }

            EnsureLevelText();
            RefreshLevelText();
            ApplyPrototypeMaterial();
        }

        private void LateUpdate()
        {
            if (isDying)
            {
                LockDeathPoseRoot();
                return;
            }

            UpdateLevelTextTransform();
        }

        private void OnDestroy()
        {
            UnregisterActiveEnemy();
            if (Application.isPlaying && createdLevelText && levelText != null)
            {
                Destroy(levelText.gameObject);
            }
        }

        private void OnDisable()
        {
            UnregisterActiveEnemy();
        }

        public void SetLevel(int value)
        {
            level = Mathf.Clamp(value, 1, 20);
            RefreshLevelText();
            ApplyPrototypeMaterial();
        }

        public void SetDespawnHandler(Action<DinoEnemy> handler)
        {
            despawnHandler = handler;
        }

        public void SetEatenHandler(Action<DinoEnemy> handler)
        {
            eatenHandler = handler;
        }

        public void RefreshLevelTextColor()
        {
            ApplyLevelTextColor();
        }

        public void Eaten()
        {
            if (isDying)
            {
                return;
            }

            isDying = true;
            eatenHandler?.Invoke(this);
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
            }

            deathRoutine = StartCoroutine(PlayDeathThenDespawn());
        }

        public Vector3 GetMouthEffectPosition()
        {
            if (mouthEffectOrigin != null)
            {
                return mouthEffectOrigin.position;
            }

            return transform.TransformPoint(mouthEffectFallbackOffset);
        }

        public float GetContactRadius()
        {
            if (!RendererBoundsUtility.TryCalculateVisibleBounds(transform, out var bounds))
            {
                return 1f;
            }

            return Mathf.Max(0.8f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.45f);
        }

        private void RegisterActiveEnemy()
        {
            if (!ActiveEnemies.Contains(this))
            {
                ActiveEnemies.Add(this);
            }
        }

        private void UnregisterActiveEnemy()
        {
            ActiveEnemies.Remove(this);
        }

        public void OnPlayerBitten()
        {
            animatorView?.PlayAttack();

            if (TryGetComponent(out EnemyWanderMovement wanderMovement))
            {
                wanderMovement.OnPlayerBitten();
            }
        }

        private IEnumerator PlayDeathThenDespawn()
        {
            deathPosition = transform.position;
            deathRotation = transform.rotation;
            hasDeathPose = true;
            SetGameplayActive(false);
            FreezeRootMotion();
            SetLevelTextVisible(false);
            if (spawnDeathEffect)
            {
                deathEffectService?.SpawnBlood(GetDeathEffectPosition());
            }

            animatorView?.SetDead(true);

            var delay = Mathf.Max(0f, deathDespawnDelay);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            deathRoutine = null;
            if (despawnHandler != null)
            {
                despawnHandler.Invoke(this);
                yield break;
            }

            Destroy(gameObject);
        }

        private void ResetRuntimeState()
        {
            isDying = false;
            deathRoutine = null;
            hasDeathPose = false;
            RestoreRootMotion();
            SetGameplayActive(true);
            SetLevelTextVisible(true);
            animatorView?.SetDead(false);
        }

        private void SetGameplayActive(bool active)
        {
            foreach (var targetCollider in GetComponents<Collider>())
            {
                targetCollider.enabled = active;
            }

            if (TryGetComponent(out EnemyWanderMovement wanderMovement))
            {
                wanderMovement.enabled = active;
            }
        }

        private void FreezeRootMotion()
        {
            if (!TryGetComponent(out Rigidbody body))
            {
                return;
            }

            if (!hasConstraintsBeforeDeath)
            {
                constraintsBeforeDeath = body.constraints;
                isKinematicBeforeDeath = body.isKinematic;
                hasConstraintsBeforeDeath = true;
            }

            StopBody(body);
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.Sleep();
        }

        private void RestoreRootMotion()
        {
            if (!hasConstraintsBeforeDeath || !TryGetComponent(out Rigidbody body))
            {
                return;
            }

            body.constraints = constraintsBeforeDeath;
            body.isKinematic = isKinematicBeforeDeath;
            StopBody(body);
            hasConstraintsBeforeDeath = false;
        }

        private void LockDeathPoseRoot()
        {
            if (!hasDeathPose)
            {
                return;
            }

            transform.SetPositionAndRotation(deathPosition, deathRotation);
            if (TryGetComponent(out Rigidbody body))
            {
                body.position = deathPosition;
                body.rotation = deathRotation;
                StopBody(body);
            }
        }

        private void SetLevelTextVisible(bool visible)
        {
            if (levelText != null)
            {
                levelText.gameObject.SetActive(visible);
            }
        }

        private Vector3 GetDeathEffectPosition()
        {
            return RendererBoundsUtility.TryCalculateVisibleBounds(transform, out var bounds)
                ? bounds.center
                : transform.position;
        }

        private void ApplyPrototypeMaterial()
        {
            if (!usePrototypeLevelMaterial)
            {
                return;
            }

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                targetRenderer.sharedMaterial = GetMaterialForLevel(level);
            }
        }

        private void EnsureLevelText()
        {
            levelText = EnemyLevelTextPresenter.Ensure(
                transform,
                levelText,
                ref createdLevelText,
                CreateLevelTextStyle(),
                level,
                playerProgress,
                cameraTransform);
        }

        private void RefreshLevelText()
        {
            EnemyLevelTextPresenter.Refresh(levelText, level, CreateLevelTextStyle(), playerProgress);
        }

        private void ApplyLevelTextColor()
        {
            EnemyLevelTextPresenter.ApplyColor(levelText, level, CreateLevelTextStyle(), playerProgress);
        }

        private void ScheduleEditorLevelTextRefresh()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || Application.isPlaying || !isActiveAndEnabled)
                {
                    return;
                }

                EnsureLevelText();
                RefreshLevelText();
                ApplyPrototypeMaterial();
            };
#endif
        }

        private void UpdateLevelTextTransform()
        {
            EnemyLevelTextPresenter.UpdateTransform(transform, levelText, levelTextHeightPadding, cameraTransform);
        }

        private EnemyLevelTextStyle CreateLevelTextStyle()
        {
            return new EnemyLevelTextStyle(
                levelTextHeightPadding,
                levelTextCharacterSize,
                levelTextFontSize,
                levelTextColor,
                lowerLevelTextColor,
                sameLevelTextColor,
                higherLevelTextColor,
                usePlayerRelativeLevelTextColor);
        }

        private Material GetMaterialForLevel(int targetLevel)
        {
            prototypeMaterials ??= new Material[3];
            var index = Mathf.Clamp(targetLevel, 1, 3) - 1;
            if (prototypeMaterials[index] != null)
            {
                return prototypeMaterials[index];
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = $"Prototype Enemy Lv{index + 1}"
            };

            SetMaterialColor(material, GetColorForLevel(index + 1));
            prototypeMaterials[index] = material;
            return material;
        }

        private Color GetColorForLevel(int targetLevel)
        {
            return targetLevel switch
            {
                1 => levelOneColor,
                2 => levelTwoColor,
                _ => levelThreeColor
            };
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void StopBody(Rigidbody body)
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

}
