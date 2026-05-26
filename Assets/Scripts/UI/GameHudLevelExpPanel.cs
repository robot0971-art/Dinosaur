using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHudLevelExpPanel : MonoBehaviour
{
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
}
