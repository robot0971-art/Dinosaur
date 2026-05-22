using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면의 맵 선택 UI 모양을 관리하는 스크립트입니다.
///
/// [역할]
/// - GREENLAND 양쪽 화살표 이미지가 Play 모드에서도 보이게 합니다.
/// - 화살표 색상과 투명도를 Inspector에서 쉽게 조절할 수 있게 합니다.
///
/// [붙이는 위치]
/// - TitleCanvas 오브젝트에 붙입니다.
///
/// [연결할 것]
/// - leftArrowImage: LeftArrowButton의 Image 컴포넌트
/// - rightArrowImage: RightArrowButton의 Image 컴포넌트
/// </summary>
[ExecuteAlways]
public class TitleMapSelectVisualManager : MonoBehaviour
{
    [Header("맵 선택 화살표 이미지")]
    [Tooltip("왼쪽 화살표 버튼의 Image 컴포넌트를 연결하세요")]
    [SerializeField] private Image leftArrowImage;

    [Tooltip("오른쪽 화살표 버튼의 Image 컴포넌트를 연결하세요")]
    [SerializeField] private Image rightArrowImage;

    [Header("화살표 색상")]
    [Tooltip("화살표 이미지 색상입니다. Alpha가 1이어야 화면에 보입니다")]
    [SerializeField] private Color arrowColor = Color.white;

    /// <summary>
    /// Awake는 Play를 눌렀을 때 자동으로 한 번 실행됩니다.
    /// 여기서 화살표 이미지를 보이게 설정합니다.
    /// </summary>
    private void Awake()
    {
        ApplyArrowVisuals();
    }

    /// <summary>
    /// OnValidate는 Inspector 값을 바꿀 때 자동으로 실행됩니다.
    /// Play 전에도 Scene/Game 창에서 바로 확인할 수 있게 해줍니다.
    /// </summary>
    private void OnValidate()
    {
        ApplyArrowVisuals();
    }

    /// <summary>
    /// 화살표 이미지가 보이도록 설정하는 함수입니다.
    /// Image가 꺼져 있거나 투명하면 Play 모드에서 보이지 않을 수 있습니다.
    /// </summary>
    private void ApplyArrowVisuals()
    {
        SetupArrowImage(leftArrowImage);
        SetupArrowImage(rightArrowImage);
    }

    /// <summary>
    /// 화살표 하나를 보이게 설정합니다.
    /// 같은 작업을 왼쪽/오른쪽에 반복해야 해서 함수로 나누었습니다.
    /// </summary>
    /// <param name="arrowImage">설정할 화살표 Image 컴포넌트입니다.</param>
    private void SetupArrowImage(Image arrowImage)
    {
        if (arrowImage == null)
        {
            return;
        }

        // Image 컴포넌트가 꺼져 있으면 화면에 안 보이므로 켭니다.
        arrowImage.enabled = true;

        // Preserve Aspect는 이미지 비율을 유지해서 찌그러지지 않게 합니다.
        arrowImage.preserveAspect = true;

        // Alpha가 0이면 완전 투명입니다. 여기서는 Inspector 색상을 그대로 적용합니다.
        arrowImage.color = arrowColor;
    }
}
