// ============================================================
// GameHudFlashEffect.cs
// ============================================================
// 이 스크립트가 하는 일:
// 게임오버 순간 화면이 잠깐 빨갛게 변하는 효과를 만듭니다.
// 0.3초 동안 빨간색이 번쩍였다가 원래대로 돌아옵니다.
// ============================================================

using DinoGrow.Core.Stage;
using DinoGrow.Infrastructure.Events;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// GameHudFlashEffect
///
/// [이 스크립트가 필요한 이유]
/// 게임오버 순간 플레이어에게 "위험하다"는 느낌을 주기 위해
/// 화면이 잠깐 빨갛게 번쩍이는 효과가 필요합니다.
///
/// [어디에 붙이나요?]
/// - GameHudCanvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - flashImage: 빨간색 Image 컴포넌트
/// </summary>
public class GameHudFlashEffect : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("플래시 이미지 연결")]
    [Tooltip("빨간색으로 번쩍일 Image 컴포넌트를 연결하세요")]
    [SerializeField] private Image flashImage;

    [Header("플래시 설정")]
    [Tooltip("플래시 색상입니다. 기본은 빨간색입니다")]
    [SerializeField] private Color flashColor = new(1f, 0f, 0f, 1f);

    [Tooltip("플래시가 지속되는 시간입니다. 0.3초가 적당합니다")]
    [SerializeField] private float flashDuration = 0.3f;

    [Header("에디터 테스트")]
    [Tooltip("Play를 누르면 자동으로 플래시 효과를 실행합니다")]
    [SerializeField] private bool playFlashOnStart;

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // 플래시가 진행 중인지 확인합니다
    private bool isFlashing;

    // 플래시가 시작된 뒤 얼마나 시간이 지났는지 저장합니다
    private float flashTimer;

    // VContainer가 자동으로 넣어주는 이벤트 버스입니다
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

    private void OnEnable()
    {
        DisableFlashRaycastTarget();

        if (!Application.isPlaying)
        {
            return;
        }

        SubscribeToEvents();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // 플래시 Image를 처음에는 숨깁니다
        SetFlashAlpha(0f);
        DisableFlashRaycastTarget();

        // 테스트 옵션이 켜져 있으면 시작할 때 플래시를 실행합니다
        if (playFlashOnStart)
        {
            PlayFlash();
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UnsubscribeFromEvents();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // 플래시가 진행 중일 때만 시간을 갱신합니다
        UpdateFlash();
    }

    // ============================================================
    // 공개 함수
    // ============================================================

    /// <summary>
    /// PlayFlash()는 빨간 플래시 효과를 실행합니다.
    /// 게임오버가 발생했을 때 호출합니다.
    /// </summary>
    public void PlayFlash()
    {
        isFlashing = true;
        flashTimer = 0f;
        SetFlashAlpha(1f);
    }

    // ============================================================
    // 플래시 갱신 함수
    // ============================================================

    /// <summary>
    /// UpdateFlash()는 플래시 효과를 매 프레임 갱신합니다.
    /// 시간이 지나면서 Alpha를 1에서 0으로 줄입니다.
    /// </summary>
    private void UpdateFlash()
    {
        if (!isFlashing)
        {
            return;
        }

        flashTimer += Time.deltaTime;

        // 0에서 1 사이 진행률을 계산합니다
        var progress = Mathf.Clamp01(flashTimer / flashDuration);

        // Alpha를 1에서 0으로 줄입니다
        var alpha = 1f - progress;
        SetFlashAlpha(alpha);

        // 플래시가 끝나면 숨깁니다
        if (progress >= 1f)
        {
            isFlashing = false;
            SetFlashAlpha(0f);
        }
    }

    /// <summary>
    /// SetFlashAlpha()는 플래시 Image의 투명도를 설정합니다.
    /// alpha가 1이면 완전히 보이고, 0이면 완전히 투명합니다.
    /// </summary>
    private void SetFlashAlpha(float alpha)
    {
        if (flashImage == null)
        {
            return;
        }

        var color = flashColor;
        color.a = alpha;
        flashImage.color = color;
    }

    /// <summary>
    /// FlashOverlay가 투명해도 버튼 클릭을 막지 않게 합니다.
    /// Image의 Raycast Target이 켜져 있으면 투명한 이미지도 마우스 클릭을 가로챌 수 있습니다.
    /// </summary>
    private void DisableFlashRaycastTarget()
    {
        if (flashImage == null)
        {
            return;
        }

        flashImage.raycastTarget = false;
    }

    // ============================================================
    // 이벤트 구독 함수
    // ============================================================

    private void SubscribeToEvents()
    {
        if (eventBus == null || subscribedToEvents)
        {
            return;
        }

        eventBus.GameStateChanged += OnGameStateChanged;
        subscribedToEvents = true;
    }

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
    /// GameState가 GameOver이면 플래시를 실행합니다.
    /// </summary>
    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            PlayFlash();
        }
    }
}
