// ============================================================
// GameHudGameOverPanel.cs
// ============================================================
// 이 스크립트가 하는 일:
// 게임오버가 발생했을 때 화면 전체를 덮는 반투명 검정 배경을 표시합니다.
// GAME OVER 텍스트와 부제목 텍스트도 같이 표시합니다.
// 기능 25: 게임오버 배경 표시
// 기능 27: GAME OVER 텍스트 표시
// 기능 28: 게임오버 부제목 텍스트 표시
// ============================================================

using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Events;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// GameHudGameOverPanel
///
/// [이 스크립트가 필요한 이유]
/// 게임오버가 발생했을 때 플레이어에게 "게임이 끝났다"는 것을
/// 화면 전체를 덮는 반투명 배경으로 알려주기 위해 필요합니다.
///
/// [어디에 붙이나요?]
/// - GameHudCanvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - gameOverBackground: 게임오버 배경 Image 또는 GameObject
/// </summary>
public class GameHudGameOverPanel : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("게임오버 배경 연결")]
    [Tooltip("게임오버 시 표시할 배경 GameObject를 연결하세요")]
    [SerializeField] private GameObject gameOverBackground;

    [Header("GAME OVER 텍스트 연결")]
    [Tooltip("게임오버 시 표시할 GAME OVER 텍스트를 연결하세요")]
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("게임오버 부제목 텍스트 연결")]
    [Tooltip("GAME OVER 아래에 표시할 부제목 텍스트를 연결하세요")]
    [SerializeField] private TextMeshProUGUI gameOverSubtitleText;

    [Header("에디터 테스트")]
    [Tooltip("Play를 누르지 않아도 게임오버 배경이 보이는지 확인할 수 있습니다")]
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
    }

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

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
    /// ShowGameOver()는 게임오버 배경을 표시합니다.
    /// </summary>
    public void ShowGameOver()
    {
        SetVisible(true);
    }

    /// <summary>
    /// HideGameOver()는 게임오버 배경을 숨깁니다.
    /// </summary>
    public void HideGameOver()
    {
        SetVisible(false);
    }

    /// <summary>
    /// SetVisible()는 배경을 보이거나 숨깁니다.
    /// visible이 true면 보이고, false면 숨깁니다.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (gameOverBackground != null)
        {
            gameOverBackground.SetActive(visible);
        }

        if (gameOverText != null)
        {
            ConfigureGameOverText();
            gameOverText.gameObject.SetActive(visible);
        }

        // 부제목 텍스트도 같이 보이거나 숨깁니다.
        if (gameOverSubtitleText != null)
        {
            ConfigureGameOverSubtitleText();
            gameOverSubtitleText.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// GAME OVER 문구가 크게 보여도 한 줄로 유지되게 설정합니다.
    /// 위치와 크기는 사용자가 RectTransform에서 직접 조절할 수 있게 건드리지 않습니다.
    /// </summary>
    private void ConfigureGameOverText()
    {
        if (gameOverText == null)
        {
            return;
        }

        gameOverText.enableWordWrapping = false;
        gameOverText.overflowMode = TextOverflowModes.Overflow;
    }

    /// <summary>
    /// 게임오버 부제목이 한 글자씩 아래로 내려가지 않게 설정합니다.
    /// RectTransform 크기와 위치는 사용자가 Inspector에서 직접 조절할 수 있게 건드리지 않습니다.
    /// </summary>
    private void ConfigureGameOverSubtitleText()
    {
        if (gameOverSubtitleText == null)
        {
            return;
        }

        gameOverSubtitleText.enableWordWrapping = false;
        gameOverSubtitleText.overflowMode = TextOverflowModes.Overflow;
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
    /// GameState가 GameOver이면 배경을 표시합니다.
    /// </summary>
    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            ShowGameOver();
        }
        else
        {
            HideGameOver();
        }
    }

    // ============================================================
    // 에디터 미리보기
    // ============================================================

    /// <summary>
    /// ApplyEditModePreview()는 에디터에서 showOnStart 값에 따라
    /// 배경을 미리 보여줍니다.
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
