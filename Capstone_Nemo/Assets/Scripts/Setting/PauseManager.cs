using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [SerializeField] private bool isPaused = false;

    public bool IsPaused => isPaused;

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
        {
            HandleEscapeInput();
        }
    }

    private void HandleEscapeInput()
    {
        if (!CanUsePause())
            return;

        //설정창 열려 있으면 설정창 닫고 메뉴로 돌아감
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettingsAndBackToMenu();
            return;
        }

        //메뉴가 열려 있으면 닫고 게임 재개
        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            ResumeGame();
            return;
        }

        //둘 다 안 열려 있으면 메뉴 열기
        PauseGame();
    }

    private bool CanUsePause()
    {
        //필요하면 나중에 추가
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pauseMenuPanel = null;
        settingsPanel = null;
        ForceResumeWithoutPanel();
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

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            pauseMenuPanel.transform.SetAsLastSibling();
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        if (!isPaused)
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
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

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
