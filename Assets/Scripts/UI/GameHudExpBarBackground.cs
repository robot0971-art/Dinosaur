// ============================================================
// GameHudExpBarBackground.cs
// ============================================================
// 이 스크립트가 하는 일:
// EXP 바의 배경 이미지를 관리합니다.
// 배경은 반투명 검정 네모입니다.
// 나중에 이 안에 초록색 채우기가 들어갑니다.
// ============================================================

using UnityEngine;      // Unity 기본 기능 사용
using UnityEngine.UI;   // Image 컴포넌트 사용

/// <summary>
/// GameHudExpBarBackground
/// 
/// [이 스크립트가 필요한 이유]
/// EXP 바의 배경을 표시하기 위해 필요합니다.
/// 배경이 있어야 채우기 막대가 잘 보입니다.
/// 
/// [어디에 붙이나요?]
/// - GameHudCanvas 오브젝트에 붙입니다.
/// 
/// [Inspector에서 연결할 것]
/// - backgroundImage: ExpBarBackground의 Image 컴포넌트
/// </summary>
public class GameHudExpBarBackground : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================
    
    [Header("EXP 바 배경 설정")]
    [Tooltip("EXP 바 배경 Image 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private Image backgroundImage;
    
    // ============================================================
    // Unity 생명주기 함수
    // ============================================================
    
    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// Inspector에서 설정한 값을 화면에 적용합니다.
    /// </summary>
    private void Start()
    {
        // EXP 바 배경이 연결되어 있는지 확인하는 함수를 호출합니다.
        // 크기와 색상은 Unity Inspector에서 직접 조절한 값을 그대로 사용합니다.
        CheckExpBarBackground();
    }

    // ============================================================
    // EXP 바 배경 설정 함수
    // ============================================================
    
    /// <summary>
    /// CheckExpBarBackground()는 EXP 바 배경 연결 상태를 확인하는 함수입니다.
    /// 색상과 크기는 Unity Inspector에서 직접 만든 값을 그대로 유지합니다.
    /// 
    /// [실행 흐름]
    /// 1. backgroundImage가 연결되었는지 확인합니다.
    /// 2. 배경 색상을 설정합니다.
    /// 3. 배경 크기를 설정합니다.
    /// </summary>
    private void CheckExpBarBackground()
    {
        // backgroundImage가 연결되지 않았으면 함수를 끝냅니다
        if (backgroundImage == null)
        {
            Debug.LogWarning("[GameHudExpBarBackground] Background Image가 연결되지 않았습니다.");
            return;
        }
        
        // 여기서는 아무 값도 강제로 바꾸지 않습니다.
        // 사용자가 Inspector에서 직접 맞춘 크기, 위치, 색상을 그대로 유지합니다.
    }

    // ============================================================
    // Inspector에서 변경할 수 있는 함수들
    // ============================================================
    
    /// <summary>
    /// SetBackgroundColor()는 배경 색상을 변경하는 함수입니다.
    /// 
    /// [언제 호출되나요?]
    /// - 다른 스크립트에서 배경 색상을 바꾸고 싶을 때 호출합니다.
    /// 
    /// [매개변수]
    /// - newColor: 새로운 배경 색상
    /// </summary>
    public void SetBackgroundColor(Color newColor)
    {
        if (backgroundImage == null) return;
        backgroundImage.color = newColor;
    }
    
    /// <summary>
    /// SetBarSize()는 배경 크기를 변경하는 함수입니다.
    /// 
    /// [언제 호출되나요?]
    /// - 다른 스크립트에서 바 크기를 바꾸고 싶을 때 호출합니다.
    /// 
    /// [매개변수]
    /// - width: 새로운 너비
    /// - height: 새로운 높이
    /// </summary>
    public void SetBarSize(float width, float height)
    {
        if (backgroundImage == null) return;
        
        RectTransform rect = backgroundImage.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
