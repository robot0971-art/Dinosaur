// ============================================================
// GameHudTitleReturnButton.cs
// ============================================================
// 이 스크립트가 하는 일:
// ESC 키를 누르면 "타이틀로" 버튼을 표시합니다.
// ESC 키를 다시 누르면 버튼을 숨깁니다.
// 버튼을 누르면 Time.timeScale을 1로 되돌리고 타이틀 씬으로 이동합니다.
// GameHudResumeButton.cs와 함께 동작합니다.
// 기능 38: 타이틀로 돌아가기 버튼 (일시정지)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// ============================================================
// [이 스크립트가 필요한 이유]
// ============================================================
// 게임 중 ESC 키를 누르면 일시정지되는데,
// 이때 "타이틀로" 버튼이 보이면 플레이어가 타이틀 화면으로 돌아갈 수 있습니다.
// GameHudResumeButton.cs는 "게임 재개"만 담당하고,
// 이 스크립트는 "타이틀로 이동"만 담당합니다.
// ============================================================

// ============================================================
// [어디에 붙이나요?]
// ============================================================
// - GameSceneUI 씬의 UI Canvas 오브젝트에 붙입니다.
// - GameHudResumeButton.cs가 이미 붙어 있는 Canvas에 같이 붙입니다.
// ============================================================

// ============================================================
// [Inspector에서 연결할 것]
// ============================================================
// - titleButton: Canvas 자식으로 만든 "타이틀로" 버튼의 Button 컴포넌트
// - titleSceneName: 이동할 타이틀 씬 이름 (기본값: "TitleScene")
// ============================================================

public class GameHudTitleReturnButton : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("타이틀로 버튼 연결")]
    [Tooltip("Canvas 자식으로 만든 타이틀로 버튼의 Button 컴포넌트를 연결하세요")]
    [SerializeField] private Button titleButton;

    [Header("씬 이름 설정")]
    [Tooltip("이동할 타이틀 씬 이름을 입력하세요. 보통 TitleScene 입니다")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("에디터 테스트")]
    [Tooltip("Play 시작 시 타이틀로 버튼을 바로 보이게 할지 정합니다. 보통은 꺼둡니다")]
    [SerializeField] private bool showOnStart;

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // 버튼이 현재 보이는지 확인합니다.
    // true면 보이는 상태, false면 숨겨진 상태입니다.
    private bool isVisible;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 호출됩니다.
    /// 버튼 클릭 이벤트를 미리 연결합니다.
    /// </summary>
    private void Awake()
    {
        SetupButtonListener();
    }

    /// <summary>
    /// Start()는 Play를 누른 뒤 한 번 호출됩니다.
    /// 시작할 때 버튼을 숨깁니다. showOnStart가 켜져 있으면 테스트용으로 보여줍니다.
    /// </summary>
    private void Start()
    {
        isVisible = false;
        SetTitleButtonVisible(showOnStart);
    }

    /// <summary>
    /// Update()는 매 프레임마다 호출됩니다.
    /// ESC 키 입력을 감지합니다.
    /// GameHudResumeButton.cs도 같은 ESC 키를 감지합니다.
    /// 두 스크립트가 같은 ESC 키에 반응하므로 버튼이 동시에 보이거나 숨겨집니다.
    /// </summary>
    private void Update()
    {
        // Player Settings의 Active Input Handling이 Both일 때 사용할 수 있는 기본 입력 방식입니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleVisibility();
        }
    }

    /// <summary>
    /// OnEnable()은 오브젝트가 켜질 때 호출됩니다.
    /// 버튼 클릭 연결이 풀렸을 수 있으니 다시 확인합니다.
    /// </summary>
    private void OnEnable()
    {
        SetupButtonListener();
    }

    /// <summary>
    /// OnDisable()은 오브젝트가 꺼질 때 호출됩니다.
    /// 버튼 클릭 연결을 정리합니다.
    /// </summary>
    private void OnDisable()
    {
        RemoveButtonListener();
    }

    // ============================================================
    // 공개 함수
    // ============================================================

    /// <summary>
    /// ToggleVisibility()는 현재 상태에 따라 버튼을 보이거나 숨깁니다.
    /// ESC 키를 누를 때 호출됩니다.
    /// </summary>
    public void ToggleVisibility()
    {
        if (isVisible)
        {
            HideTitleButton();
        }
        else
        {
            ShowTitleButton();
        }
    }

    /// <summary>
    /// ShowTitleButton()은 타이틀로 버튼을 표시합니다.
    /// ESC 키를 눌러서 일시정지할 때 같이 호출됩니다.
    /// </summary>
    public void ShowTitleButton()
    {
        isVisible = true;
        SetTitleButtonVisible(true);
    }

    /// <summary>
    /// HideTitleButton()은 타이틀로 버튼을 숨깁니다.
    /// ESC 키를 다시 눌러서 재개할 때 같이 호출됩니다.
    /// </summary>
    public void HideTitleButton()
    {
        isVisible = false;
        SetTitleButtonVisible(false);
    }

    /// <summary>
    /// ReturnToTitle()은 타이틀 씬으로 이동합니다.
    /// 타이틀로 버튼을 클릭할 때 호출됩니다.
    /// Time.timeScale을 1로 되돌린 뒤 씬을 이동합니다.
    /// </summary>
    public void ReturnToTitle()
    {
        // Time.timeScale이 0이면 씬 전환이 제대로 안 될 수 있습니다.
        // 그래서 반드시 1로 되돌린 뒤 씬을 이동합니다.
        Time.timeScale = 1f;

        // 지정한 타이틀 씬 이름이 비어있으면 경고를 표시합니다.
        if (string.IsNullOrEmpty(titleSceneName))
        {
            Debug.LogWarning("[GameHudTitleReturnButton] 타이틀 씬 이름이 비어있습니다. Inspector에서 Title Scene Name을 입력하세요.");
            return;
        }

        // 타이틀 씬으로 이동합니다.
        SceneManager.LoadScene(titleSceneName);
    }

    // ============================================================
    // 버튼 표시 함수
    // ============================================================

    /// <summary>
    /// SetTitleButtonVisible()은 타이틀로 버튼을 보이거나 숨깁니다.
    /// true면 보이고, false면 숨깁니다.
    /// </summary>
    private void SetTitleButtonVisible(bool visible)
    {
        if (titleButton != null)
        {
            titleButton.gameObject.SetActive(visible);
        }
    }

    // ============================================================
    // 버튼 연결 함수
    // ============================================================

    /// <summary>
    /// SetupButtonListener()는 버튼 클릭 이벤트를 연결합니다.
    /// 버튼을 누르면 ReturnToTitle()이 실행됩니다.
    /// </summary>
    private void SetupButtonListener()
    {
        if (titleButton == null)
        {
            return;
        }

        titleButton.interactable = true;

        if (titleButton.targetGraphic != null)
        {
            titleButton.targetGraphic.raycastTarget = true;
        }

        titleButton.onClick.RemoveListener(ReturnToTitle);
        titleButton.onClick.AddListener(ReturnToTitle);
    }

    /// <summary>
    /// RemoveButtonListener()는 버튼 클릭 이벤트 연결을 해제합니다.
    /// </summary>
    private void RemoveButtonListener()
    {
        if (titleButton == null)
        {
            return;
        }

        titleButton.onClick.RemoveListener(ReturnToTitle);
    }
}
