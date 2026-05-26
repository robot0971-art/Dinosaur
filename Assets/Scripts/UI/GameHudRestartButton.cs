// ============================================================
// GameHudRestartButton.cs
// ============================================================
// 이 스크립트가 하는 일:
// 게임오버가 발생했을 때 "다시 시작" 버튼을 표시합니다.
// 버튼을 누르면 현재 씬이 다시 로드되어 게임을 처음부터 다시 시작합니다.
// 기능 29: 다시 시작 버튼
// ============================================================

using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// GameHudRestartButton
///
/// [이 스크립트가 필요한 이유]
/// 게임오버가 발생했을 때 플레이어에게 "게임을 다시 시작할 수 있다"는 것을
/// 버튼으로 알려주기 위해 필요합니다.
///
/// [어디에 붙이나요?]
/// - GameOverCanvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - restartButton: 다시 시작 버튼 Button 컴포넌트
/// </summary>
public class GameHudRestartButton : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("다시 시작 버튼 연결")]
    [Tooltip("게임오버 시 표시할 다시 시작 버튼의 Button 컴포넌트를 연결하세요")]
    [SerializeField] private Button restartButton;

    [Header("다시 시작할 씬 이름")]
    [Tooltip("다시 시작 버튼을 눌렀을 때 이동할 씬 이름입니다. 예: GameScene")]
    [SerializeField] private string restartSceneName = "GameScene";

    [Header("에디터 테스트")]
    [Tooltip("Play를 누르지 않아도 버튼이 보이는지 확인할 수 있습니다")]
    [SerializeField] private bool showOnStart;

    // ============================================================
    // DI로 주입받을 변수들
    // ============================================================

    // VContainer가 자동으로 넣어주는 이벤트 버스입니다.
    // GameLifetimeScope가 없는 씬에서는 null일 수 있습니다.
    // null이면 showOnStart 옵션으로만 테스트할 수 있습니다.
    private GameEventBus eventBus;
    private bool subscribedToEvents;

    // ============================================================
    // DI 주입 함수
    // ============================================================

    /// <summary>
    /// Construct()는 VContainer가 자동으로 호출해주는 함수입니다.
    /// GameLifetimeScope가 있는 씬에서만 호출됩니다.
    /// </summary>
    [Inject]
    public void Construct(GameEventBus eventBus)
    {
        this.eventBus = eventBus;
        SubscribeToEvents();

        SetupButtonListener();
    }

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 호출됩니다.
    /// 버튼 클릭 연결을 최대한 빨리 준비합니다.
    /// </summary>
    private void Awake()
    {
        SetupButtonListener();
    }

    /// <summary>
    /// OnEnable()는 오브젝트가 활성화될 때 호출됩니다.
    /// </summary>
    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            ApplyEditModePreview();
            return;
        }

        SubscribeToEvents();
    }

    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// </summary>
    private void Start()
    {
        if (!Application.isPlaying)
        {
            ApplyEditModePreview();
            return;
        }

        // 기본은 숨겨져 있어야 합니다.
        // showOnStart가 켜져 있으면 테스트용으로 보여줍니다.
        SetVisible(showOnStart);
    }

    /// <summary>
    /// OnDisable()는 오브젝트가 비활성화될 때 호출됩니다.
    /// 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UnsubscribeFromEvents();
        RemoveButtonListener();
    }

    /// <summary>
    /// OnValidate()는 Inspector에서 값이 바뀔 때 호출됩니다.
    /// </summary>
    private void OnValidate()
    {
        ApplyEditModePreview();
    }

    // ============================================================
    // 공개 함수 (다른 스크립트가 호출할 수 있습니다)
    // ============================================================

    /// <summary>
    /// ShowRestartButton()는 다시 시작 버튼을 표시합니다.
    /// </summary>
    public void ShowRestartButton()
    {
        SetVisible(true);
    }

    /// <summary>
    /// HideRestartButton()는 다시 시작 버튼을 숨깁니다.
    /// </summary>
    public void HideRestartButton()
    {
        SetVisible(false);
    }

    /// <summary>
    /// SetVisible()는 버튼을 보이거나 숨깁니다.
    /// visible이 true면 보이고, false면 숨깁니다.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(visible);
        }
    }

    // ============================================================
    // 버튼 설정 함수
    // ============================================================

    /// <summary>
    /// SetupButtonListener()는 버튼 클릭 이벤트를 연결합니다.
    /// 버튼을 누르면 OnRestartButtonClicked() 함수가 호출됩니다.
    /// </summary>
    private void SetupButtonListener()
    {
        if (restartButton == null)
        {
            Debug.LogWarning("Restart Button이 연결되지 않았습니다. GameOverCanvas의 GameHudRestartButton에서 RestartButton을 연결하세요.");
            return;
        }

        restartButton.interactable = true;

        if (restartButton.targetGraphic != null)
        {
            restartButton.targetGraphic.raycastTarget = true;
        }

        // 중복 연결을 막기 위해 먼저 한 번 제거한 뒤 다시 연결합니다.
        restartButton.onClick.RemoveListener(RestartGame);

        // 버튼을 누르면 RestartGame() 함수가 호출되도록 연결합니다.
        restartButton.onClick.AddListener(RestartGame);
    }

    /// <summary>
    /// RemoveButtonListener()는 버튼 클릭 이벤트 연결을 해제합니다.
    /// 오브젝트가 꺼질 때 중복 연결이나 오류를 막기 위해 사용합니다.
    /// </summary>
    private void RemoveButtonListener()
    {
        if (restartButton == null)
        {
            return;
        }

        restartButton.onClick.RemoveListener(RestartGame);
    }

    /// <summary>
    /// RestartGame()은 다시 시작 버튼을 누르면 호출됩니다.
    /// public 함수라서 Button의 OnClick에 직접 연결해서 테스트할 수도 있습니다.
    /// 현재 씬을 다시 로드합니다.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log($"다시 시작 버튼 클릭됨. 이동할 씬: {restartSceneName}");

        // 현재 씬을 다시 로드합니다.
        RestartCurrentScene();
    }

    /// <summary>
    /// RestartCurrentScene()는 Inspector에 적은 씬으로 이동합니다.
    /// GameOver 씬에서 게임 씬으로 돌아갈 때 사용합니다.
    /// </summary>
    private void RestartCurrentScene()
    {
        if (string.IsNullOrWhiteSpace(restartSceneName))
        {
            Debug.LogWarning("다시 시작할 씬 이름이 비어 있습니다. GameHudRestartButton의 Restart Scene Name을 확인하세요.");
            return;
        }

        // 게임오버 때 시간이 멈춰 있었을 수 있으니 다시 정상 속도로 돌립니다.
        Time.timeScale = 1f;

        // Inspector에 적은 씬 이름으로 이동합니다.
        SceneManager.LoadScene(restartSceneName, LoadSceneMode.Single);
    }

    // ============================================================
    // 이벤트 구독 함수
    // ============================================================

    /// <summary>
    /// SubscribeToEvents()는 GameEventBus의 이벤트를 구독합니다.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (eventBus == null || subscribedToEvents)
        {
            return;
        }

        eventBus.GameStateChanged += OnGameStateChanged;
        subscribedToEvents = true;
    }

    /// <summary>
    /// UnsubscribeFromEvents()는 이벤트 구독을 해제합니다.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (eventBus == null || !subscribedToEvents)
        {
            return;
        }

        eventBus.GameStateChanged -= OnGameStateChanged;
        subscribedToEvents = false;
    }

    // ============================================================
    // 이벤트 핸들러
    // ============================================================

    /// <summary>
    /// OnGameStateChanged()는 게임 상태가 바뀔 때 호출됩니다.
    /// GameState가 GameOver이면 버튼을 표시합니다.
    /// </summary>
    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            ShowRestartButton();
        }
        else
        {
            HideRestartButton();
        }
    }

    // ============================================================
    // 에디터 미리보기
    // ============================================================

    /// <summary>
    /// ApplyEditModePreview()는 에디터에서 showOnStart 값에 따라
    /// 버튼을 미리 보여줍니다.
    /// </summary>
    private void ApplyEditModePreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        SetVisible(showOnStart);
    }
}
