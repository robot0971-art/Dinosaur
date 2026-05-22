// ============================================================
// GameHudLevelExpPanel.cs
// ============================================================
// 이 스크립트가 하는 일:
// 레벨 배경 패널 안에 레벨 텍스트와 EXP 바를 한 번에 관리합니다.
// 다른 스크립트 3개를 하나로 합친 것입니다.
// ============================================================

using UnityEngine;      // Unity 기본 기능 사용
using UnityEngine.UI;   // Image 컴포넌트 사용
using TMPro;            // TextMeshPro 텍스트 사용

/// <summary>
/// GameHudLevelExpPanel
/// 
/// [이 스크립트가 필요한 이유]
/// 레벨과 EXP 바를 한 번에 관리하기 위해 필요합니다.
/// 스크립트가 하나면 초보자가 이해하기 쉽습니다.
/// 
/// [어디에 붙이나요?]
/// - LevelExpPanel 오브젝트에 붙입니다.
/// 
/// [Inspector에서 연결할 것]
/// - levelText: 레벨 텍스트 컴포넌트
/// - expBarFill: EXP 바 채우기 Image 컴포넌트
/// - expValueText: EXP 수치 텍스트 컴포넌트
/// </summary>
public class GameHudLevelExpPanel : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================
    
    [Header("레벨 텍스트 설정")]
    [Tooltip("레벨을 표시할 텍스트 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private TextMeshProUGUI levelText;
    
    [Tooltip("게임 시작 시 기본 레벨입니다")]
    [SerializeField] private int startLevel = 1;
    
    [Tooltip("레벨 표시 형식입니다. {0}은 숫자로 바뀝니다")]
    [SerializeField] private string levelFormat = "Lv. {0}";
    
    [Header("EXP 바 설정")]
    [Tooltip("EXP 바 채우기 Image 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private Image expBarFill;
    
    [Tooltip("게임 시작 시 현재 EXP입니다")]
    [SerializeField] private int currentExp = 0;
    
    [Tooltip("레벨업에 필요한 최대 EXP입니다")]
    [SerializeField] private int maxExp = 50;

    [Header("EXP 수치 텍스트 설정")]
    [Tooltip("현재 EXP와 최대 EXP를 표시할 텍스트 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private TextMeshProUGUI expValueText;

    [Tooltip("EXP 수치 표시 형식입니다. {0}은 현재 EXP, {1}은 최대 EXP로 바뀝니다")]
    [SerializeField] private string expValueFormat = "{0} / {1}";

    // ============================================================
    // 현재 레벨을 저장하는 변수
    // ============================================================
    
    private int currentLevel = 1;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================
    
    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// Inspector에서 설정한 값을 화면에 적용합니다.
    /// </summary>
    private void Start()
    {
        // 레벨을 설정합니다
        SetLevel(startLevel);
        
        // EXP를 설정합니다
        SetExp(currentExp);
    }

    // ============================================================
    // 레벨 설정 함수
    // ============================================================
    
    /// <summary>
    /// SetLevel()은 레벨을 변경하는 함수입니다.
    /// 다른 스크립트에서 이 함수를 호출해서 레벨을 바꿀 수 있습니다.
    /// 
    /// [예시]
    /// SetLevel(5);  // 레벨을 5로 변경
    /// </summary>
    public void SetLevel(int newLevel)
    {
        // 레벨이 1보다 작으면 1로 보정합니다
        currentLevel = Mathf.Max(1, newLevel);
        
        // 화면에 레벨을 표시합니다
        UpdateLevelDisplay();
    }

    // ============================================================
    // EXP 설정 함수
    // ============================================================
    
    /// <summary>
    /// SetExp()는 EXP를 변경하는 함수입니다.
    /// 다른 스크립트에서 이 함수를 호출해서 EXP를 바꿀 수 있습니다.
    /// 
    /// [예시]
    /// SetExp(50);  // EXP를 50으로 변경
    /// </summary>
    public void SetExp(int newExp)
    {
        // EXP가 0보다 작으면 0으로 보정합니다
        currentExp = Mathf.Max(0, newExp);
        
        // EXP가 최대 EXP를 넘지 않게 제한합니다
        currentExp = Mathf.Min(currentExp, maxExp);
        
        // 화면에 EXP 바를 갱신합니다
        UpdateExpBarDisplay();

        // 화면에 EXP 수치 텍스트를 갱신합니다
        UpdateExpValueText();
    }

    // ============================================================
    // 화면 갱신 함수들
    // ============================================================
    
    /// <summary>
    /// UpdateLevelDisplay()는 화면의 레벨 텍스트를 갱신합니다.
    /// </summary>
    private void UpdateLevelDisplay()
    {
        if (levelText == null) return;
        levelText.text = string.Format(levelFormat, currentLevel);
    }
    
    /// <summary>
    /// UpdateExpBarDisplay()는 화면의 EXP 바를 갱신합니다.
    /// Fill Amount = 현재 EXP / 최대 EXP
    /// </summary>
    private void UpdateExpBarDisplay()
    {
        if (expBarFill == null) return;

        // 최대 EXP가 0 이하이면 나눗셈을 할 수 없으므로 0으로 표시합니다.
        // 나눗셈에서 0으로 나누면 오류가 날 수 있습니다.
        if (maxExp <= 0)
        {
            expBarFill.fillAmount = 0f;
            return;
        }
        
        // EXP를 0~1 사이 비율로 변환합니다
        float fillAmount = (float)currentExp / (float)maxExp;
        expBarFill.fillAmount = fillAmount;
    }

    /// <summary>
    /// UpdateExpValueText()는 화면의 EXP 수치 텍스트를 갱신합니다.
    /// 예: currentExp가 70이고 maxExp가 100이면 "70 / 100"으로 표시합니다.
    /// </summary>
    private void UpdateExpValueText()
    {
        // expValueText를 연결하지 않았으면 아무것도 하지 않습니다.
        // 이렇게 하면 EXP 수치 텍스트를 아직 안 만들어도 오류가 나지 않습니다.
        if (expValueText == null) return;

        // expValueFormat에 현재 EXP와 최대 EXP를 넣어서 화면 글자를 만듭니다.
        expValueText.text = string.Format(expValueFormat, currentExp, maxExp);
    }

    // ============================================================
    // Inspector에서 변경할 수 있는 함수들
    // ============================================================
    
    /// <summary>
    /// GetCurrentLevel()은 현재 레벨을 반환합니다.
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    /// <summary>
    /// GetCurrentExp()는 현재 EXP를 반환합니다.
    /// </summary>
    public int GetCurrentExp()
    {
        return currentExp;
    }
}
