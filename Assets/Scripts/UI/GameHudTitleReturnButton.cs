using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameHudTitleReturnButton : MonoBehaviour
{
    [Header("Title Button")]
    [SerializeField] private Button titleButton;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("Editor Preview")]
    [SerializeField] private bool showOnStart;

    private bool isVisible;

    private void Awake()
    {
        SetupButtonListener();
    }

    private void Start()
    {
        isVisible = showOnStart;
        SetTitleButtonVisible(showOnStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleVisibility();
        }
    }

    private void OnEnable()
    {
        SetupButtonListener();
    }

    private void OnDisable()
    {
        RemoveButtonListener();
    }

    public void ToggleVisibility()
    {
        if (isVisible)
        {
            HideTitleButton();
            return;
        }

        ShowTitleButton();
    }

    public void ShowTitleButton()
    {
        isVisible = true;
        SetTitleButtonVisible(true);
    }

    public void HideTitleButton()
    {
        isVisible = false;
        SetTitleButtonVisible(false);
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(titleSceneName))
        {
            Debug.LogWarning("[GameHudTitleReturnButton] Title scene name is empty.", this);
            return;
        }

        SceneManager.LoadScene(titleSceneName);
    }

    private void SetTitleButtonVisible(bool visible)
    {
        if (titleButton != null && titleButton.gameObject.activeSelf != visible)
        {
            titleButton.gameObject.SetActive(visible);
        }
    }

    private void SetupButtonListener()
    {
        if (titleButton == null)
        {
            return;
        }

        titleButton.interactable = true;

        if (titleButton.targetGraphic != null)
        {
            titleButton.targetGraphic.raycastTarget = true;
        }

        titleButton.onClick.RemoveListener(ReturnToTitle);
        titleButton.onClick.AddListener(ReturnToTitle);
    }

    private void RemoveButtonListener()
    {
        if (titleButton == null)
        {
            return;
        }

        titleButton.onClick.RemoveListener(ReturnToTitle);
    }
}
