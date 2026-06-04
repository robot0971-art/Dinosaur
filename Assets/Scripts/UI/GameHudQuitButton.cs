using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameHudQuitButton : MonoBehaviour
{
    [Header("Quit Button")]
    [SerializeField] private Button quitButton;

    [Header("Editor Preview")]
    [SerializeField] private bool showOnStart;

    private void Awake()
    {
        SetupButtonListener();
    }

    private void Start()
    {
        SetVisible(showOnStart);
    }

    private void OnEnable()
    {
        SetupButtonListener();
    }

    private void OnDisable()
    {
        RemoveButtonListener();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        SetVisible(showOnStart);
    }

    public void ShowQuitButton()
    {
        SetVisible(true);
    }

    public void HideQuitButton()
    {
        SetVisible(false);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetVisible(bool visible)
    {
        if (quitButton != null && quitButton.gameObject.activeSelf != visible)
        {
            quitButton.gameObject.SetActive(visible);
        }
    }

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

    private void RemoveButtonListener()
    {
        if (quitButton == null)
        {
            return;
        }

        quitButton.onClick.RemoveListener(QuitGame);
    }
}
