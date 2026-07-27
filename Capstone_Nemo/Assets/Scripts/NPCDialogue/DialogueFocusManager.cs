using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DialogueFocusManager : MonoBehaviour
{
    public static DialogueFocusManager Instance;

    [Header("World Dimmer")]
    [SerializeField] private SpriteRenderer worldDimmerRenderer;
    [SerializeField] private float fadeDuration = 0.25f;

    [Range(0f, 1f)]
    [SerializeField] private float targetAlpha = 0.6f;

    [Header("Sorting Layers")]
    [SerializeField]
    private string dimmerSortingLayerName = "DialogueDimmer";

    [SerializeField]
    private string focusSortingLayerName = "DialogueFocus";

    [SerializeField]
    private int dimmerSortingOrder = 0;

    private Coroutine fadeCoroutine;
    private bool isFocused;

    /*
     * 플레이어와 NPC가 사용하던 원래 Sorting Layer를
     * 대화 종료 후 복구하기 위한 정보
     */
    private struct SpriteRendererBackup
    {
        public SpriteRenderer renderer;
        public int originalSortingLayerId;
    }

    private struct SortingGroupBackup
    {
        public SortingGroup sortingGroup;
        public int originalSortingLayerId;
    }

    private readonly List<SpriteRendererBackup>
        playerRendererBackups = new List<SpriteRendererBackup>();

    private readonly List<SpriteRendererBackup>
        npcRendererBackups = new List<SpriteRendererBackup>();

    private readonly List<SortingGroupBackup>
        playerGroupBackups = new List<SortingGroupBackup>();

    private readonly List<SortingGroupBackup>
        npcGroupBackups = new List<SortingGroupBackup>();

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

        InitializeDimmer();
    }

    private void InitializeDimmer()
    {
        if (worldDimmerRenderer == null)
        {
            Debug.LogWarning(
                "[DialogueFocusManager] World Dimmer Renderer가 " +
                "연결되지 않았습니다.",
                gameObject
            );

            return;
        }

        worldDimmerRenderer.sortingLayerName =
            dimmerSortingLayerName;

        worldDimmerRenderer.sortingOrder =
            dimmerSortingOrder;

        Color color = worldDimmerRenderer.color;
        color.a = 0f;
        worldDimmerRenderer.color = color;

        worldDimmerRenderer.gameObject.SetActive(false);
    }

    public void BeginFocus(
        GameObject playerObj,
        GameObject npcObj
    )
    {
        /*
         * 이전 포커스가 남아 있다면 먼저 원래 상태로 복구
         */
        if (isFocused)
        {
            EndFocusImmediate();
        }

        ApplyFocusSortingLayer(
            playerObj,
            playerRendererBackups,
            playerGroupBackups
        );

        ApplyFocusSortingLayer(
            npcObj,
            npcRendererBackups,
            npcGroupBackups
        );

        if (worldDimmerRenderer != null)
        {
            /*
             * 인스펙터에서 값이 변경됐을 수도 있으므로
             * 대화를 시작할 때 다시 적용
             */
            worldDimmerRenderer.sortingLayerName =
                dimmerSortingLayerName;

            worldDimmerRenderer.sortingOrder =
                dimmerSortingOrder;

            worldDimmerRenderer.gameObject.SetActive(true);

            StartFade(
                GetCurrentAlpha(),
                targetAlpha
            );
        }

        isFocused = true;
    }

    /*
     * 대상 오브젝트와 모든 자식의 Sorting Layer를
     * DialogueFocus로 변경
     */
    private void ApplyFocusSortingLayer(
        GameObject target,
        List<SpriteRendererBackup> rendererBackups,
        List<SortingGroupBackup> groupBackups
    )
    {
        rendererBackups.Clear();
        groupBackups.Clear();

        if (target == null)
        {
            return;
        }

        /*
         * SortingGroup을 사용하는 플레이어/NPC도 처리
         */
        SortingGroup[] sortingGroups =
            target.GetComponentsInChildren<SortingGroup>(true);

        foreach (SortingGroup sortingGroup in sortingGroups)
        {
            if (sortingGroup == null)
            {
                continue;
            }

            groupBackups.Add(new SortingGroupBackup
            {
                sortingGroup = sortingGroup,
                originalSortingLayerId =
                    sortingGroup.sortingLayerID
            });

            sortingGroup.sortingLayerName =
                focusSortingLayerName;
        }

        /*
         * 일반 SpriteRenderer 및 여러 개의 신체 스프라이트,
         * 장식 스프라이트 등을 모두 처리
         */
        SpriteRenderer[] renderers =
            target.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            /*
             * 혹시 Dimmer가 대상의 자식으로 들어간 경우에는
             * 포커스 레이어로 변경하지 않음
             */
            if (renderer == worldDimmerRenderer)
            {
                continue;
            }

            rendererBackups.Add(new SpriteRendererBackup
            {
                renderer = renderer,
                originalSortingLayerId =
                    renderer.sortingLayerID
            });

            renderer.sortingLayerName =
                focusSortingLayerName;
        }
    }

    public void EndFocus()
    {
        if (!isFocused)
        {
            return;
        }

        isFocused = false;

        if (worldDimmerRenderer != null)
        {
            /*
             * 검은 오버레이가 사라질 때까지는
             * 플레이어와 NPC를 포커스 레이어에 유지
             */
            StartFade(
                GetCurrentAlpha(),
                0f,
                () =>
                {
                    RestoreAllSortingLayers();

                    if (worldDimmerRenderer != null)
                    {
                        worldDimmerRenderer.gameObject
                            .SetActive(false);
                    }
                }
            );
        }
        else
        {
            RestoreAllSortingLayers();
        }
    }

    public void EndFocusImmediate()
    {
        isFocused = false;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        RestoreAllSortingLayers();

        if (worldDimmerRenderer != null)
        {
            Color color = worldDimmerRenderer.color;
            color.a = 0f;
            worldDimmerRenderer.color = color;

            worldDimmerRenderer.gameObject.SetActive(false);
        }
    }

    private void RestoreAllSortingLayers()
    {
        RestoreSpriteRendererLayers(
            playerRendererBackups
        );

        RestoreSpriteRendererLayers(
            npcRendererBackups
        );

        RestoreSortingGroupLayers(
            playerGroupBackups
        );

        RestoreSortingGroupLayers(
            npcGroupBackups
        );
    }

    private void RestoreSpriteRendererLayers(
        List<SpriteRendererBackup> backups
    )
    {
        foreach (SpriteRendererBackup backup in backups)
        {
            if (backup.renderer == null)
            {
                continue;
            }

            backup.renderer.sortingLayerID =
                backup.originalSortingLayerId;
        }

        backups.Clear();
    }

    private void RestoreSortingGroupLayers(
        List<SortingGroupBackup> backups
    )
    {
        foreach (SortingGroupBackup backup in backups)
        {
            if (backup.sortingGroup == null)
            {
                continue;
            }

            backup.sortingGroup.sortingLayerID =
                backup.originalSortingLayerId;
        }

        backups.Clear();
    }

    private float GetCurrentAlpha()
    {
        if (worldDimmerRenderer == null)
        {
            return 0f;
        }

        return worldDimmerRenderer.color.a;
    }

    private void StartFade(
        float from,
        float to,
        System.Action onComplete = null
    )
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(
            FadeRoutine(from, to, onComplete)
        );
    }

    private IEnumerator FadeRoutine(
        float from,
        float to,
        System.Action onComplete
    )
    {
        if (worldDimmerRenderer == null)
        {
            fadeCoroutine = null;
            onComplete?.Invoke();
            yield break;
        }

        if (fadeDuration <= 0f)
        {
            SetDimmerAlpha(to);

            fadeCoroutine = null;
            onComplete?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / fadeDuration
            );

            float alpha = Mathf.Lerp(
                from,
                to,
                progress
            );

            SetDimmerAlpha(alpha);

            yield return null;
        }

        SetDimmerAlpha(to);

        fadeCoroutine = null;
        onComplete?.Invoke();
    }

    private void SetDimmerAlpha(float alpha)
    {
        if (worldDimmerRenderer == null)
        {
            return;
        }

        Color color = worldDimmerRenderer.color;
        color.a = alpha;
        worldDimmerRenderer.color = color;
    }
}
