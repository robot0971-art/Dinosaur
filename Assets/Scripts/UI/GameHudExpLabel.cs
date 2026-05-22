// ============================================================
// GameHudExpLabel.cs
// ============================================================
// 이 스크립트가 하는 일:
// EXP 바 근처에 "EXP" 글자를 표시합니다.
// EXP 바가 무엇을 의미하는지 플레이어가 바로 알 수 있게 해줍니다.
// ============================================================

using UnityEngine; // Unity 기본 기능 사용
using TMPro;       // TextMeshProUGUI 텍스트 사용

/// <summary>
/// GameHudExpLabel
///
/// [이 스크립트가 필요한 이유]
/// EXP 바 옆이나 아래에 "EXP" 글자를 보여주기 위해 필요합니다.
/// 글자가 없으면 초보 플레이어가 이 막대가 무엇인지 헷갈릴 수 있습니다.
///
/// [어디에 붙이나요?]
/// - LevelExpPanel 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - expLabelText: EXP 글자를 표시할 TextMeshProUGUI 컴포넌트
/// </summary>
public class GameHudExpLabel : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("EXP 레이블 설정")]
    [Tooltip("EXP 글자를 표시할 TextMeshProUGUI 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private TextMeshProUGUI expLabelText;

    [Tooltip("EXP 바 옆에 표시할 글자입니다")]
    [SerializeField] private string labelText = "EXP";

    [Tooltip("EXP 글자의 폰트 크기입니다")]
    [SerializeField] private float fontSize = 20f;

    [Tooltip("EXP 글자의 색상입니다")]
    [SerializeField] private Color textColor = Color.white;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// 여기서 Inspector에 적어둔 글자, 크기, 색상을 실제 화면에 적용합니다.
    /// </summary>
    private void Start()
    {
        ApplyLabelSettings();
    }

    // ============================================================
    // EXP 레이블 설정 함수
    // ============================================================

    /// <summary>
    /// ApplyLabelSettings()는 EXP 레이블의 모양을 설정하는 함수입니다.
    ///
    /// [실행 흐름]
    /// 1. expLabelText가 연결되었는지 확인합니다.
    /// 2. 텍스트 내용을 "EXP"로 설정합니다.
    /// 3. 폰트 크기를 설정합니다.
    /// 4. 글자 색상을 설정합니다.
    /// 5. 왼쪽 정렬로 설정합니다.
    /// </summary>
    private void ApplyLabelSettings()
    {
        // expLabelText가 연결되지 않았으면 경고를 보여주고 함수를 끝냅니다.
        if (expLabelText == null)
        {
            Debug.LogWarning("[GameHudExpLabel] EXP Label Text가 연결되지 않았습니다.");
            return;
        }

        // 화면에 표시할 글자를 설정합니다.
        expLabelText.text = labelText;

        // 글자 크기를 설정합니다.
        expLabelText.fontSize = fontSize;

        // 글자 색상을 설정합니다.
        expLabelText.color = textColor;

        // 왼쪽 정렬로 설정합니다.
        expLabelText.alignment = TextAlignmentOptions.Left;
    }

    // ============================================================
    // 다른 스크립트에서 사용할 수 있는 함수들
    // ============================================================

    /// <summary>
    /// SetLabelText()는 EXP 레이블 글자를 바꾸는 함수입니다.
    /// 예: SetLabelText("DNA")를 호출하면 EXP 대신 DNA가 표시됩니다.
    /// </summary>
    public void SetLabelText(string newLabelText)
    {
        labelText = newLabelText;
        ApplyLabelSettings();
    }
}
