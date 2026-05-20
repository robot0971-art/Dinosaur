// ============================================================
// GameHudExpBarFill.cs
// ============================================================
// 이 스크립트가 하는 일:
// EXP 바 안에 초록색 채우기를 표시합니다.
// 현재 EXP 값에 따라 초록색이 왼쪽에서 오른쪽으로 채워집니다.
// EXP가 0이면 비어있고, 100이면 가득 참니다.
// ============================================================

using UnityEngine;      // Unity 기본 기능 사용
using UnityEngine.UI;   // Image 컴포넌트 사용

/// <summary>
/// GameHudExpBarFill
/// 
/// [이 스크립트가 필요한 이유]
/// 플레이어가 얼마나 성장했는지 시각적으로 보여주기 위해 필요합니다.
/// EXP 바가 차오르면 플레이어는 "조금만 더 먹으면 레벨업이야!"라고 알 수 있습니다.
/// 
/// [어디에 붙이나요?]
/// - GameHudCanvas 오브젝트에 붙입니다.
/// 
/// [Inspector에서 연결할 것]
/// - fillImage: ExpBarFill의 Image 컴포넌트
/// </summary>
public class GameHudExpBarFill : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================
    
    [Header("EXP 바 채우기 설정")]
    [Tooltip("EXP 바 채우기 Image 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private Image fillImage;
    
    [Tooltip("채우기 색상입니다. 초록색이 기본값입니다")]
    [SerializeField] private Color fillColor = new Color(0f, 1f, 0.27f, 1f); // #00FF44
    
    [Header("EXP 수치 설정")]
    [Tooltip("게임 시작 시 현재 EXP입니다")]
    [SerializeField] private int currentExp = 70;
    
    [Tooltip("레벨업에 필요한 최대 EXP입니다")]
    [SerializeField] private int maxExp = 100;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================
    
    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// Inspector에서 설정한 값을 화면에 적용합니다.
    /// </summary>
    private void Start()
    {
        // EXP 바 채우기를 설정하는 함수를 호출합니다
        SetupExpBarFill();
        
        // 현재 EXP로 화면을 갱신합니다
        SetExp(currentExp);
    }

    // ============================================================
    // EXP 바 채우기 설정 함수
    // ============================================================
    
    /// <summary>
    /// SetupExpBarFill()는 EXP 바 채우기를 설정하는 함수입니다.
    /// Inspector에서 설정한 색상을 적용합니다.
    /// 
    /// [실행 흐름]
    /// 1. fillImage가 연결되었는지 확인합니다.
    /// 2. 채우기 색상을 설정합니다.
    /// </summary>
    private void SetupExpBarFill()
    {
        // fillImage가 연결되지 않았으면 함수를 끝냅니다
        if (fillImage == null)
        {
            Debug.LogWarning("[GameHudExpBarFill] Fill Image가 연결되지 않았습니다.");
            return;
        }
        
        // 채우기 색상을 설정합니다
        fillImage.color = fillColor;
    }

    // ============================================================
    // EXP 설정 함수 (다른 스크립트에서 호출 가능)
    // ============================================================
    
    /// <summary>
    /// SetExp()는 EXP를 변경하는 함수입니다.
    /// 다른 스크립트에서 이 함수를 호출해서 EXP를 바꿀 수 있습니다.
    /// 
    /// [언제 호출되나요?]
    /// - 플레이어가 공룡을 먹었을 때 GrowthSystem이 호출합니다.
    /// - 게임 시작 시 Start()에서 호출합니다.
    /// 
    /// [매개변수]
    /// - newExp: 새로운 EXP 값 (0~100)
    /// 
    /// [예시]
    /// SetExp(50);  // EXP를 50으로 변경
    /// </summary>
    public void SetExp(int newExp)
    {
        // EXP가 0보다 작으면 0으로 보정합니다
        currentExp = Mathf.Max(0, newExp);
        
        // EXP가 최대 EXP를 넘지 않게 제한합니다
        // Mathf.Min은 "둘 중 작은 값을 고른다"는 뜻입니다
        currentExp = Mathf.Min(currentExp, maxExp);
        
        // 화면에 EXP 바를 갱신하는 함수를 호출합니다
        UpdateExpBarDisplay();
    }

    // ============================================================
    // 화면 갱신 함수
    // ============================================================
    
    /// <summary>
    /// UpdateExpBarDisplay()는 화면의 EXP 바를 갱신하는 함수입니다.
    /// currentExp 값을 0~1 사이 비율로 변환해서 Fill Amount에 적용합니다.
    /// 
    /// [계산 방법]
    /// Fill Amount = 현재 EXP / 최대 EXP
    /// 예: 70 / 100 = 0.7 (70% 채워짐)
    /// </summary>
    private void UpdateExpBarDisplay()
    {
        // fillImage가 연결되지 않았으면 함수를 끝냅니다
        if (fillImage == null)
        {
            return;
        }
        
        // EXP를 0~1 사이 비율로 변환합니다
        // (float)은 "소수점으로 바꾼다"는 뜻입니다
        // 정수끼리 나누면 소수점이 사라지므로 (float)을 붙입니다
        float fillAmount = (float)currentExp / (float)maxExp;
        
        // Fill Amount를 설정합니다
        fillImage.fillAmount = fillAmount;
    }

    // ============================================================
    // Inspector에서 변경할 수 있는 함수들
    // ============================================================
    
    /// <summary>
    /// GetCurrentExp()는 현재 EXP를 반환하는 함수입니다.
    /// 다른 스크립트에서 현재 EXP를 확인할 때 사용합니다.
    /// </summary>
    public int GetCurrentExp()
    {
        return currentExp;
    }
    
    /// <summary>
    /// GetMaxExp()는 최대 EXP를 반환하는 함수입니다.
    /// </summary>
    public int GetMaxExp()
    {
        return maxExp;
    }
    
    /// <summary>
    /// GetExpRatio()는 현재 EXP 비율을 반환하는 함수입니다.
    /// 0~1 사이 값을 반환합니다.
    /// </summary>
    public float GetExpRatio()
    {
        if (maxExp <= 0) return 0f;
        return (float)currentExp / (float)maxExp;
    }
}
