// ============================================================
// GameHudQuitButton.cs
// ============================================================
// 이 스크립트가 하는 일:
// 게임오버 화면에서 "게임종료" 버튼을 관리합니다.
// 버튼을 누르면 빌드된 게임은 종료되고,
// Unity 에디터에서는 Play 모드가 꺼집니다.
// 기능 5: 게임 종료 버튼
// ============================================================

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// GameHudQuitButton
///
/// [이 스크립트가 필요한 이유]
/// 게임오버 화면에서 플레이어가 게임을 완전히 끄고 싶을 때 필요합니다.
///
/// [어디에 붙이나요?]
/// - GameOverCanvas 오브젝트에 붙입니다.
///
/// [Inspector에서 연결할 것]
/// - quitButton: 게임종료 버튼의 Button 컴포넌트
/// </summary>
public class GameHudQuitButton : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("게임종료 버튼 연결")]
    [Tooltip("게임종료 버튼의 Button 컴포넌트를 여기에 연결하세요")]
    [SerializeField] private Button quitButton;

    [Header("에디터 테스트")]
    [Tooltip("Play를 눌렀을 때 게임종료 버튼을 바로 보이게 할지 정합니다")]
    [SerializeField] private bool showOnStart;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 호출됩니다.
    /// 버튼 클릭 연결을 최대한 빨리 준비합니다.
    /// </summary>
    private void Awake()
    {
        SetupButtonListener();
    }

    /// <summary>
    /// Start()는 Play를 누른 뒤 한 번 호출됩니다.
    /// 테스트용으로 showOnStart 값에 따라 버튼을 보이거나 숨깁니다.
    /// </summary>
    private void Start()
    {
        SetVisible(showOnStart);
    }

    /// <summary>
    /// OnEnable()은 오브젝트가 켜질 때 호출됩니다.
    /// 버튼 클릭 연결이 풀리지 않게 다시 확인합니다.
    /// </summary>
    private void OnEnable()
    {
        SetupButtonListener();
    }

    /// <summary>
    /// OnDisable()은 오브젝트가 꺼질 때 호출됩니다.
    /// 버튼 클릭 연결을 정리합니다.
    /// </summary>
    private void OnDisable()
    {
        RemoveButtonListener();
    }

    /// <summary>
    /// OnValidate()는 Inspector 값이 바뀔 때 호출됩니다.
    /// 에디터에서 showOnStart 미리보기를 바로 반영합니다.
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        SetVisible(showOnStart);
    }

    // ============================================================
    // 공개 함수
    // ============================================================

    /// <summary>
    /// ShowQuitButton()은 게임종료 버튼을 보이게 합니다.
    /// </summary>
    public void ShowQuitButton()
    {
        SetVisible(true);
    }

    /// <summary>
    /// HideQuitButton()은 게임종료 버튼을 숨깁니다.
    /// </summary>
    public void HideQuitButton()
    {
        SetVisible(false);
    }

    /// <summary>
    /// QuitGame()은 게임종료 버튼을 누르면 호출됩니다.
    /// public 함수라서 Button의 OnClick에 직접 연결해서 테스트할 수도 있습니다.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("게임종료 버튼 클릭됨. 게임을 종료합니다.");

        Time.timeScale = 1f;

#if UNITY_EDITOR
        // Unity 에디터에서는 Play 모드를 중지합니다.
        EditorApplication.isPlaying = false;
#else
        // 빌드된 게임에서는 프로그램을 종료합니다.
        Application.Quit();
#endif
    }

    /// <summary>
    /// SetVisible()은 버튼을 보이거나 숨깁니다.
    /// true면 보이고, false면 숨겨집니다.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(visible);
        }
    }

    // ============================================================
    // 버튼 연결 함수
    // ============================================================

    /// <summary>
    /// SetupButtonListener()는 버튼 클릭 이벤트를 연결합니다.
    /// 버튼을 누르면 QuitGame()이 실행됩니다.
    /// </summary>
    private void SetupButtonListener()
    {
        if (quitButton == null)
        {
            return;
        }

        quitButton.interactable = true;

        if (quitButton.targetGraphic != null)
        {
            quitButton.targetGraphic.raycastTarget = true;
        }

        quitButton.onClick.RemoveListener(QuitGame);
        quitButton.onClick.AddListener(QuitGame);
    }

    /// <summary>
    /// RemoveButtonListener()는 버튼 클릭 이벤트 연결을 해제합니다.
    /// </summary>
    private void RemoveButtonListener()
    {
        if (quitButton == null)
        {
            return;
        }

        quitButton.onClick.RemoveListener(QuitGame);
    }
}
