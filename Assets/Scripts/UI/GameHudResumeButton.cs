// ============================================================
// GameHudResumeButton.cs
// ============================================================
// 이 스크립트가 하는 일:
// ESC 키를 누르면 일시정지 팝업이 표시되고 게임이 멈춥니다.
// "게임 재개" 버튼을 누르면 게임이 다시 시작됩니다.
// PausePanel을 Inspector에서 연결하지 않아도 자동으로 동작합니다.
// 기능 37: 게임 재개 버튼
// ============================================================

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameHudResumeButton
///
/// [이 스크립트가 필요한 이유]
/// 플레이어가 게임 중에 ESC 키를 누르면 게임을 일시정지하고 싶을 때 필요합니다.
/// 일시정지 화면에서 "게임 재개" 버튼을 누르면 게임을 다시 시작할 수 있습니다.
///
/// [어디에 붙이나요?]
/// - GameSceneUI 씬의 UI Canvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - pausePanel: 일시정지 팝업 배경 GameObject (선택, 없으면 자동으로 생성)
/// - resumeButton: 게임 재개 버튼의 Button 컴포넌트 (선택, 없으면 자동으로 생성)
/// </summary>
public class GameHudResumeButton : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("일시정지 팝업 연결")]
    [Tooltip("일시정지 시 표시할 팝업 배경 GameObject를 연결하세요. 비어 있으면 자동으로 생성합니다")]
    [SerializeField] private GameObject pausePanel;

    [Header("게임 재개 버튼 연결")]
    [Tooltip("게임 재개 버튼의 Button 컴포넌트를 연결하세요. 비어 있으면 자동으로 생성합니다")]
    [SerializeField] private Button resumeButton;

    [Header("자동 생성 설정")]
    [Tooltip("Inspector 연결이 없을 때 자동으로 오브젝트를 생성할지 정합니다")]
    [SerializeField] private bool autoCreateIfMissing = true;

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // 게임이 일시정지 상태인지 확인합니다.
    private bool isPaused;

    // 자동으로 생성한 오브젝트를 기억합니다.
    private GameObject createdPausePanel;
    private Button createdResumeButton;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 호출됩니다.
    /// Inspector 연결이 없으면 자동으로 오브젝트를 생성합니다.
    /// </summary>
    private void Awake()
    {
        // Inspector 연결이 없으면 자동으로 생성합니다.
        if (autoCreateIfMissing)
        {
            EnsurePausePanel();
            EnsureResumeButton();
        }

        SetupButtonListener();
    }

    /// <summary>
    /// Start()는 Play를 누른 뒤 한 번 호출됩니다.
    /// 처음에는 일시정지 팝업을 숨깁니다.
    /// </summary>
    private void Start()
    {
        // 게임 시작 시 일시정지 팝업을 숨깁니다.
        HidePausePanel();
    }

    /// <summary>
    /// Update()는 매 프레임마다 호출됩니다.
    /// ESC 키 입력을 감지합니다.
    /// </summary>
    private void Update()
    {
        // ESC 키를 누르면 일시정지를 토글합니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// OnEnable()은 오브젝트가 켜질 때 호출됩니다.
    /// 버튼 클릭 연결을 다시 확인합니다.
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
    /// TogglePause()는 일시정지 상태를 토글합니다.
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
    /// Time.timeScale을 0으로 설정하고 팝업을 표시합니다.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;

        // Time.timeScale을 0으로 설정하면 게임이 멈춥니다.
        // Time.timeScale = 시간 흐름 속도. 1이 normal, 0이 멈춤.
        Time.timeScale = 0f;

        ShowPausePanel();
    }

    /// <summary>
    /// ResumeGame()은 게임을 재개합니다.
    /// Time.timeScale을 1로 설정하고 팝업을 숨깁니다.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;

        // Time.timeScale을 1로 설정하면 게임이 정상 속도로 돌아옵니다.
        Time.timeScale = 1f;

        HidePausePanel();
    }

    /// <summary>
    /// ShowPausePanel()은 일시정지 팝업을 표시합니다.
    /// </summary>
    public void ShowPausePanel()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    /// <summary>
    /// HidePausePanel()은 일시정지 팝업을 숨깁니다.
    /// </summary>
    public void HidePausePanel()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    // ============================================================
    // 자동 생성 함수
    // ============================================================

    /// <summary>
    /// EnsurePausePanel()은 PausePanel이 없으면 Canvas 아래에 자동으로 생성합니다.
    /// Inspector에서 연결하지 않아도 동작합니다.
    /// </summary>
    private void EnsurePausePanel()
    {
        // Inspector에서 이미 연결했으면 아무것도 하지 않습니다.
        if (pausePanel != null)
        {
            return;
        }

        // 이미 자동으로 생성했으면 아무것도 하지 않습니다.
        if (createdPausePanel != null)
        {
            pausePanel = createdPausePanel;
            return;
        }

        // Canvas 아래에 PausePanel을 생성합니다.
        createdPausePanel = new GameObject("PausePanel");
        createdPausePanel.transform.SetParent(transform, false);

        // RectTransform을 화면 전체 크기로 설정합니다.
        var rectTransform = createdPausePanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // 반투명 검정 배경 이미지를 붙입니다.
        var image = createdPausePanel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.5f);

        // CanvasGroup을 붙여서 투명화 효과를 사용할 수 있게 합니다.
        createdPausePanel.AddComponent<CanvasGroup>();

        pausePanel = createdPausePanel;

        Debug.Log("PausePanel을 자동으로 생성했습니다.");
    }

    /// <summary>
    /// EnsureResumeButton()은 ResumeButton이 없으면 PausePanel 안에 자동으로 생성합니다.
    /// Inspector에서 연결하지 않아도 동작합니다.
    /// </summary>
    private void EnsureResumeButton()
    {
        // Inspector에서 이미 연결했으면 아무것도 하지 않습니다.
        if (resumeButton != null)
        {
            return;
        }

        // 이미 자동으로 생성했으면 아무것도 하지 않습니다.
        if (createdResumeButton != null)
        {
            resumeButton = createdResumeButton;
            return;
        }

        // PausePanel이 없으면 먼저 생성합니다.
        if (pausePanel == null)
        {
            EnsurePausePanel();
        }

        // PausePanel 안에 ResumeButton을 생성합니다.
        var buttonObj = new GameObject("ResumeButton");
        buttonObj.transform.SetParent(pausePanel.transform, false);

        // RectTransform을 설정합니다.
        var rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(220f, 70f);
        rectTransform.anchoredPosition = new Vector2(0f, -80f);

        // 버튼 배경 이미지를 붙입니다.
        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.69f, 0.31f, 1f);

        // Button 컴포넌트를 붙입니다.
        createdResumeButton = buttonObj.AddComponent<Button>();

        // 버튼 안에 텍스트를 생성합니다.
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        var textRectTransform = textObj.AddComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.offsetMin = Vector2.zero;
        textRectTransform.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "게임 재개";
        text.fontSize = 32f;
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;

        resumeButton = createdResumeButton;

        Debug.Log("ResumeButton을 자동으로 생성했습니다.");
    }

    // ============================================================
    // 버튼 연결 함수
    // ============================================================

    /// <summary>
    /// SetupButtonListener()는 버튼 클릭 이벤트를 연결합니다.
    /// 버튼을 누르면 ResumeGame()이 실행됩니다.
    /// </summary>
    private void SetupButtonListener()
    {
        if (resumeButton == null)
        {
            return;
        }

        // 버튼이 눌릴 수 있는 상태인지 확인합니다.
        resumeButton.interactable = true;

        // 중복 연결을 막기 위해 먼저 제거한 뒤 다시 연결합니다.
        resumeButton.onClick.RemoveListener(ResumeGame);
        resumeButton.onClick.AddListener(ResumeGame);
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

        resumeButton.onClick.RemoveListener(ResumeGame);
    }
}
