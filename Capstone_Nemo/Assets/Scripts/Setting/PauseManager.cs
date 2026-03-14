using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject currentSettingsPanel;

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

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private bool CanUsePause()
    {
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSettingsPanel = null;

        ForceResumeWithoutPanel();
    }

    public void RegisterSettingsPanel(GameObject panel)
    {
        currentSettingsPanel = panel;

        if (currentSettingsPanel != null)
            currentSettingsPanel.SetActive(false);
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        if (currentSettingsPanel != null)
        {
            currentSettingsPanel.SetActive(true);
            currentSettingsPanel.transform.SetAsLastSibling();
        }

    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        if (currentSettingsPanel != null)
            currentSettingsPanel.SetActive(false);
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void ForceResumeWithoutPanel()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }
}
