// ============================================================
// GameHudLevelUpPopup.cs
// ============================================================
// 이 스크립트가 하는 일:
// 레벨업이 발생했을 때 "LEVEL UP!" 텍스트를 표시합니다.
// 2초 후 투명화 효과와 함께 팝업이 자동으로 사라집니다.
// CanvasGroup은 Inspector 연결 없이 자동으로 준비합니다.
// 기능 22: LEVEL UP 텍스트 표시
// 기능 24: 레벨업 팝업 자동 닫기
// ============================================================

using DinoGrow.Core.Growth;
using DinoGrow.Infrastructure.Events;
using UnityEngine;
using VContainer;

/// <summary>
/// GameHudLevelUpPopup
///
/// [이 스크립트가 필요한 이유]
/// 레벨업이 발생했을 때 플레이어에게 "레벨업 했다!"는 것을
/// 텍스트로 알려주기 위해 필요합니다.
///
/// [어디에 붙이나요?]
/// - GameSceneUI 씬의 UI Canvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - levelUpText: LEVEL UP 텍스트
/// </summary>
public class GameHudLevelUpPopup : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("LEVEL UP 텍스트 연결")]
    [Tooltip("레벨업 시 표시할 LEVEL UP 텍스트를 연결하세요")]
    [SerializeField] private TMPro.TextMeshProUGUI levelUpText;

    [Header("자동 닫기 설정")]
    [Tooltip("팝업이 자동으로 사라지기까지의 시간입니다. 기본 2초")]
    [SerializeField] private float autoCloseDelay = 2f;

    [Tooltip("투명화 효과가 걸리는 시간입니다. 기본 0.5초")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("에디터 테스트")]
    [Tooltip("Play를 누르지 않아도 팝업 배경이 보이는지 확인할 수 있습니다")]
    [SerializeField] private bool showOnStart;

    // ============================================================
    // DI로 주입받을 변수들
    // ============================================================

    // VContainer가 자동으로 넣어주는 이벤트 버스입니다.
    // GameLifetimeScope가 있는 씬에서만 주입됩니다.
    private GameEventBus eventBus;

    // 이벤트를 이미 구독했는지 확인하는 변수입니다.
    // 같은 이벤트를 여러 번 연결하는 실수를 막기 위해 사용합니다.
    private bool subscribedToEvents;

    // ============================================================
    // 자동 닫기 상태 변수
    // ============================================================

    // 팝업이 표시된 후 얼마나 시간이 지났는지 저장합니다.
    private float popupTimer;

    // 팝업이 표시 중인지 확인합니다.
    private bool isPopupVisible;

    // 투명화가 진행 중인지 확인합니다.
    private bool isFading;

    // 투명화가 시작된 후 얼마나 시간이 지났는지 저장합니다.
    private float fadeTimer;

    // 투명화 효과에 사용하는 CanvasGroup입니다.
    // Inspector에 보이지 않고, LevelUpPanel에서 자동으로 찾거나 붙입니다.
    private CanvasGroup canvasGroup;

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
    /// OnEnable()은 오브젝트가 켜질 때 호출됩니다.
    /// 이벤트 구독을 준비합니다.
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
    /// Start()는 Play를 누른 뒤 한 번 호출됩니다.
    /// CanvasGroup을 자동으로 준비하고 텍스트를 숨깁니다.
    /// </summary>
    private void Start()
    {
        if (!Application.isPlaying)
        {
            ApplyEditModePreview();
            return;
        }

        // CanvasGroup이 없으면 LEVEL UP 텍스트에 자동으로 준비합니다.
        EnsureCanvasGroup();

        // 기본은 숨겨져 있어야 합니다.
        // showOnStart가 켜져 있으면 테스트용으로 보여줍니다.
        SetVisible(showOnStart);
    }

    /// <summary>
    /// OnDisable()은 오브젝트가 꺼질 때 호출됩니다.
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
    /// Update()는 매 프레임마다 호출됩니다.
    /// 팝업 표시 후 자동 닫기 타이머를 갱신합니다.
    /// </summary>
    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UpdateAutoClose();
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
    /// ShowLevelUpPopup()은 LEVEL UP 텍스트를 보이게 합니다.
    /// 레벨업이 발생했을 때 호출됩니다.
    /// 팝업을 표시하고 자동 닫기 타이머를 초기화합니다.
    /// </summary>
    public void ShowLevelUpPopup()
    {
        // 자동 닫기 타이머를 초기화합니다.
        popupTimer = 0f;
        isPopupVisible = true;
        isFading = false;
        fadeTimer = 0f;

        // CanvasGroup Alpha를 1로 초기화합니다.
        ResetCanvasGroupAlpha();

        SetVisible(true);
    }

    /// <summary>
    /// HideLevelUpPopup()은 LEVEL UP 텍스트를 숨깁니다.
    /// </summary>
    public void HideLevelUpPopup()
    {
        isPopupVisible = false;
        isFading = false;
        SetVisible(false);
    }

    /// <summary>
    /// SetVisible()은 LEVEL UP 텍스트를 보이거나 숨깁니다.
    /// true면 보이고, false면 숨겨집니다.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (levelUpText != null)
        {
            levelUpText.gameObject.SetActive(visible);
        }
    }

    // ============================================================
    // CanvasGroup 자동 준비 함수
    // ============================================================

    /// <summary>
    /// EnsureCanvasGroup()은 CanvasGroup이 없으면 LevelUpText 오브젝트에 자동으로 붙입니다.
    /// Inspector에서 따로 연결하지 않아도 자동으로 동작합니다.
    /// </summary>
    private void EnsureCanvasGroup()
    {
        // CanvasGroup이 이미 연결되어 있으면 아무것도 하지 않습니다.
        if (canvasGroup != null)
        {
            return;
        }

        // LevelUpText가 없으면 아무것도 하지 않습니다.
        if (levelUpText == null)
        {
            Debug.LogWarning("LevelUpText가 연결되지 않아서 CanvasGroup을 자동으로 붙일 수 없습니다.");
            return;
        }

        // LevelUpText 오브젝트에 CanvasGroup이 이미 있는지 확인합니다.
        canvasGroup = levelUpText.GetComponent<CanvasGroup>();

        // 없으면 새로 붙입니다.
        if (canvasGroup == null)
        {
            canvasGroup = levelUpText.gameObject.AddComponent<CanvasGroup>();
            Debug.Log("LevelUpText에 CanvasGroup을 자동으로 붙였습니다.");
        }
    }

    // ============================================================
    // 자동 닫기 함수
    // ============================================================

    /// <summary>
    /// UpdateAutoClose()는 매 프레임마다 자동 닫기 타이머를 갱신합니다.
    /// 팝업 표시 후 autoCloseDelay가 지나면 투명화를 시작합니다.
    /// 투명화가 끝나면 팝업을 숨깁니다.
    /// </summary>
    private void UpdateAutoClose()
    {
        // 팝업이 표시 중이 아니면 아무것도 하지 않습니다.
        if (!isPopupVisible)
        {
            return;
        }

        // 투명화가 진행 중이면 투명화를 갱신합니다.
        if (isFading)
        {
            fadeTimer += Time.unscaledDeltaTime;

            // 투명화 진행률을 계산합니다. 0에서 1 사이 값입니다.
            var fadeProgress = Mathf.Clamp01(fadeTimer / fadeDuration);

            // CanvasGroup Alpha를 1에서 0으로 줄입니다.
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - fadeProgress;
            }

            // 투명화가 완료되면 팝업을 숨깁니다.
            if (fadeProgress >= 1f)
            {
                HideLevelUpPopup();
            }

            return;
        }

        // 팝업 표시 후 시간을 갱신합니다.
        popupTimer += Time.unscaledDeltaTime;

        // autoCloseDelay가 지나면 투명화를 시작합니다.
        if (popupTimer >= autoCloseDelay)
        {
            isFading = true;
            fadeTimer = 0f;
        }
    }

    /// <summary>
    /// ResetCanvasGroupAlpha()는 CanvasGroup의 Alpha를 1로 초기화합니다.
    /// 팝업이 다시 표시될 때 투명하지 않게 하기 위해 사용합니다.
    /// </summary>
    private void ResetCanvasGroupAlpha()
    {
        // CanvasGroup이 없으면 자동으로 준비합니다.
        EnsureCanvasGroup();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    // ============================================================
    // 이벤트 구독 함수
    // ============================================================

    /// <summary>
    /// SubscribeToEvents()는 GameEventBus의 이벤트를 구독합니다.
    /// PlayerGrowthChanged 이벤트가 발생하면 OnPlayerGrowthChanged()가 호출됩니다.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (eventBus == null || subscribedToEvents)
        {
            return;
        }

        eventBus.PlayerGrowthChanged += OnPlayerGrowthChanged;
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

        eventBus.PlayerGrowthChanged -= OnPlayerGrowthChanged;
        subscribedToEvents = false;
    }

    // ============================================================
    // 이벤트 핸들러
    // ============================================================

    /// <summary>
    /// OnPlayerGrowthChanged()는 플레이어 성장 상태가 바뀔 때 호출됩니다.
    /// GrowthResult의 LeveledUp이 true이면 LEVEL UP 텍스트를 표시합니다.
    /// </summary>
    private void OnPlayerGrowthChanged(GrowthResult result)
    {
        // LeveledUp이 true이면 레벨업이 발생한 것입니다.
        if (result.LeveledUp)
        {
            // 팝업을 표시합니다.
            ShowLevelUpPopup();
        }
        else
        {
            HideLevelUpPopup();
        }
    }

    // ============================================================
    // 에디터 미리보기
    // ============================================================

    /// <summary>
    /// ApplyEditModePreview()는 에디터에서 showOnStart 값에 따라
    /// 팝업 배경을 미리 보여줍니다.
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
