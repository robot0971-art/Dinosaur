using UnityEngine;
using UnityEngine.UI;

public class GameHudResumeButton : MonoBehaviour
{
    [Header("Resume Button")]
    [SerializeField] private Button resumeButton;

    [Header("Editor Preview")]
    [SerializeField] private bool showOnStart;

    private bool isPaused;

    private void Awake()
    {
        SetupButtonListener();
    }

    private void Start()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetResumeButtonVisible(showOnStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
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

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
            return;
        }

        PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetResumeButtonVisible(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetResumeButtonVisible(false);
    }

    private void SetResumeButtonVisible(bool visible)
    {
        if (resumeButton != null && resumeButton.gameObject.activeSelf != visible)
        {
            resumeButton.gameObject.SetActive(visible);
        }
    }

    private void SetupButtonListener()
    {
        if (resumeButton == null)
        {
            return;
        }

        resumeButton.interactable = true;

        if (resumeButton.targetGraphic != null)
        {
            resumeButton.targetGraphic.raycastTarget = true;
        }

        resumeButton.onClick.RemoveListener(TogglePause);
        resumeButton.onClick.AddListener(TogglePause);
    }

    private void RemoveButtonListener()
    {
        if (resumeButton == null)
        {
            return;
        }

        resumeButton.onClick.RemoveListener(TogglePause);
    }
}
