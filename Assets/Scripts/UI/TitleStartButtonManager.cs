// ============================================================
// TitleStartButtonManager.cs
// ============================================================
// 이 스크립트가 하는 일:
// 타이틀 화면에서 "게임 시작" 버튼을 관리합니다.
// 버튼을 누르면 게임 씬으로 전환됩니다.
// ============================================================

using UnityEngine;           // Unity의 기본 기능을 사용하기 위해 필요
using UnityEngine.UI;        // Button, Image 등 UI 기능을 사용하기 위해 필요
using UnityEngine.SceneManagement;  // 씬 전환 기능을 사용하기 위해 필요
using TMPro;                 // TextMeshPro 텍스트를 사용하기 위해 필요

/// <summary>
/// TitleStartButtonManager
/// 
/// [이 스크립트가 필요한 이유]
/// 타이틀 화면에 "게임 시작" 버튼을 표시하기 위해 필요합니다.
/// 버튼을 누르면 게임 씬으로 전환됩니다.
/// 
/// [어디에 붙이나요?]
/// - TitleCanvas 오브젝트에 붙입니다.
/// - TitleUIManager.cs와 같은 오브젝트에 붙입니다.
/// 
/// [Inspector에서 연결할 것]
/// - startButton: 게임 시작 버튼 컴포넌트를 연결합니다.
/// - buttonText: 버튼 위에 표시될 텍스트를 연결합니다.
/// </summary>
public class TitleStartButtonManager : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================
    // [Header]는 Inspector에서 그룹을 나누는 제목입니다
    // [Tooltip]은 변수 위에 마우스를 올렸을 때 나오는 설명입니다
    
    [Header("게임 시작 버튼 설정")]
    [Tooltip("게임 시작 버튼 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private Button startButton;
    
    [Tooltip("버튼 위에 표시될 텍스트 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private TextMeshProUGUI buttonText;
    
    [Tooltip("버튼에 표시할 텍스트 내용입니다")]
    [SerializeField] private string buttonLabel = "게임 시작";
    
    [Tooltip("전환할 게임 씬의 이름입니다")]
    [SerializeField] private string gameSceneName = "GameScene";
    
    [Header("버튼 색상 설정")]
    [Tooltip("버튼 기본 색상입니다")]
    [SerializeField] private Color normalColor = new Color(0.3f, 0.69f, 0.31f, 1f); // #4CAF50 (초록색)
    
    [Tooltip("버튼에 마우스를 올렸을 때 색상입니다")]
    [SerializeField] private Color hoverColor = new Color(0.4f, 0.73f, 0.42f, 1f); // #66BB6A (밝은 초록색)
    
    [Tooltip("버튼을 누르고 있을 때 색상입니다")]
    [SerializeField] private Color pressedColor = new Color(0.2f, 0.59f, 0.21f, 1f); // #388E3C (어두운 초록색)

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================
    
    /// <summary>
    /// Awake()는 오브젝트가 생성될 때 한 번 호출됩니다.
    /// Start()보다 먼저 실행됩니다.
    /// 
    /// [언제 호출되나요?]
    /// - 씬이 시작될 때 자동으로 호출됩니다.
    /// - Inspector에서 설정한 값을 적용하는 데 사용합니다.
    /// </summary>
    private void Awake()
    {
        // 버튼을 설정하는 함수를 호출합니다
        SetupStartButton();
    }

    // ============================================================
    // 버튼 설정 함수
    // ============================================================
    
    /// <summary>
    /// SetupStartButton()는 게임 시작 버튼을 설정하는 함수입니다.
    /// Inspector에서 설정한 색상과 텍스트를 적용합니다.
    /// 버튼을 누르면 게임 씬으로 전환됩니다.
    /// 
    /// [실행 흐름]
    /// 1. startButton이 연결되었는지 확인합니다.
    /// 2. 버튼 색상을 설정합니다.
    /// 3. 버튼 텍스트를 설정합니다.
    /// 4. 버튼 클릭 이벤트를 연결합니다.
    /// </summary>
    private void SetupStartButton()
    {
        // startButton이 연결되지 않았으면 함수를 끝냅니다
        // null은 "비어있다"는 뜻입니다
        if (startButton == null)
        {
            // 경고 메시지를 표시합니다
            Debug.LogWarning("[TitleStartButtonManager] Start Button이 연결되지 않았습니다.");
            return; // 함수를 여기서 끝냅니다
        }

        // 버튼 색상을 설정합니다
        // ColorBlock은 버튼의 여러 상태(기본, 호버, 누름 등)의 색상을 관리합니다
        ColorBlock colors = startButton.colors;
        colors.normalColor = normalColor;      // 기본 색상
        colors.highlightedColor = hoverColor;  // 마우스를 올렸을 때 색상
        colors.pressedColor = pressedColor;    // 버튼을 누르고 있을 때 색상
        startButton.colors = colors;           // 설정한 색상을 적용합니다
        
        // 버튼 텍스트를 설정합니다
        if (buttonText != null)
        {
            buttonText.text = buttonLabel; // 텍스트 내용을 설정합니다
        }
        
        // 버튼 클릭 이벤트를 연결합니다
        // AddListener는 "이벤트를 연결한다"는 뜻입니다
        // OnStartButtonClicked 함수가 버튼을 누를 때 호출됩니다
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    // ============================================================
    // 버튼 클릭 이벤트 함수
    // ============================================================
    
    /// <summary>
    /// OnStartButtonClicked()는 버튼을 누를 때 호출되는 함수입니다.
    /// 게임 씬으로 전환하는 역할을 합니다.
    /// 
    /// [언제 호출되나요?]
    /// - 플레이어가 "게임 시작" 버튼을 클릭할 때 자동으로 호출됩니다.
    /// </summary>
    private void OnStartButtonClicked()
    {
        // 콘솔에 메시지를 표시합니다 (디버깅용)
        Debug.Log("[TitleStartButtonManager] 게임 시작 버튼이 클릭되었습니다.");
        
        // 게임 씬으로 전환합니다
        LoadGameScene();
    }

    // ============================================================
    // 씬 전환 함수
    // ============================================================
    
    /// <summary>
    /// LoadGameScene()는 게임 씬을 로드하는 함수입니다.
    /// SceneManager.LoadScene를 사용하여 씬을 전환합니다.
    /// 
    /// [언제 호출되나요?]
    /// - OnStartButtonClicked() 함수 안에서 호출됩니다.
    /// 
    /// [주의할 점]
    /// - 게임 씬의 이름이 정확해야 합니다.
    /// - 게임 씬이 Build Settings에 추가되어 있어야 합니다.
    /// </summary>
    private void LoadGameScene()
    {
        // 씬 이름이 비어있으면 경고를 표시합니다
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[TitleStartButtonManager] 게임 씬 이름이 설정되지 않았습니다.");
            return; // 함수를 여기서 끝냅니다
        }
        
        // 콘솔에 메시지를 표시합니다 (디버깅용)
        Debug.Log("[TitleStartButtonManager] 게임 씬을 로드합니다: " + gameSceneName);
        
        // 게임 씬을 로드합니다
        // LoadScene은 "씬을 불러온다"는 뜻입니다
        SceneManager.LoadScene(gameSceneName);
    }

    // ============================================================
    // Inspector에서 변경할 수 있는 함수들
    // ============================================================
    
    /// <summary>
    /// ChangeButtonText()는 버튼 텍스트를 변경하는 함수입니다.
    /// 
    /// [언제 호출되나요?]
    /// - 다른 스크립트에서 버튼 텍스트를 바꾸고 싶을 때 호출합니다.
    /// 
    /// [매개변수]
    /// - newText: 새로운 버튼 텍스트
    /// </summary>
    public void ChangeButtonText(string newText)
    {
        // buttonText가 없으면 함수를 끝냅니다
        if (buttonText == null) return;
        
        // 버튼 텍스트를 변경합니다
        buttonText.text = newText;
    }

    /// <summary>
    /// ChangeButtonColor()는 버튼 색상을 변경하는 함수입니다.
    /// 
    /// [언제 호출되나요?]
    /// - 다른 스크립트에서 버튼 색상을 바꾸고 싶을 때 호출합니다.
    /// 
    /// [매개변수]
    /// - newColor: 새로운 버튼 색상
    /// </summary>
    public void ChangeButtonColor(Color newColor)
    {
        // startButton이 없으면 함수를 끝냅니다
        if (startButton == null) return;
        
        // 버튼 색상을 변경합니다
        ColorBlock colors = startButton.colors;
        colors.normalColor = newColor;
        startButton.colors = colors;
    }

    /// <summary>
    /// ChangeGameSceneName()는 전환할 게임 씬 이름을 변경하는 함수입니다.
    /// 
    /// [언제 호출되나요?]
    /// - 다른 스크립트에서 전환할 씬을 바꾸고 싶을 때 호출합니다.
    /// 
    /// [매개변수]
    /// - newSceneName: 새로운 게임 씬 이름
    /// </summary>
    public void ChangeGameSceneName(string newSceneName)
    {
        // 게임 씬 이름을 변경합니다
        gameSceneName = newSceneName;
    }
}
