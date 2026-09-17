using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene-local manager. Keep its GameObject active and OUTSIDE panelRoot.
[DefaultExecutionOrder(-200)]
public sealed class InspectionUIManager : MonoBehaviour
{
    public static InspectionUIManager Instance { get; private set; }
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform slidingPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;
    [SerializeField, Min(0f)] private float slideDistance = 600f;
    [SerializeField, Min(0.01f)] private float slideDuration = 0.25f;
    [Tooltip("Assign active-only UI roots, e.g. quest/shop/inventory panels.")]
    [SerializeField] private GameObject[] blockingPanels;

    private Vector2 restPosition;
    private InspectableObject owner;
    private Coroutine animationRoutine;
    private bool busy;
    private bool closing;
    private int consumedFrame = -1;
    public bool IsOpen => busy;

    // Use at NPC entry points. Includes closing animation and its completion frame.
    public static bool BlocksNpcInteraction => Instance != null &&
        (Instance.busy || Instance.consumedFrame == Time.frameCount);

    // Use at OTHER world interaction entry points; inspection reserves nearby input.
    public static bool BlocksOtherWorldInteraction => BlocksNpcInteraction ||
        (Instance != null && Instance.isActiveAndEnabled && !Instance.IsExternallyBlocked() &&
         Instance.FindNearest() != null);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        if (panelRoot == null || slidingPanel == null || titleText == null ||
            bodyText == null || closeButton == null || transform.IsChildOf(panelRoot.transform))
        {
            Debug.LogError("InspectionUIManager: assign UI references; manager must be outside panelRoot.", this);
            enabled = false;
            return;
        }
        Canvas.ForceUpdateCanvases();
        restPosition = slidingPanel.anchoredPosition;
        panelRoot.SetActive(false);
        closeButton.onClick.AddListener(Close);
    }

    private bool IsExternallyBlocked()
    {
        // Existing NPC system takes priority when both ranges overlap.
        if (NPCInteractable.BlocksOtherWorldInteraction) return true;
        if (blockingPanels != null)
            foreach (GameObject panel in blockingPanels)
                if (panel != null && panel.activeInHierarchy) return true;
        return false;
    }

    private InspectableObject FindNearest()
    {
        InspectableObject best = null;
        float shortest = float.PositiveInfinity;
        foreach (InspectableObject item in InspectableObject.All)
        {
            if (item == null || !item.IsInRange) continue;
            float distance = item.DistanceSquared;
            if (distance < shortest || (distance == shortest &&
                (best == null || item.GetInstanceID() < best.GetInstanceID())))
            { best = item; shortest = distance; }
        }
        return best;
    }

    private void Update()
    {
        if (busy)
        {
            if (owner == null || !owner.IsInRange ||
                (NPCDialogueUIManager.Instance != null && NPCDialogueUIManager.Instance.IsDialogueOpen))
                Close();
            else if (Input.GetKeyDown(KeyCode.E)) Close();
            return;
        }
        if (consumedFrame != Time.frameCount && !IsExternallyBlocked() && Input.GetKeyDown(KeyCode.E))
            TryOpen(FindNearest());
    }

    private void LateUpdate()
    {
        bool show = !busy && consumedFrame != Time.frameCount && !IsExternallyBlocked();
        foreach (InspectableObject item in InspectableObject.All)
            if (item != null) item.SetHint(show && item.IsInRange);
    }

    public bool TryOpen(InspectableObject item)
    {
        if (!isActiveAndEnabled || busy || consumedFrame == Time.frameCount ||
            item == null || !item.IsInRange || IsExternallyBlocked()) return false;
        busy = true;
        closing = false;
        owner = item;
        consumedFrame = Time.frameCount;
        titleText.text = item.Title;
        bodyText.text = item.Description;
        slidingPanel.anchoredPosition = restPosition + Vector2.down * slideDistance;
        panelRoot.SetActive(true);
        foreach (InspectableObject candidate in InspectableObject.All)
            if (candidate != null) candidate.SetHint(false);
        animationRoutine = StartCoroutine(Slide(restPosition, false));
        return true;
    }

    public void CloseIfOwner(InspectableObject item) { if (owner == item) Close(); }
    public void Close()
    {
        if (!busy || closing) return;
        closing = true;
        consumedFrame = Time.frameCount;
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(Slide(restPosition + Vector2.down * slideDistance, true));
    }
    private IEnumerator Slide(Vector2 destination, bool hide)
    {
        Vector2 start = slidingPanel.anchoredPosition;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, slideDuration);
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            slidingPanel.anchoredPosition = Vector2.LerpUnclamped(start, destination, 1f - Mathf.Pow(1f - t, 3f));
        }
        slidingPanel.anchoredPosition = destination;
        animationRoutine = null;
        if (hide)
        {
            panelRoot.SetActive(false);
            slidingPanel.anchoredPosition = restPosition;
            busy = false;
            closing = false;
            owner = null;
            consumedFrame = Time.frameCount;
        }
    }
    private void OnDisable()
    {
        if (Instance != this) return;
        StopAllCoroutines();
        animationRoutine = null;
        if (panelRoot != null) panelRoot.SetActive(false);
        if (slidingPanel != null) slidingPanel.anchoredPosition = restPosition;
        busy = closing = false;
        owner = null;
        consumedFrame = Time.frameCount;
        foreach (InspectableObject item in InspectableObject.All)
            if (item != null) item.SetHint(false);
    }
    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (Instance == this) Instance = null;
    }
}
