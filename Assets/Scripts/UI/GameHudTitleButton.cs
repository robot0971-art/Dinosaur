// ============================================================
// GameHudTitleButton.cs
// ============================================================
// 이 스크립트가 하는 일:
// 게임오버 화면에서 "타이틀" 버튼을 보여줍니다.
// 버튼을 누르면 TitleScene으로 이동합니다.
// 기능 30: 타이틀로 돌아가기 버튼
// ============================================================

using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// GameHudTitleButton
///
/// [이 스크립트가 필요한 이유]
/// 게임오버 화면에서 바로 다시 시작하지 않고,
/// 시작 화면(타이틀 화면)으로 돌아가고 싶을 때 필요합니다.
///
/// [어디에 붙이나요?]
/// - GameOverCanvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - titleButton: 타이틀 버튼의 Button 컴포넌트
/// - titleSceneName: 이동할 타이틀 씬 이름
/// </summary>
public class GameHudTitleButton : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("타이틀 버튼 연결")]
    [Tooltip("게임오버 시 표시할 타이틀 버튼의 Button 컴포넌트를 연결하세요")]
    [SerializeField] private Button titleButton;

    [Header("타이틀 씬 이름")]
    [Tooltip("타이틀 버튼을 눌렀을 때 이동할 씬 이름입니다. 예: TitleScene")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("에디터 테스트")]
    [Tooltip("Play를 눌렀을 때 타이틀 버튼을 바로 보이게 할지 정합니다")]
    [SerializeField] private bool showOnStart;

    // ============================================================
    // DI로 주입받을 변수들
    // ============================================================

    // VContainer가 자동으로 넣어주는 이벤트 버스입니다.
    // GameLifetimeScope가 없는 GameOver 씬에서는 null일 수 있습니다.
    private GameEventBus eventBus;

    // 이벤트를 이미 구독했는지 확인하는 변수입니다.
    // 같은 이벤트를 여러 번 연결하는 실수를 막기 위해 사용합니다.
    private bool subscribedToEvents;

    // ============================================================
    // DI 주입 함수
    // ============================================================

    /// <summary>
    /// Construct()는 VContainer가 자동으로 호출하는 함수입니다.
    /// GameLifetimeScope가 있는 씬에서 GameEventBus를 연결받습니다.
    /// </summary>
    [Inject]
    public void Construct(GameEventBus eventBus)
    {
        this.eventBus = eventBus;
        SubscribeToEvents();
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
    /// OnEnable()은 오브젝트가 켜질 때 호출됩니다.
    /// 이벤트 구독과 버튼 클릭 연결을 준비합니다.
    /// </summary>
    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            ApplyEditModePreview();
            return;
        }

        SubscribeToEvents();
        SetupButtonListener();
    }

    /// <summary>
    /// Start()는 Play를 누른 뒤 한 번 호출됩니다.
    /// 처음에는 버튼을 숨기고, showOnStart가 켜져 있으면 테스트용으로 보여줍니다.
    /// </summary>
    private void Start()
    {
        if (!Application.isPlaying)
        {
            ApplyEditModePreview();
            return;
        }

        SetVisible(showOnStart);
    }

    /// <summary>
    /// OnDisable()은 오브젝트가 꺼질 때 호출됩니다.
    /// 연결했던 이벤트와 버튼 클릭 연결을 정리합니다.
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
    /// OnValidate()는 Inspector 값이 바뀔 때 호출됩니다.
    /// 에디터에서 showOnStart 미리보기를 바로 반영합니다.
    /// </summary>
    private void OnValidate()
    {
        ApplyEditModePreview();
    }

    // ============================================================
    // 공개 함수
    // ============================================================

    /// <summary>
    /// ShowTitleButton()은 타이틀 버튼을 보이게 합니다.
    /// 게임오버 상태가 되었을 때 호출됩니다.
    /// </summary>
    public void ShowTitleButton()
    {
        SetVisible(true);
    }

    /// <summary>
    /// HideTitleButton()은 타이틀 버튼을 숨깁니다.
    /// 게임오버 상태가 아닐 때 호출됩니다.
    /// </summary>
    public void HideTitleButton()
    {
        SetVisible(false);
    }

    /// <summary>
    /// GoToTitleScene()은 타이틀 버튼을 누르면 호출됩니다.
    /// Inspector에 적은 titleSceneName 씬으로 이동합니다.
    /// public 함수라서 Button의 OnClick에 직접 연결해서 테스트할 수도 있습니다.
    /// </summary>
    public void GoToTitleScene()
    {
        if (string.IsNullOrWhiteSpace(titleSceneName))
        {
            Debug.LogWarning("타이틀 씬 이름이 비어 있습니다. GameHudTitleButton의 Title Scene Name을 확인하세요.");
            return;
        }

        Debug.Log($"타이틀 버튼 클릭됨. 이동할 씬: {titleSceneName}");

        // 게임오버 때 시간이 멈춰 있었을 수 있으니 정상 속도로 되돌립니다.
        Time.timeScale = 1f;

        // Inspector에 적은 타이틀 씬으로 이동합니다.
        SceneManager.LoadScene(titleSceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// SetVisible()은 버튼을 보이거나 숨깁니다.
    /// true면 보이고, false면 숨겨집니다.
    /// </summary>
    public void SetVisible(bool visible)
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
    /// 버튼을 누르면 GoToTitleScene()이 실행됩니다.
    /// </summary>
    private void SetupButtonListener()
    {
        if (titleButton == null)
        {
            Debug.LogWarning("Title Button이 연결되지 않았습니다. GameOverCanvas의 GameHudTitleButton에서 TitleButton을 연결하세요.");
            return;
        }

        // 버튼이 눌릴 수 있는 상태인지 확인합니다.
        titleButton.interactable = true;

        // 버튼 이미지가 마우스 클릭을 받을 수 있게 합니다.
        if (titleButton.targetGraphic != null)
        {
            titleButton.targetGraphic.raycastTarget = true;
        }

        // 중복 연결을 막기 위해 먼저 제거한 뒤 다시 연결합니다.
        titleButton.onClick.RemoveListener(GoToTitleScene);
        titleButton.onClick.AddListener(GoToTitleScene);
    }

    /// <summary>
    /// RemoveButtonListener()는 버튼 클릭 이벤트 연결을 해제합니다.
    /// 오브젝트가 꺼질 때 안전하게 정리하기 위해 사용합니다.
    /// </summary>
    private void RemoveButtonListener()
    {
        if (titleButton == null)
        {
            return;
        }

        titleButton.onClick.RemoveListener(GoToTitleScene);
    }

    // ============================================================
    // 이벤트 구독 함수
    // ============================================================

    /// <summary>
    /// SubscribeToEvents()는 GameEventBus의 GameStateChanged 이벤트를 구독합니다.
    /// 게임 상태가 바뀌면 OnGameStateChanged()가 호출됩니다.
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
    /// UnsubscribeFromEvents()는 GameEventBus 이벤트 구독을 해제합니다.
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
    /// GameOver 상태면 타이틀 버튼을 보여주고, 아니면 숨깁니다.
    /// </summary>
    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            ShowTitleButton();
        }
        else
        {
            HideTitleButton();
        }
    }

    // ============================================================
    // 에디터 미리보기
    // ============================================================

    /// <summary>
    /// ApplyEditModePreview()는 에디터에서 showOnStart 값에 따라
    /// 버튼을 미리 보이거나 숨깁니다.
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
