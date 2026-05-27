// ============================================================
// GameHudHeartUI.cs
// ============================================================
// 이 스크립트가 하는 일:
// 화면에 하트 목숨 UI를 표시합니다.
// 하트를 먹으면 바로 화면에 반영됩니다.
// ============================================================

using UnityEngine;        // Unity 기본 기능 사용
using UnityEngine.Events; // Inspector에서 연결 가능한 이벤트 사용
using UnityEngine.UI;     // Image 컴포넌트 사용

/// <summary>
/// GameHudHeartUI
///
/// [이 스크립트가 필요한 이유]
/// 플레이어의 목숨 상태를 시각적으로 보여주기 위해 필요합니다.
/// 하트가 남아있는지, 얼마나 남았는지 바로 알 수 있습니다.
///
/// [어디에 붙이나요?]
/// - GameHudCanvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - heartImages: 하트 Image 컴포넌트 배열
/// - fullHeartSprite: 채워진 하트 이미지
/// - emptyHeartSprite: 빈 하트 이미지
/// </summary>
public class GameHudHeartUI : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("하트 설정")]
    [Tooltip("하트 Image 컴포넌트 배열을 여기에 연결하세요")]
    [SerializeField] private Image[] heartImages;

    [Tooltip("채워진 하트 이미지입니다")]
    [SerializeField] private Sprite fullHeartSprite;

    [Tooltip("빈 하트 이미지입니다")]
    [SerializeField] private Sprite emptyHeartSprite;

    [Tooltip("최대 하트 개수입니다")]
    [SerializeField] private int maxHearts = 3;

    [Tooltip("현재 하트 개수입니다. 게임 시작 시 이 값으로 하트가 표시됩니다")]
    [SerializeField] private int currentHearts = 3;

    [Header("하트 이벤트")]
    [Tooltip("하트 개수가 바뀔 때 실행됩니다. 지금은 비워 두어도 됩니다")]
    [SerializeField] private UnityEvent<int> onHeartsChanged;

    [Tooltip("하트가 0개가 되었을 때 실행됩니다. 게임오버 UI를 만들 때 연결하면 됩니다")]
    [SerializeField] private UnityEvent onHeartsEmpty;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 한 번 호출됩니다.
    /// maxHearts가 1보다 작으면 하트 UI가 이상해질 수 있어서 최소 1로 고정합니다.
    /// </summary>
    private void Awake()
    {
        if (maxHearts < 1)
        {
            maxHearts = 1;
        }
    }

    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// Inspector에서 설정한 현재 하트 수를 화면에 적용합니다.
    /// </summary>
    private void Start()
    {
        SetHearts(currentHearts);
    }

    // ============================================================
    // 하트 설정 함수
    // ============================================================

    /// <summary>
    /// SetHearts()는 현재 하트 수를 설정하는 함수입니다.
    /// 다른 스크립트에서 이 함수를 호출해서 하트를 바로바로 바꿀 수 있습니다.
    ///
    /// [예시]
    /// SetHearts(2);  // 하트 2개로 설정
    /// SetHearts(0);  // 하트 0개 (게임오버)
    /// </summary>
    public void SetHearts(int hearts)
    {
        // 하트 수를 0과 최대 하트 사이로 제한합니다
        currentHearts = Mathf.Clamp(hearts, 0, maxHearts);

        // 화면에 하트를 갱신합니다
        UpdateHeartDisplay();

        // 하트 개수가 바뀌었다고 이벤트로 알려줍니다
        onHeartsChanged?.Invoke(currentHearts);

        // 하트가 0개가 되면 빈 하트 이벤트를 실행합니다
        if (currentHearts == 0)
        {
            onHeartsEmpty?.Invoke();
        }
    }

    // ============================================================
    // 하트 추가 함수
    // ============================================================

    /// <summary>
    /// AddHeart()는 하트를 1개 추가하는 함수입니다.
    /// 하트를 먹었을 때 이 함수를 호출합니다.
    ///
    /// [예시]
    /// AddHeart();  // 하트 1개 추가
    /// </summary>
    public void AddHeart()
    {
        // SetHearts()를 사용하면 최대 하트 수를 넘지 않게 자동으로 막아줍니다
        SetHearts(currentHearts + 1);
    }

    // ============================================================
    // 하트 제거 함수
    // ============================================================

    /// <summary>
    /// RemoveHeart()는 하트를 1개 제거하는 함수입니다.
    /// 데미지를 받았을 때 이 함수를 호출합니다.
    ///
    /// [예시]
    /// RemoveHeart();  // 하트 1개 제거
    /// </summary>
    public void RemoveHeart()
    {
        // SetHearts()를 사용하면 0보다 작아지지 않게 자동으로 막아줍니다
        SetHearts(currentHearts - 1);
    }

    // ============================================================
    // 화면 갱신 함수
    // ============================================================

    /// <summary>
    /// UpdateHeartDisplay()는 화면의 하트를 갱신하는 함수입니다.
    /// 현재 하트 수에 따라 빨간 하트와 검정 하트를 표시합니다.
    ///
    /// [실행 흐름]
    /// 1. heartImages 배열을 순회합니다
    /// 2. 현재 하트 수보다 작으면 빨간 하트 (채워짐)
    /// 3. 현재 하트 수보다 크면 검정 하트 (비어있음)
    /// </summary>
    private void UpdateHeartDisplay()
    {
        if (heartImages == null) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            if (i < currentHearts)
            {
                // 현재 하트 수보다 작으면 빨간 하트 (채워짐)
                heartImages[i].sprite = fullHeartSprite;
                heartImages[i].enabled = true;
            }
            else
            {
                // 현재 하트 수보다 크면 검정 하트 (비어있음)
                heartImages[i].sprite = emptyHeartSprite;
                heartImages[i].enabled = true;
            }
        }
    }

    // ============================================================
    // Inspector에서 변경할 수 있는 함수들
    // ============================================================

    /// <summary>
    /// GetCurrentHearts()는 현재 하트 수를 반환합니다.
    /// </summary>
    public int GetCurrentHearts()
    {
        return currentHearts;
    }

    /// <summary>
    /// GetMaxHearts()는 최대 하트 수를 반환합니다.
    /// </summary>
    public int GetMaxHearts()
    {
        return maxHearts;
    }

    /// <summary>
    /// IsDead()는 하트가 0개인지 확인하는 함수입니다.
    /// </summary>
    public bool IsDead()
    {
        return currentHearts <= 0;
    }
}
