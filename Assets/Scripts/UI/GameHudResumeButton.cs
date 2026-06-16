// ============================================================
// GameHudResumeButton.cs
// ============================================================
// 이 스크립트가 하는 일:
// ESC 키를 누르면 Canvas 자식으로 만든 "게임 재개" 버튼을 표시하고 게임을 멈춥니다.
// "게임 재개" 버튼을 누르면 버튼을 숨기고 게임을 다시 시작합니다.
// PausePanel 없이 Canvas와 ResumeButton만 사용합니다.
// 기능 37: 게임 재개 버튼
// ============================================================

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameHudResumeButton
///
/// [이 스크립트가 필요한 이유]
/// 게임 중 ESC 키를 눌렀을 때 잠시 멈추고,
/// 버튼을 눌러 다시 게임을 재개하기 위해 필요합니다.
///
/// [어디에 붙이나요?]
/// - GameSceneUI 씬의 UI Canvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - resumeButton: Canvas 자식으로 만든 게임 재개 버튼의 Button 컴포넌트
/// </summary>
public class GameHudResumeButton : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("게임 재개 버튼 연결")]
    [Tooltip("Canvas 자식으로 만든 게임 재개 버튼의 Button 컴포넌트를 연결하세요")]
    [SerializeField] private Button resumeButton;

    [Header("에디터 테스트")]
    [Tooltip("Play 시작 시 게임 재개 버튼을 바로 보이게 할지 정합니다. 보통은 꺼둡니다")]
    [SerializeField] private bool showOnStart;

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // 게임이 일시정지 상태인지 확인합니다.
    // true면 멈춘 상태, false면 정상 플레이 상태입니다.
    private bool isPaused;

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
        isPaused = false;
        Time.timeScale = 1f;
        SetResumeButtonVisible(showOnStart);
    }

    /// <summary>
    /// Update()는 매 프레임마다 호출됩니다.
    /// ESC 키 입력을 감지합니다.
    /// </summary>
    private void Update()
    {
        // Player Settings의 Active Input Handling이 Both일 때 사용할 수 있는 기본 입력 방식입니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
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
    /// TogglePause()는 현재 상태에 따라 일시정지 또는 재개를 실행합니다.
    /// ESC 키를 누를 때 호출됩니다.
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// PauseGame()은 게임을 일시정지합니다.
    /// Time.timeScale을 0으로 만들고 게임 재개 버튼을 표시합니다.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetResumeButtonVisible(true);
    }

    /// <summary>
    /// ResumeGame()은 게임을 재개합니다.
    /// Time.timeScale을 1로 되돌리고 게임 재개 버튼을 숨깁니다.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetResumeButtonVisible(false);
    }

    // ============================================================
    // 버튼 표시 함수
    // ============================================================

    /// <summary>
    /// SetResumeButtonVisible()은 게임 재개 버튼을 보이거나 숨깁니다.
    /// true면 보이고, false면 숨깁니다.
    /// </summary>
    private void SetResumeButtonVisible(bool visible)
    {
        if (resumeButton != null)
        {
            resumeButton.gameObject.SetActive(visible);
        }
    }

    // ============================================================
    // 버튼 연결 함수
    // ============================================================

    /// <summary>
    /// SetupButtonListener()는 버튼 클릭 이벤트를 연결합니다.
    /// 버튼을 누르면 TogglePause()가 실행됩니다.
    /// </summary>
    private void SetupButtonListener()
    {
        if (resumeButton == null)
        {
            return;
        }

        resumeButton.interactable = true;

        if (resumeButton.targetGraphic != null)
        {
            resumeButton.targetGraphic.raycastTarget = true;
        }

        resumeButton.onClick.RemoveListener(TogglePause);
        resumeButton.onClick.AddListener(TogglePause);
    }

    /// <summary>
    /// RemoveButtonListener()는 버튼 클릭 이벤트 연결을 해제합니다.
    /// </summary>
    private void RemoveButtonListener()
    {
        if (resumeButton == null)
        {
            return;
        }

        resumeButton.onClick.RemoveListener(TogglePause);
    }
}
