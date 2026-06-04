using DinoGrow.Core.Growth;
using DinoGrow.Gameplay;
using UnityEngine;

namespace DinoGrow.Gameplay.Enemy
{
    internal readonly struct EnemyLevelTextStyle
    {
        public EnemyLevelTextStyle(
            float heightPadding,
            float characterSize,
            int fontSize,
            Color fallbackColor,
            Color lowerLevelColor,
            Color sameLevelColor,
            Color higherLevelColor,
            bool usePlayerRelativeColor)
        {
            HeightPadding = heightPadding;
            CharacterSize = characterSize;
            FontSize = fontSize;
            FallbackColor = fallbackColor;
            LowerLevelColor = lowerLevelColor;
            SameLevelColor = sameLevelColor;
            HigherLevelColor = higherLevelColor;
            UsePlayerRelativeColor = usePlayerRelativeColor;
        }

        public float HeightPadding { get; }
        public float CharacterSize { get; }
        public int FontSize { get; }
        public Color FallbackColor { get; }
        public Color LowerLevelColor { get; }
        public Color SameLevelColor { get; }
        public Color HigherLevelColor { get; }
        public bool UsePlayerRelativeColor { get; }
    }

    internal static class EnemyLevelTextPresenter
    {
        public static TextMesh Ensure(
            Transform owner,
            TextMesh levelText,
            ref bool createdLevelText,
            EnemyLevelTextStyle style,
            int enemyLevel,
            PlayerProgress playerProgress,
            Transform cameraTransform)
        {
            if (levelText == null)
            {
                var labelObject = new GameObject("EnemyLevelText");
                labelObject.transform.SetParent(owner, false);
                levelText = labelObject.AddComponent<TextMesh>();
                createdLevelText = true;
            }

            levelText.anchor = TextAnchor.MiddleCenter;
            levelText.alignment = TextAlignment.Center;
            levelText.characterSize = style.CharacterSize;
            levelText.fontSize = style.FontSize;
            ConfigureMaterial(levelText);
            Refresh(levelText, enemyLevel, style, playerProgress);
            UpdateTransform(owner, levelText, style.HeightPadding, cameraTransform);
            return levelText;
        }

        public static void Refresh(
            TextMesh levelText,
            int enemyLevel,
            EnemyLevelTextStyle style,
            PlayerProgress playerProgress)
        {
            if (levelText == null)
            {
                return;
            }

            levelText.text = $"Lv. {enemyLevel}";
            ApplyColor(levelText, enemyLevel, style, playerProgress);
        }

        public static void ApplyColor(
            TextMesh levelText,
            int enemyLevel,
            EnemyLevelTextStyle style,
            PlayerProgress playerProgress)
        {
            if (levelText == null)
            {
                return;
            }

            var fallback = IsWhiteFallback(style.FallbackColor)
                ? style.SameLevelColor
                : style.FallbackColor;
            var palette = new EnemyLevelTextColorPalette(
                style.LowerLevelColor,
                style.SameLevelColor,
                style.HigherLevelColor,
                fallback);
            var playerLevel = playerProgress != null ? playerProgress.Level : 0;
            levelText.color = EnemyLevelTextColorRule.Resolve(
                enemyLevel,
                playerLevel,
                Application.isPlaying && style.UsePlayerRelativeColor,
                palette);
        }

        public static void UpdateTransform(
            Transform owner,
            TextMesh levelText,
            float heightPadding,
            Transform cameraTransform)
        {
            if (levelText == null)
            {
                return;
            }

            var levelTextTransform = levelText.transform;
            levelTextTransform.position = GetPosition(owner, heightPadding);
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

        private static void ConfigureMaterial(TextMesh targetText)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (font == null)
            {
                return;
            }

            targetText.font = font;

            if (targetText.TryGetComponent<MeshRenderer>(out var textRenderer))
            {
                textRenderer.sharedMaterial = font.material;
            }
        }

        private static Vector3 GetPosition(Transform owner, float heightPadding)
        {
            if (!RendererBoundsUtility.TryCalculateVisibleBounds(owner, out var bounds))
            {
                return owner.position + Vector3.up * heightPadding;
            }

            return new Vector3(bounds.center.x, bounds.max.y + heightPadding, bounds.center.z);
        }

        private static bool IsWhiteFallback(Color color)
        {
            return color.r > 0.98f && color.g > 0.98f && color.b > 0.98f;
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
