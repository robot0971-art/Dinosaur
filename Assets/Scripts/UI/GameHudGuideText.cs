// ============================================================
// GameHudGuideText.cs
// ============================================================
// 이 스크립트가 하는 일:
// 게임 씬 하단 중앙에 안내 문구를 표시합니다.
// 일정 시간이 지나면 자동으로 사라집니다.
// ============================================================

using UnityEngine;          // Unity 기본 기능 사용
using TMPro;                // TextMeshPro 텍스트 사용
using System.Collections;   // Coroutine 사용

/// <summary>
/// GameHudGuideText
///
/// [이 스크립트가 필요한 이유]
/// 초보 플레이어에게 게임 방법을 알려주기 위해 필요합니다.
/// "내 레벨 이하 공룡을 먹으세요!" 같은 문구를 화면에 보여줍니다.
///
/// [어디에 붙이나요?]
/// - GameHudCanvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - guideText: 안내 문구를 표시할 TextMeshProUGUI 컴포넌트
/// </summary>
public class GameHudGuideText : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("가이드 문구 설정")]
    [Tooltip("안내 문구를 표시할 TextMeshProUGUI 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private TextMeshProUGUI guideText;

    [Tooltip("화면에 표시할 안내 문구입니다")]
    [SerializeField] private string guideMessage = "내 레벨 이하 공룡을 먹으세요!";

    [Tooltip("문구 폰트 크기입니다")]
    [SerializeField] private float fontSize = 24f;

    [Tooltip("문구 색상입니다")]
    [SerializeField] private Color textColor = new Color(1f, 0.87f, 0f, 1f); // #FFDD00 노란색

    [Header("표시 시간 설정")]
    [Tooltip("문구가 표시된 후 사라지기까지 걸리는 시간(초)입니다")]
    [SerializeField] private float displayDuration = 5f;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// 가이드 문구를 설정하고 표시 시간 후 사라지게 합니다.
    /// </summary>
    private void Start()
    {
        SetupGuideText();
        StartCoroutine(HideAfterDelay());
    }

    // ============================================================
    // 가이드 문구 설정 함수
    // ============================================================

    /// <summary>
    /// SetupGuideText()는 가이드 문구의 모양을 설정하는 함수입니다.
    ///
    /// [실행 흐름]
    /// 1. guideText가 연결되었는지 확인합니다.
    /// 2. 문구 내용을 설정합니다.
    /// 3. 폰트 크기를 설정합니다.
    /// 4. 색상을 설정합니다.
    /// 5. 가운데 정렬을 설정합니다.
    /// </summary>
    private void SetupGuideText()
    {
        if (guideText == null)
        {
            Debug.LogWarning("[GameHudGuideText] Guide Text가 연결되지 않았습니다.");
            return;
        }

        guideText.text = guideMessage;
        guideText.fontSize = fontSize;
        guideText.color = textColor;
        guideText.alignment = TextAlignmentOptions.Center;
    }

    // ============================================================
    // Coroutine 함수 (일정 시간 후 사라지게 하는 함수)
    // ============================================================

    /// <summary>
    /// HideAfterDelay()는 일정 시간 후 가이드 문구를 사라지게 하는 Coroutine입니다.
    ///
    /// Coroutine은 "잠시 쉬었다가 다시 실행되는 함수"입니다.
    /// 쉽게 말하면 타이머 같은 것입니다.
    ///
    /// [실행 흐름]
    /// 1. displayDuration만큼 기다립니다.
    /// 2. guideText 오브젝트를 비활성화합니다.
    /// 3. 비활성화하면 화면에서 사라집니다.
    /// </summary>
    private IEnumerator HideAfterDelay()
    {
        // displayDuration(초)만큼 기다립니다
        // WaitForSeconds는 "이만큼 기다려"라는 뜻입니다
        yield return new WaitForSeconds(displayDuration);

        // guideText 오브젝트를 비활성화하면 화면에서 사라집니다
        if (guideText != null)
        {
            guideText.gameObject.SetActive(false);
        }
    }
}
