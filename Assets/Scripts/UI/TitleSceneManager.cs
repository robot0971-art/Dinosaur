using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TitleSceneManager.cs
/// 
/// [이 스크립트가 필요한 이유]
/// 타이틀 화면의 배경 이미지를 관리하기 위해 필요합니다.
/// Inspector에서 배경 이미지를 쉽게 교체할 수 있게 해줍니다.
/// Play를 누르지 않아도 Game 창에서 배경이 보이게 합니다.
/// 
/// [어디에 붙이나요?]
/// - TitleScene의 빈 GameObject에 붙입니다.
/// - GameObject 이름은 "TitleSceneManager"로 설정하세요.
/// 
/// [Inspector에서 연결할 것]
/// - backgroundImage: 타이틀 배경 Image 컴포넌트를 연결합니다.
/// - backgroundSprite: 배경 이미지로 사용할 Sprite를 넣습니다.
/// - backgroundColor: 배경 색상을 설정합니다.
/// </summary>
[ExecuteAlways] // Play를 누르지 않아도 Game 창에서 배경이 보이게 하는 속성
public class TitleSceneManager : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================
    
    [Header("타이틀 배경 설정")]
    [Tooltip("타이틀 배경 이미지 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private Image backgroundImage;
    
    [Tooltip("배경 이미지로 사용할 Sprite를 여기에 넣으세요. 비어있으면 색상 배경이 표시됩니다")]
    [SerializeField] private Sprite backgroundSprite;
    
    [Tooltip("배경 이미지의 색상입니다. Sprite가 없으면 이 색상이 표시됩니다")]
    [SerializeField] private Color backgroundColor = Color.white;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================
    
    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// 배경 이미지를 설정하는 역할을 합니다.
    /// </summary>
    private void Start()
    {
        // 배경 이미지 설정 함수를 호출합니다
        SetupBackground();
    }

    // ============================================================
    // 배경 설정 함수
    // ============================================================
    
    /// <summary>
    /// SetupBackground()는 배경 이미지를 설정하는 함수입니다.
    /// Inspector에서 설정한 Sprite와 Color를 적용합니다.
    /// 
    /// [실행 흐름]
    /// 1. backgroundImage가 연결되었는지 확인합니다.
    /// 2. 배경 색상을 설정합니다.
    /// 3. Sprite가 있으면 이미지를 표시합니다.
    /// 4. Sprite가 없으면 색상만 표시합니다.
    /// </summary>
    private void SetupBackground()
    {
        // backgroundImage가 연결되지 않았으면 경고를 표시합니다
        if (backgroundImage == null)
        {
            Debug.LogWarning("[TitleSceneManager] Background Image가 연결되지 않았습니다.");
            return;
        }

        // 배경 이미지의 색상을 설정합니다
        backgroundImage.color = backgroundColor;

        // 배경 Sprite가 설정되어 있으면 적용합니다
        if (backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
        }
        else
        {
            // Sprite가 없으면 색상만으로 배경을 표시합니다
            backgroundImage.sprite = null;
        }
    }

    // ============================================================
    // Inspector에서 배경을 변경할 수 있는 함수들
    // ============================================================
    
    /// <summary>
    /// ChangeBackgroundSprite()는 Inspector나 다른 스크립트에서
    /// 배경 이미지를 변경할 때 사용하는 함수입니다.
    /// 
    /// [언제 호출되나요?]
    /// - 다른 스크립트에서 배경을 바꾸고 싶을 때 호출합니다.
    /// - 예: 이벤트 발생 시 배경 변경
    /// 
    /// [매개변수]
    /// - newSprite: 새로운 배경 이미지
    /// </summary>
    public void ChangeBackgroundSprite(Sprite newSprite)
    {
        if (backgroundImage == null)
        {
            Debug.LogWarning("[TitleSceneManager] Background Image가 연결되지 않았습니다.");
            return;
        }

        backgroundImage.sprite = newSprite;
    }

    /// <summary>
    /// ChangeBackgroundColor()는 배경 색상을 변경하는 함수입니다.
    /// 
    /// [언제 호출되나요?]
    /// - 다른 스크립트에서 배경 색상을 바꾸고 싶을 때 호출합니다.
    /// 
    /// [매개변수]
    /// - newColor: 새로운 배경 색상
    /// </summary>
    public void ChangeBackgroundColor(Color newColor)
    {
        if (backgroundImage == null)
        {
            Debug.LogWarning("[TitleSceneManager] Background Image가 연결되지 않았습니다.");
            return;
        }

        backgroundImage.color = newColor;
    }
}
