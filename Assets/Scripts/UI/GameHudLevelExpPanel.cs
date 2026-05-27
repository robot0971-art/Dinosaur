using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHudLevelExpPanel : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private bool applyLayout = true;
    [SerializeField] private Vector2 panelSize = new(600f, 140f);
    [SerializeField] private Vector2 levelTextPosition = new(-240f, 38f);
    [SerializeField] private Vector2 expLabelPosition = new(-240f, -38f);
    [SerializeField] private Vector2 expBarOffsetMin = new(130f, 34f);
    [SerializeField] private Vector2 expBarOffsetMax = new(-130f, -78f);
    [SerializeField] private Vector2 expValuePosition = new(175f, 0f);
    [SerializeField] private Vector2 expValueSize = new(210f, 96f);
    [SerializeField] private float levelFontSize = 44f;
    [SerializeField] private float expLabelFontSize = 40f;
    [SerializeField] private float expValueFontSize = 42f;

    [Header("Level")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private int startLevel = 1;
    [SerializeField] private string levelFormat = "Lv. {0}";

    [Header("EXP")]
    [SerializeField] private Image expBarFill;
    [SerializeField] private int currentExp;
    [SerializeField] private int maxExp = 50;

    [Header("EXP Value")]
    [SerializeField] private TextMeshProUGUI expValueText;
    [SerializeField] private string expValueFormat = "{0} / {1}";

    private int currentLevel = 1;
    private bool isMaxLevel;

    private void Start()
    {
        ApplyLayout();
        SetProgress(startLevel, currentExp, maxExp, false);
    }

    public void SetLevel(int newLevel)
    {
        currentLevel = Mathf.Max(1, newLevel);
        UpdateLevelDisplay();
    }

    public void SetExp(int newExp)
    {
        SetProgress(currentLevel, newExp, maxExp, false);
    }

    public void SetProgress(int newLevel, int newExp, int newMaxExp, bool newIsMaxLevel)
    {
        currentLevel = Mathf.Max(1, newLevel);
        maxExp = Mathf.Max(1, newMaxExp);
        isMaxLevel = newIsMaxLevel;
        currentExp = isMaxLevel ? maxExp : Mathf.Clamp(newExp, 0, maxExp);

        ApplyLayout();
        UpdateLevelDisplay();
        UpdateExpBarDisplay();
        UpdateExpValueText();
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public int GetCurrentExp()
    {
        return currentExp;
    }

    private void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, currentLevel);
        }
    }

    private void UpdateExpBarDisplay()
    {
        if (expBarFill != null)
        {
            expBarFill.fillAmount = isMaxLevel ? 1f : (float)currentExp / maxExp;
        }
    }

    private void UpdateExpValueText()
    {
        if (expValueText != null)
        {
            expValueText.text = isMaxLevel ? "EXP MAX" : string.Format(expValueFormat, currentExp, maxExp);
        }
    }

    private void ApplyLayout()
    {
        if (!applyLayout)
        {
            return;
        }

        if (TryGetComponent<RectTransform>(out var panelTransform))
        {
            panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSize.x);
            panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelSize.y);
        }

        ConfigureText(levelText, levelTextPosition, new Vector2(190f, 58f), levelFontSize, TextAlignmentOptions.Left);
        ConfigureExpLabel();
        ConfigureExpBar();
        ConfigureText(expValueText, expValuePosition, expValueSize, expValueFontSize, TextAlignmentOptions.Center);
    }

    private void ConfigureExpLabel()
    {
        var expLabelText = FindChildText("ExpLabelText");
        ConfigureText(expLabelText, expLabelPosition, new Vector2(190f, 58f), expLabelFontSize, TextAlignmentOptions.Left);
    }

    private void ConfigureExpBar()
    {
        if (expBarFill == null)
        {
            return;
        }

        var rectTransform = expBarFill.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = expBarOffsetMin;
        rectTransform.offsetMax = expBarOffsetMax;
    }

    private TextMeshProUGUI FindChildText(string childName)
    {
        var root = transform;
        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == childName && child.TryGetComponent<TextMeshProUGUI>(out var text))
            {
                return text;
            }
        }

        return null;
    }

    private static void ConfigureText(
        TextMeshProUGUI targetText,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        if (targetText == null)
        {
            return;
        }

        var rectTransform = targetText.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

        targetText.fontSize = fontSize;
        targetText.enableAutoSizing = false;
        targetText.textWrappingMode = TextWrappingModes.NoWrap;
        targetText.overflowMode = TextOverflowModes.Overflow;
        targetText.alignment = alignment;
    }
}
