using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.35f;

    [SerializeField] private bool isPaused = false;
    private bool isSceneChanging = false;

    public bool IsPaused => isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("PauseManager");
        go.AddComponent<PauseManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEscapeInput();
    }

    private void HandleEscapeInput()
    {
        if (isSceneChanging)
            return;

        if (!CanUsePause())
            return;

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettingsAndBackToMenu();
            return;
        }

        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            ResumeGame();
            return;
        }

        PauseGame();
    }

    private bool CanUsePause()
    {
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pauseMenuPanel = null;
        settingsPanel = null;
        ForceResumeWithoutPanel();

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }

        isSceneChanging = false;
    }

    public void RegisterPauseUI(GameObject menuPanel, GameObject settingPanel)
    {
        pauseMenuPanel = menuPanel;
        settingsPanel = settingPanel;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void RegisterFadeImage(Image image)
    {
        fadeImage = image;

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }
    }

    public void PauseGame()
    {
        if (isPaused || isSceneChanging)
            return;

        if (pauseMenuPanel == null)
        {
            Debug.LogWarning("[PauseManager] pauseMenuPanel is null. Pause cancelled.");
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;

        pauseMenuPanel.SetActive(true);
        pauseMenuPanel.transform.SetAsLastSibling();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        if (!isPaused || isSceneChanging)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
        }
    }

    public void CloseSettingsAndBackToMenu()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            pauseMenuPanel.transform.SetAsLastSibling();
        }
    }

    public void GoToMainScene(string sceneName)
    {
        if (isSceneChanging)
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[PauseManager] sceneName is empty.");
            return;
        }

        StartCoroutine(GoToMainSceneRoutine(sceneName));
    }

    private IEnumerator GoToMainSceneRoutine(string sceneName)
    {
        isSceneChanging = true;

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.transform.SetAsLastSibling();

            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / fadeDuration);

                c.a = a;
                fadeImage.color = c;

                yield return null;
            }

            c.a = 1f;
            fadeImage.color = c;
        }

        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        Application.Quit();
    }

    public void ForceResumeWithoutPanel()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }
}