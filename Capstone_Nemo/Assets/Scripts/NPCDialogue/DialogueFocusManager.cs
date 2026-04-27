using System.Collections;
using UnityEngine;

public class DialogueFocusManager : MonoBehaviour
{
    public static DialogueFocusManager Instance;

    [Header("World Dimmer")]
    [SerializeField] private SpriteRenderer worldDimmerRenderer;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float targetAlpha = 0.6f;

    [Header("Sorting")]
    [SerializeField] private string dimmerSortingLayerName = "Obj";
    [SerializeField] private int dimmerSortingOrder = 100;
    [SerializeField] private int focusSortingOrder = 200;
    [SerializeField] private int focusSortingOffset = 200;

    private Coroutine fadeCoroutine;

    private YSort playerYSort;
    private YSort npcYSort;

    private bool isFocused = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (worldDimmerRenderer != null)
        {
            Color c = worldDimmerRenderer.color;
            c.a = 0f;
            worldDimmerRenderer.color = c;
            worldDimmerRenderer.gameObject.SetActive(false);
            worldDimmerRenderer.sortingLayerName = dimmerSortingLayerName;
            worldDimmerRenderer.sortingOrder = dimmerSortingOrder;
        }
    }

    public void BeginFocus(GameObject playerObj, GameObject npcObj)
    {
        if (isFocused)
            EndFocusImmediate();

        if (playerObj != null)
            playerYSort = playerObj.GetComponentInChildren<YSort>();

        if (npcObj != null)
            npcYSort = npcObj.GetComponentInChildren<YSort>();

        int playerCurrentOrder = GetCurrentSortingOrder(playerObj);
        int npcCurrentOrder = GetCurrentSortingOrder(npcObj);

        if (playerYSort != null)
            playerYSort.SetSortingLock(true, playerCurrentOrder + focusSortingOffset);

        if (npcYSort != null)
            npcYSort.SetSortingLock(true, npcCurrentOrder + focusSortingOffset);

        if (worldDimmerRenderer != null)
        {
            worldDimmerRenderer.gameObject.SetActive(true);
            StartFade(GetCurrentAlpha(), targetAlpha);
        }

        isFocused = true;
    }

    public void EndFocus()
    {
        if (playerYSort != null)
            playerYSort.SetSortingLock(false);

        if (npcYSort != null)
            npcYSort.SetSortingLock(false);

        playerYSort = null;
        npcYSort = null;

        if (worldDimmerRenderer != null)
        {
            StartFade(GetCurrentAlpha(), 0f, () =>
            {
                worldDimmerRenderer.gameObject.SetActive(false);
            });
        }

        isFocused = false;
    }

    public void EndFocusImmediate()
    {
        if (playerYSort != null)
            playerYSort.SetSortingLock(false);

        if (npcYSort != null)
            npcYSort.SetSortingLock(false);

        playerYSort = null;
        npcYSort = null;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (worldDimmerRenderer != null)
        {
            Color c = worldDimmerRenderer.color;
            c.a = 0f;
            worldDimmerRenderer.color = c;
            worldDimmerRenderer.gameObject.SetActive(false);
        }

        isFocused = false;
    }

    private float GetCurrentAlpha()
    {
        return worldDimmerRenderer != null ? worldDimmerRenderer.color.a : 0f;
    }

    private void StartFade(float from, float to, System.Action onComplete = null)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(from, to, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, System.Action onComplete)
    {
        if (worldDimmerRenderer == null)
            yield break;

        float t = 0f;
        Color c = worldDimmerRenderer.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / fadeDuration);
            c.a = Mathf.Lerp(from, to, lerp);
            worldDimmerRenderer.color = c;
            yield return null;
        }

        c.a = to;
        worldDimmerRenderer.color = c;
        fadeCoroutine = null;
        onComplete?.Invoke();
    }

    private int GetCurrentSortingOrder(GameObject obj)
    {
        if (obj == null) return 0;

        YSort ySort = obj.GetComponentInChildren<YSort>();
        if (ySort != null)
        {
            SpriteRenderer sr = ySort.GetComponent<SpriteRenderer>();
            if (sr != null)
                return sr.sortingOrder;
        }

        SpriteRenderer directSr = obj.GetComponentInChildren<SpriteRenderer>();
        if (directSr != null)
            return directSr.sortingOrder;

        return 0;
    }
}
