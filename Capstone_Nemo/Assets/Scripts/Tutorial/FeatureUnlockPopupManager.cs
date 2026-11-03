using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FeatureUnlockPopupManager : MonoBehaviour
{
    [Header("Popup Objects (initially inactive)")]
    [SerializeField] private GameObject level4Popup;
    [SerializeField] private GameObject level7Popup;
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private Canvas popupCanvas;
    [SerializeField] private float showDelay = 0.5f;

    private PlayerManager lockedPlayer;
    private bool previousTimeFlow;
    private int showingLevel;
    private bool isShowing;

    private void Awake()
    {
        if (level4Popup != null) level4Popup.SetActive(false);
        if (level7Popup != null) level7Popup.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);
    }

    private IEnumerator Start()
    {
        // This component is placed in VillageScene, the scene entered after the cutscene.
        if (SceneManager.GetActiveScene().name != "VillageScene") yield break;
        yield return null;
        yield return new WaitForSecondsRealtime(showDelay);
        TryShowNext();
    }

    private void TryShowNext()
    {
        if (isShowing || UnlockManager.Instance == null) return;
        if (UnlockManager.Instance.HasPendingFeaturePopup(4) && level4Popup != null)
            Show(4, level4Popup);
        else if (UnlockManager.Instance.HasPendingFeaturePopup(7) && level7Popup != null)
            Show(7, level7Popup);
    }

    private void Show(int level, GameObject popup)
    {
        if (popupCanvas != null) popupCanvas.sortingOrder = 500;
        showingLevel = level;
        isShowing = true;
        if (TimeManager.Instance != null)
        {
            previousTimeFlow = TimeManager.Instance.isTimeFlow;
            TimeManager.Instance.SetTimeFlow(false);
        }
        lockedPlayer = FindObjectOfType<PlayerManager>();
        if (lockedPlayer != null) lockedPlayer.SetActionLocked(true);
        if (dimBackground != null) dimBackground.SetActive(true);
        popup.SetActive(true);
    }

    public void ClosePopup()
    {
        if (!isShowing) return;
        if (level4Popup != null) level4Popup.SetActive(false);
        if (level7Popup != null) level7Popup.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);
        UnlockManager.Instance?.MarkFeaturePopupShown(showingLevel);
        isShowing = false;
        showingLevel = 0;
        if (lockedPlayer != null) lockedPlayer.SetActionLocked(false);
        lockedPlayer = null;
        if (TimeManager.Instance != null) TimeManager.Instance.SetTimeFlow(previousTimeFlow);
        TryShowNext();
    }

    private void OnDisable()
    {
        if (!isShowing) return;
        if (lockedPlayer != null) lockedPlayer.SetActionLocked(false);
        if (TimeManager.Instance != null) TimeManager.Instance.SetTimeFlow(previousTimeFlow);
        isShowing = false;
    }
}
