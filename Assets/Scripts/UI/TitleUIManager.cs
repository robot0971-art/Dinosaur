using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TitleUIManager.cs
/// 
/// [이 스크립트가 필요한 이유]
/// 타이틀 화면의 배경 이미지와 게임 제목을 한 번에 관리하기 위해 필요합니다.
/// Inspector에서 모든 설정을 변경할 수 있습니다.
/// Play를 누르지 않아도 Game 창에서 UI가 보이게 합니다.
/// 
/// [어디에 붙이나요?]
/// - TitleCanvas 오브젝트에 붙입니다.
/// 
/// [Inspector에서 연결할 것]
/// - backgroundImage: 배경 이미지 컴포넌트
/// - titleText: 제목 텍스트 컴포넌트
/// </summary>
[ExecuteAlways] // Play를 누르지 않아도 Game 창에서 보이게 하는 속성
public class TitleUIManager : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================
    
    [Header("배경 이미지 설정")]
    [Tooltip("배경 이미지 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private Image backgroundImage;
    
    [Tooltip("배경 이미지로 사용할 Sprite를 여기에 넣으세요")]
    [SerializeField] private Sprite backgroundSprite;
    
    [Tooltip("배경 색상입니다. Sprite가 없으면 이 색상이 표시됩니다")]
    [SerializeField] private Color backgroundColor = Color.white;

    [Header("게임 제목 설정")]
    [Tooltip("게임 제목 텍스트 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Tooltip("게임 제목 내용입니다")]
    [SerializeField] private string titleContent = "Dino Evolution";
    
    [Tooltip("제목 폰트 크기입니다")]
    [SerializeField] private float titleFontSize = 72f;
    
    [Tooltip("제목 색상입니다")]
    [SerializeField] private Color titleColor = new Color(1f, 0.84f, 0f, 1f); // #FFD700

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================
    
    /// <summary>
    /// Awake()는 오브젝트가 생성될 때 한 번 호출됩니다.
    /// Start()보다 먼저 실행됩니다.
    /// </summary>
    private void Awake()
    {
        // 배경 이미지와 제목 텍스트를 설정합니다
        SetupBackground();
        SetupTitleText();
    }

    // ============================================================
    // 배경 이미지 설정 함수
    // ============================================================
    
    /// <summary>
    /// SetupBackground()는 배경 이미지를 설정하는 함수입니다.
    /// Inspector에서 설정한 Sprite와 Color를 적용합니다.
    /// </summary>
    private void SetupBackground()
    {
        // backgroundImage가 연결되지 않았으면 함수를 끝냅니다
        if (backgroundImage == null)
        {
            Debug.LogWarning("[TitleUIManager] Background Image가 연결되지 않았습니다.");
            return;
        }

        // 배경 색상을 설정합니다
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
    // 제목 텍스트 설정 함수
    // ============================================================
    
    /// <summary>
    /// SetupTitleText()는 게임 제목 텍스트를 설정하는 함수입니다.
    /// Inspector에서 설정한 내용, 크기, 색상을 적용합니다.
    /// 그림자 효과도 자동으로 추가합니다.
    /// </summary>
    private void SetupTitleText()
    {
        // titleText가 연결되지 않았으면 함수를 끝냅니다
        if (titleText == null)
        {
            Debug.LogWarning("[TitleUIManager] Title Text가 연결되지 않았습니다.");
            return;
        }

        // 제목 내용을 설정합니다
        titleText.text = titleContent;
        
        // 폰트 크기를 설정합니다
        titleText.fontSize = titleFontSize;
        
        // 색상을 설정합니다
        titleText.color = titleColor;
        
        // 정렬을 가운데로 설정합니다
        titleText.alignment = TextAlignmentOptions.Center;
        
        // 그림자 효과를 자동으로 추가합니다
        SetupShadowEffect();
    }

    // ============================================================
    // 그림자 효과 설정 함수
    // ============================================================
    
    /// <summary>
    /// SetupShadowEffect()는 텍스트에 그림자 효과를 추가하는 함수입니다.
    /// </summary>
    private void SetupShadowEffect()
    {
        // 이미 그림자 컴포넌트가 있으면 새로 만들지 않습니다
        if (titleText.GetComponent<Shadow>() != null)
        {
            return;
        }

        // 그림자 컴포넌트를 추가합니다
        Shadow shadow = titleText.gameObject.AddComponent<Shadow>();
        
        // 그림자 색상을 설정합니다 (반투명 검정)
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        
        // 그림자 위치를 설정합니다 (오른쪽 아래로 살짝)
        shadow.effectDistance = new Vector2(3f, -3f);
    }

    // ============================================================
    // Inspector에서 변경할 수 있는 함수들
    // ============================================================
    
    /// <summary>
    /// ChangeBackgroundSprite()는 배경 이미지를 변경하는 함수입니다.
    /// </summary>
    public void ChangeBackgroundSprite(Sprite newSprite)
    {
        if (backgroundImage == null) return;
        backgroundImage.sprite = newSprite;
    }

    /// <summary>
    /// ChangeBackgroundColor()는 배경 색상을 변경하는 함수입니다.
    /// </summary>
    public void ChangeBackgroundColor(Color newColor)
    {
        if (backgroundImage == null) return;
        backgroundImage.color = newColor;
    }

    /// <summary>
    /// ChangeTitleContent()는 제목 내용을 변경하는 함수입니다.
    /// </summary>
    public void ChangeTitleContent(string newContent)
    {
        if (titleText == null) return;
        titleText.text = newContent;
    }

    /// <summary>
    /// ChangeTitleColor()는 제목 색상을 변경하는 함수입니다.
    /// </summary>
    public void ChangeTitleColor(Color newColor)
    {
        if (titleText == null) return;
        titleText.color = newColor;
    }
}
