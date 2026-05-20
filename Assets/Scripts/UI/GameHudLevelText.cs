// ============================================================
// GameHudLevelText.cs
// ============================================================
// 이 스크립트가 하는 일:
// 게임 씬 왼쪽 위에 플레이어 레벨을 표시합니다.
// "Lv. 1" 같은 형식으로 화면에 보여줍니다.
// 나중에 레벨업하면 숫자를 바꿀 수 있습니다.
// ============================================================

using UnityEngine;      // Unity 기본 기능 사용
using TMPro;            // TextMeshPro 텍스트 사용

/// <summary>
/// GameHudLevelText
/// 
/// [이 스크립트가 필요한 이유]
/// 플레이어가 현재 몇 레벨인지 화면에 보여주기 위해 필요합니다.
/// 레벨을 알아야 플레이어가 성장했는지 알 수 있습니다.
/// 
/// [어디에 붙이나요?]
/// - GameHudCanvas 오브젝트에 붙입니다.
/// 
/// [Inspector에서 연결할 것]
/// - levelText: 레벨을 표시할 TextMeshProUGUI 컴포넌트
/// </summary>
public class GameHudLevelText : MonoBehaviour
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
    
    // ============================================================
    // 현재 레벨을 저장하는 변수
    // ============================================================
    // private은 "이 스크립트 안에서만 쓰는 변수"라는 뜻입니다.
    // currentLevel은 현재 플레이어 레벨을 기억하는 변수입니다.
    
    private int currentLevel = 1;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================
    
    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// Awake()보다 조금 늦게 실행됩니다.
    /// 
    /// [언제 호출되나요?]
    /// - 게임 씬이 로드되면 자동으로 호출됩니다.
    /// - Inspector에서 설정한 값을 화면에 적용합니다.
    /// </summary>
    private void Start()
    {
        // 레벨 텍스트를 설정하는 함수를 호출합니다
        SetupLevelText();
        
        // 시작 레벨로 화면을 갱신합니다
        SetLevel(startLevel);
    }

    // ============================================================
    // 레벨 텍스트 설정 함수
    // ============================================================
    
    /// <summary>
    /// SetupLevelText()는 레벨 텍스트 컴포넌트를 설정하는 함수입니다.
    /// Inspector에서 설정한 값이 올바른지 확인합니다.
    /// 
    /// [실행 흐름]
    /// 1. levelText가 연결되었는지 확인합니다.
    /// 2. 연결되지 않았으면 경고 메시지를 표시합니다.
    /// </summary>
    private void SetupLevelText()
    {
        // levelText가 연결되지 않았으면 함수를 끝냅니다
        if (levelText == null)
        {
            Debug.LogWarning("[GameHudLevelText] Level Text가 연결되지 않았습니다.");
            return;
        }
    }

    // ============================================================
    // 레벨 설정 함수 (다른 스크립트에서 호출 가능)
    // ============================================================
    
    /// <summary>
    /// SetLevel()은 레벨을 변경하는 함수입니다.
    /// 다른 스크립트에서 이 함수를 호출해서 레벨을 바꿀 수 있습니다.
    /// 
    /// [언제 호출되나요?]
    /// - 플레이어가 레벨업할 때 GrowthSystem이 호출합니다.
    /// - 게임 시작 시 Start()에서 호출합니다.
    /// 
    /// [매개변수]
    /// - newLevel: 새로운 레벨 값
    /// 
    /// [예시]
    /// SetLevel(5);  // 레벨을 5로 변경
    /// </summary>
    public void SetLevel(int newLevel)
    {
        // 레벨이 1보다 작으면 1로 보정합니다
        // Mathf.Max는 "둘 중 큰 값을 고른다"는 뜻입니다
        // newLevel이 0이나 음수여도 최소 1이 됩니다
        currentLevel = Mathf.Max(1, newLevel);
        
        // 화면에 레벨을 표시하는 함수를 호출합니다
        UpdateLevelDisplay();
    }

    // ============================================================
    // 화면 갱신 함수
    // ============================================================
    
    /// <summary>
    /// UpdateLevelDisplay()는 화면의 레벨 텍스트를 갱신하는 함수입니다.
    /// currentLevel 값이 바뀔 때마다 이 함수를 호출합니다.
    /// 
    /// [실행 흐름]
    /// 1. levelText가 연결되었는지 확인합니다.
    /// 2. levelFormat에 현재 레벨을 넣습니다.
    /// 3. 화면에 표시합니다.
    /// </summary>
    private void UpdateLevelDisplay()
    {
        // levelText가 연결되지 않았으면 함수를 끝냅니다
        if (levelText == null)
        {
            return;
        }
        
        // levelFormat에 현재 레벨을 넣어서 텍스트를 만듭니다
        // string.Format는 "형식에 값을 넣어서 글자를 만든다"는 뜻입니다
        // 예: "Lv. {0}" 에서 {0}을 currentLevel로 바꿉니다
        levelText.text = string.Format(levelFormat, currentLevel);
    }

    // ============================================================
    // Inspector에서 변경할 수 있는 함수들
    // ============================================================
    
    /// <summary>
    /// GetCurrentLevel()은 현재 레벨을 반환하는 함수입니다.
    /// 다른 스크립트에서 현재 레벨을 확인할 때 사용합니다.
    /// 
    /// [사용 예시]
    /// int level = hudLevelText.GetCurrentLevel();
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}
