using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SaveSelectOpenAnimator : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private RectTransform tabButtonsRoot;
    [SerializeField] private float tabStartOffsetX = -25f;
    [SerializeField] private float tabMoveDuration = 0.22f;

    [Header("File Panel")]
    [SerializeField] private GameObject filePanel;
    [SerializeField] private float panelStartDelay = 0.08f;
    [SerializeField] private float panelFadeDuration = 0.22f;
    [SerializeField] private float delayBeforeSlots = 0.12f;

    [Header("Save Slots - 위에서부터 순서대로")]
    [SerializeField]
    private RectTransform[] saveSlots =
        new RectTransform[3];

    [SerializeField] private LayoutGroup slotsLayoutGroup;

    [SerializeField] private float slotStartOffsetY = 20f;
    [SerializeField] private float slotMoveDuration = 0.21f;
    [SerializeField] private float slotStaggerDelay = 0.12f;

    private CanvasGroup tabCanvasGroup;
    private CanvasGroup panelCanvasGroup;
    private CanvasGroup[] slotCanvasGroups;

    private Vector2 tabFinalPosition;

    private bool initialized;
    private bool playing;

    [Header("Slot Contents - 슬롯과 같은 순서")]
    [SerializeField]
    private CanvasGroup[] slotContents = new CanvasGroup[3];

    [SerializeField]
    private float slotContentFadeDuration = 0.1f;

    [SerializeField]
    [Range(0f, 1f)]
    private float slotContentStartPoint = 0.78f;

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        if (tabButtonsRoot != null)
        {
            tabCanvasGroup =
                GetOrAddCanvasGroup(tabButtonsRoot.gameObject);

            tabFinalPosition = tabButtonsRoot.anchoredPosition;
        }

        if (filePanel != null)
        {
            panelCanvasGroup = GetOrAddCanvasGroup(filePanel);
        }

        slotCanvasGroups = new CanvasGroup[saveSlots.Length];

        for (int i = 0; i < saveSlots.Length; i++)
        {
            if (saveSlots[i] == null)
            {
                Debug.LogError(
                    $"Save Slots의 {i}번 요소가 연결되지 않았습니다.",
                    this
                );

                continue;
            }

            slotCanvasGroups[i] =
                GetOrAddCanvasGroup(saveSlots[i].gameObject);

            if (i >= slotContents.Length ||
                slotContents[i] == null)
            {
                Debug.LogError(
                    $"Slot Contents의 {i}번 요소가 연결되지 않았습니다.",
                    this
                );

                continue;
            }

            if (!slotContents[i].transform.IsChildOf(
                    saveSlots[i]))
            {
                Debug.LogError(
                    $"Slot Contents의 {i}번 요소는 " +
                    $"해당 Save Slot의 자식이어야 합니다.",
                    slotContents[i]
                );
            }
        }
    }

    public void PrepareHidden()
    {
        Initialize();

        if (tabCanvasGroup != null)
        {
            tabCanvasGroup.alpha = 0f;
            tabCanvasGroup.interactable = true;
            tabCanvasGroup.blocksRaycasts = false;
        }

        for (int i = 0; i < slotCanvasGroups.Length; i++)
        {
            if (slotCanvasGroups[i] != null)
            {
                slotCanvasGroups[i].alpha = 0f;
                slotCanvasGroups[i].interactable = true;
                slotCanvasGroups[i].blocksRaycasts = false;
            }

            if (i < slotContents.Length && slotContents[i] != null)
            {
                slotContents[i].alpha = 0f;
                slotContents[i].interactable = true;
                slotContents[i].blocksRaycasts = false;
            }
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = false;
        }
    }

    public IEnumerator PlayOpen()
    {
        if (playing)
            yield break;

        playing = true;
        Initialize();
        PrepareHidden();

        if (filePanel != null)
            filePanel.SetActive(true);

        // 활성화 직후 다른 초기화 코드가 값을 바꿨을 수 있으므로
        // 같은 프레임에 다시 숨김 처리
        PrepareHidden();

        // 레이아웃이 최종 위치를 계산하도록 한 프레임 대기
        yield return null;

        Canvas.ForceUpdateCanvases();

        Vector2[] slotFinalPositions =
            new Vector2[saveSlots.Length];

        for (int i = 0; i < saveSlots.Length; i++)
        {
            if (saveSlots[i] != null)
            {
                slotFinalPositions[i] =
                    saveSlots[i].anchoredPosition;
            }
        }

        // 애니메이션 중 LayoutGroup이 슬롯 위치를
        // 원래 위치로 되돌리지 못하게 막음
        if (slotsLayoutGroup != null)
            slotsLayoutGroup.enabled = false;

        if (tabButtonsRoot != null)
        {
            tabButtonsRoot.anchoredPosition =
                tabFinalPosition +
                Vector2.right * tabStartOffsetX;
        }

        if (tabCanvasGroup != null)
            tabCanvasGroup.alpha = 0f;

        for (int i = 0; i < saveSlots.Length; i++)
        {
            if (saveSlots[i] == null)
                continue;

            saveSlots[i].anchoredPosition =
                slotFinalPositions[i] +
                Vector2.up * slotStartOffsetY;

            if (slotCanvasGroups[i] != null)
                slotCanvasGroups[i].alpha = 0f;

            if (i < slotContents.Length &&
                slotContents[i] != null)
            {
                slotContents[i].alpha = 0f;
            }
        }

        // 탭 이동 시작
        StartCoroutine(AnimateTabButtons());

        // 탭이 움직이는 도중 패널 페이드 시작
        yield return WaitRealtime(panelStartDelay);

        StartCoroutine(
            FadeCanvasGroup(
                panelCanvasGroup,
                0f,
                1f,
                panelFadeDuration
            )
        );

        // 패널 페이드가 끝나기 전에 슬롯 등장 시작
        yield return WaitRealtime(delayBeforeSlots);

        for (int i = 0; i < saveSlots.Length; i++)
        {
            if (saveSlots[i] == null)
                continue;

            CanvasGroup contentGroup =
                i < slotContents.Length
                    ? slotContents[i]
                    : null;

            StartCoroutine(
                AnimateSlot(
                    saveSlots[i],
                    slotCanvasGroups[i],
                    slotFinalPositions[i],
                    i * slotStaggerDelay,
                    contentGroup
                )
            );
        }

        float oneSlotDuration = Mathf.Max(
            slotMoveDuration,
            slotMoveDuration * slotContentStartPoint +
            slotContentFadeDuration
        );

        float totalSlotTime =
            oneSlotDuration +
            Mathf.Max(0, saveSlots.Length - 1) *
            slotStaggerDelay;

        yield return WaitRealtime(totalSlotTime);

        // 마지막 상태 확정
        for (int i = 0; i < saveSlots.Length; i++)
        {
            if (saveSlots[i] == null)
                continue;

            saveSlots[i].anchoredPosition =
                slotFinalPositions[i];

            if (slotCanvasGroups[i] != null)
            {
                slotCanvasGroups[i].alpha = 1f;
                slotCanvasGroups[i].interactable = true;
                slotCanvasGroups[i].blocksRaycasts = true;
            }

            if (i < slotContents.Length &&
                slotContents[i] != null)
            {
                slotContents[i].alpha = 1f;
                slotContents[i].interactable = true;
                slotContents[i].blocksRaycasts = true;
            }
        }

        if (tabCanvasGroup != null)
        {
            tabCanvasGroup.alpha = 1f;
            tabCanvasGroup.interactable = true;
            tabCanvasGroup.blocksRaycasts = true;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        playing = false;
    }

    private IEnumerator AnimateTabButtons()
    {
        if (tabButtonsRoot == null)
            yield break;

        Vector2 startPosition =
            tabFinalPosition + Vector2.right * tabStartOffsetX;

        float elapsed = 0f;

        while (elapsed < tabMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = tabMoveDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / tabMoveDuration);

            float eased = EaseOutCubic(progress);

            tabButtonsRoot.anchoredPosition =
                Vector2.Lerp(startPosition, tabFinalPosition, eased);

            if (tabCanvasGroup != null)
                tabCanvasGroup.alpha = eased;

            yield return null;
        }

        tabButtonsRoot.anchoredPosition = tabFinalPosition;

        if (tabCanvasGroup != null)
            tabCanvasGroup.alpha = 1f;
    }

    private IEnumerator AnimateSlot(
    RectTransform slot,
    CanvasGroup slotGroup,
    Vector2 finalPosition,
    float startDelay,
    CanvasGroup contentGroup)
    {
        yield return WaitRealtime(startDelay);

        Vector2 startPosition =
            finalPosition +
            Vector2.up * slotStartOffsetY;

        if (slotGroup != null)
        {
            slotGroup.alpha = 0f;
            slotGroup.interactable = true;
            slotGroup.blocksRaycasts = false;
        }

        if (contentGroup != null)
        {
            contentGroup.alpha = 0f;
            contentGroup.interactable = true;
            contentGroup.blocksRaycasts = false;
        }

        float contentStartTime =
            slotMoveDuration * slotContentStartPoint;

        float totalDuration = contentGroup != null
            ? Mathf.Max(
                slotMoveDuration,
                contentStartTime +
                slotContentFadeDuration
            )
            : slotMoveDuration;

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float moveProgress =
                slotMoveDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed / slotMoveDuration
                    );

            float moveEased =
                EaseOutCubic(moveProgress);

            slot.anchoredPosition = Vector2.Lerp(
                startPosition,
                finalPosition,
                moveEased
            );

            // 슬롯 배경은 이동 시작 직후 빠르게 선명해짐
            if (slotGroup != null)
            {
                slotGroup.alpha =
                    Mathf.Clamp01(moveProgress / 0.2f);
            }

            // 텍스트는 슬롯 이동 후반부터 자연스럽게 겹쳐 등장
            if (contentGroup != null)
            {
                float contentProgress;

                if (slotContentFadeDuration <= 0f)
                {
                    contentProgress =
                        elapsed >= contentStartTime
                            ? 1f
                            : 0f;
                }
                else
                {
                    contentProgress = Mathf.Clamp01(
                        (elapsed - contentStartTime) /
                        slotContentFadeDuration
                    );
                }

                contentGroup.alpha =
                    EaseOutCubic(contentProgress);
            }

            yield return null;
        }

        slot.anchoredPosition = finalPosition;

        if (slotGroup != null)
        {
            slotGroup.alpha = 1f;
            slotGroup.interactable = true;
            slotGroup.blocksRaycasts = true;
        }

        if (contentGroup != null)
        {
            contentGroup.alpha = 1f;
            contentGroup.interactable = true;
            contentGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup canvasGroup,
        float from,
        float to,
        float duration)
    {
        if (canvasGroup == null)
            yield break;

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = duration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / duration);

            canvasGroup.alpha = Mathf.Lerp(
    from,
    to,
    EaseOutCubic(progress)
);

            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator WaitRealtime(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup =
            target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = target.AddComponent<CanvasGroup>();

        return canvasGroup;
    }

    private float EaseOutCubic(float value)
    {
        return 1f - Mathf.Pow(1f - value, 3f);
    }

}
