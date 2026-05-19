using System;
using System.Collections;
using DinoGrow.Core.Growth;
using DinoGrow.Gameplay.Animation;
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
        [SerializeField] private float deathDespawnDelay = 1.2f;

        private static Material[] prototypeMaterials;
        private Action<DinoEnemy> despawnHandler;
        private DeathEffectService deathEffectService;
        private PlayerProgress playerProgress;
        private Transform levelTextTransform;
        private Transform cameraTransform;
        private bool createdLevelText;
        private bool isDying;
        private Coroutine deathRoutine;
        private RigidbodyConstraints constraintsBeforeDeath;
        private bool hasConstraintsBeforeDeath;
        private Vector3 deathPosition;
        private Quaternion deathRotation;
        private bool hasDeathPose;

        public int Level => level;
        public bool IsDying => isDying;

        [Inject]
        public void Construct(PlayerProgress playerProgress, DeathEffectService deathEffectService)
        {
            this.playerProgress = playerProgress;
            this.deathEffectService = deathEffectService;
            ApplyLevelTextColor();
        }

        private void Awake()
        {
            if (animatorView == null)
            {
                animatorView = GetComponentInChildren<DinoAnimatorView>();
            }

            EnsureLevelText();
            RefreshLevelText();
            ApplyPrototypeMaterial();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
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
            if (Application.isPlaying && createdLevelText && levelText != null)
            {
                Destroy(levelText.gameObject);
            }
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
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
            }

            deathRoutine = StartCoroutine(PlayDeathThenDespawn());
        }

        private IEnumerator PlayDeathThenDespawn()
        {
            deathPosition = transform.position;
            deathRotation = transform.rotation;
            hasDeathPose = true;
            SetGameplayActive(false);
            FreezeRootMotion();
            SetLevelTextVisible(false);
            deathEffectService?.SpawnBlood(GetDeathEffectPosition());
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
                hasConstraintsBeforeDeath = true;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
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
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
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
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
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
            var renderers = GetComponentsInChildren<Renderer>();
            var hasBounds = false;
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
            }

            return hasBounds ? bounds.center : transform.position;
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
            if (levelText == null)
            {
                var labelObject = new GameObject("EnemyLevelText");
                labelObject.transform.SetParent(transform, false);
                levelText = labelObject.AddComponent<TextMesh>();
                createdLevelText = true;
            }

            levelTextTransform = levelText.transform;
            levelText.anchor = TextAnchor.MiddleCenter;
            levelText.alignment = TextAlignment.Center;
            levelText.characterSize = levelTextCharacterSize;
            levelText.fontSize = levelTextFontSize;
            ApplyLevelTextColor();
            UpdateLevelTextTransform();
        }

        private void RefreshLevelText()
        {
            if (levelText == null)
            {
                return;
            }

            levelText.text = $"Lv. {level}";
            ApplyLevelTextColor();
        }

        private void ApplyLevelTextColor()
        {
            if (levelText == null)
            {
                return;
            }

            var palette = new EnemyLevelTextColorPalette(
                lowerLevelTextColor,
                sameLevelTextColor,
                higherLevelTextColor,
                levelTextColor);
            var playerLevel = playerProgress != null ? playerProgress.Level : 0;
            levelText.color = EnemyLevelTextColorRule.Resolve(
                level,
                playerLevel,
                Application.isPlaying && usePlayerRelativeLevelTextColor,
                palette);
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
            if (levelTextTransform == null)
            {
                return;
            }

            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
            }

            levelTextTransform.position = GetLevelTextPosition();
            if (cameraTransform == null)
            {
                return;
            }

            var lookDirection = levelTextTransform.position - cameraTransform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                levelTextTransform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        private Vector3 GetLevelTextPosition()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var hasBounds = false;
            var bounds = new Bounds(transform.position, Vector3.zero);

            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
            }

            if (!hasBounds)
            {
                return transform.position + Vector3.up * levelTextHeightPadding;
            }

            return new Vector3(bounds.center.x, bounds.max.y + levelTextHeightPadding, bounds.center.z);
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
    }

    public readonly struct EnemyLevelTextColorPalette
    {
        public EnemyLevelTextColorPalette(Color lowerOrEqual, Color sameLevel, Color higherLevel, Color fallback)
        {
            LowerOrEqual = lowerOrEqual;
            SameLevel = sameLevel;
            HigherLevel = higherLevel;
            Fallback = fallback;
        }

        public Color LowerOrEqual { get; }
        public Color SameLevel { get; }
        public Color HigherLevel { get; }
        public Color Fallback { get; }
    }

    public static class EnemyLevelTextColorRule
    {
        public static Color Resolve(
            int enemyLevel,
            int playerLevel,
            bool usePlayerRelativeColor,
            EnemyLevelTextColorPalette palette)
        {
            if (!usePlayerRelativeColor || playerLevel <= 0)
            {
                return palette.Fallback;
            }

            if (enemyLevel > playerLevel)
            {
                return palette.HigherLevel;
            }

            return enemyLevel == playerLevel ? palette.SameLevel : palette.LowerOrEqual;
        }
    }
}
