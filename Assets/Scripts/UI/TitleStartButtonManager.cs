using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleStartButtonManager : MonoBehaviour
{
    private const string DefaultGameSceneName = "GameScene";

    [Header("Start Button")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private string buttonLabel = "START";
    [SerializeField] private string gameSceneName = DefaultGameSceneName;

    [Header("Button Colors")]
    [SerializeField] private Color normalColor = new(0.3f, 0.69f, 0.31f, 1f);
    [SerializeField] private Color hoverColor = new(0.4f, 0.73f, 0.42f, 1f);
    [SerializeField] private Color pressedColor = new(0.2f, 0.59f, 0.21f, 1f);

    private void Awake()
    {
        NormalizeGameSceneName();
        SetupStartButton();
    }

    private void Start()
    {
        SelectStartButton();
    }

    private void OnValidate()
    {
        NormalizeGameSceneName();
    }

    private void SetupStartButton()
    {
        if (startButton == null)
        {
            Debug.LogWarning("[TitleStartButtonManager] Start Button is not assigned.", this);
            return;
        }

        var colors = startButton.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = pressedColor;
        startButton.colors = colors;

        if (buttonText != null)
        {
            buttonText.text = buttonLabel;
        }

        startButton.onClick.RemoveListener(OnStartButtonClicked);
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        NormalizeGameSceneName();
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[TitleStartButtonManager] Game scene name is empty.", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void NormalizeGameSceneName()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName) || gameSceneName == "SampleScene")
        {
            gameSceneName = DefaultGameSceneName;
        }
    }

    private void SelectStartButton()
    {
        if (startButton == null || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    public void ChangeButtonText(string newText)
    {
        if (buttonText == null)
        {
            return;
        }

        buttonText.text = newText;
    }

    public void ChangeButtonColor(Color newColor)
    {
        if (startButton == null)
        {
            return;
        }

        var colors = startButton.colors;
        colors.normalColor = newColor;
        startButton.colors = colors;
    }

    public void ChangeGameSceneName(string newSceneName)
    {
        gameSceneName = newSceneName;
        NormalizeGameSceneName();
    }
}
