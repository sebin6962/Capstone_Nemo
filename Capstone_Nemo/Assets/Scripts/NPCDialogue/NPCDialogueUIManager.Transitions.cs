using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Same class as NPCDialogueUIManager.cs; do not attach this file separately.
public partial class NPCDialogueUIManager
{
    [Serializable]
    private class DialogueHudTarget
    {
        public RectTransform root;
        public bool moveUp;
        [NonSerialized] public UnityEngine.Object sceneOwner;
    }

    [Header("Dialogue HUD Transition")]
    [SerializeField] private List<DialogueHudTarget> dialogueHudTargets = new();
    [SerializeField, Min(0f)] private float dialogueLeadIn = 0.15f;
    [SerializeField, Min(0f)] private float hudSlideDuration = 0.25f;
    [SerializeField, Min(0f)] private float dialogueAfterHudDelay = 0.08f;
    [SerializeField, Min(0f)] private float hudOutsidePadding = 24f;
    [SerializeField] private bool animateTutorialHud = true;

    private sealed class HudSnapshot
    {
        public RectTransform root;
        public RectTransform viewport;
        public CanvasGroup group;
        public Vector2 home;
        public Vector2 closeStart;
        public bool moveUp;
        public bool interactable;
    }

    private readonly List<HudSnapshot> hudSnapshots = new();
    private readonly Vector3[] hudCorners = new Vector3[4];
    private readonly List<RectTransform> hudBoundsRects = new();
    private Coroutine hudTransitionCoroutine;
    private bool dialogueSessionActive;
    private bool dialogueTransitionClosing;
    private bool dialogueTransitionOpening;
    private NPCInteractable npcAwaitingHudReturn;
    private Action tutorialAwaitingHudReturn;

    // Includes lead-in, portrait, dialogue and HUD return, even with panel hidden.
    public bool IsDialogueTransitioning =>
        dialogueTransitionOpening || dialogueTransitionClosing;

    private void BeginDialogueWithHud(Action onPortraitFinished)
    {
        dialogueSessionActive = true;
        dialogueTransitionOpening = true;
        SetEKeyGuideVisible(false);
        bool animateHud = !isTutorialDialogueMode || animateTutorialHud;
        if (animateHud)
            CaptureDialogueHud();
        hudTransitionCoroutine = StartCoroutine(
            EnterDialogueWithHud(onPortraitFinished, animateHud));
    }

    private IEnumerator EnterDialogueWithHud(Action onPortraitFinished, bool animateHud)
    {
        // Ensure the coroutine handle is assigned before any synchronous callback.
        yield return null;
        if (animateHud)
        {
            yield return DialogueRealtimeDelay(dialogueLeadIn);
            yield return SlideDialogueHud(true);
            yield return DialogueRealtimeDelay(dialogueAfterHudDelay);
        }
        hudTransitionCoroutine = null;
        // Original panel OnEnable / portrait / first-line timing stays intact.
        OpenPanelWithPortraitAnimation(() =>
        {
            dialogueTransitionOpening = false;
            onPortraitFinished?.Invoke();
        });
    }

    private IEnumerator DialogueRealtimeDelay(float seconds)
    {
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            yield return null;
    }

    private void CaptureDialogueHud()
    {
        Canvas.ForceUpdateCanvases();
        hudSnapshots.Clear();
        dialogueHudTargets.RemoveAll(t => t == null || t.root == null);
        foreach (DialogueHudTarget target in dialogueHudTargets)
            CaptureDialogueHudTarget(target);
    }

    private HudSnapshot CaptureDialogueHudTarget(DialogueHudTarget target)
    {
        if (target == null || target.root == null)
            return null;
        RectTransform root = target.root;
        Canvas canvas = root.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.rootCanvas.renderMode == RenderMode.WorldSpace ||
            root.parent == null || root == canvas.rootCanvas.transform ||
            (dialoguePanel != null && dialoguePanel.transform.IsChildOf(root)))
        {
            Debug.LogWarning("Dialogue HUD: use a separate HUD wrapper under a screen-space Canvas.", root);
            return null;
        }
        bool overlaps = hudSnapshots.Exists(s => s.root != null &&
            (root.IsChildOf(s.root) || s.root.IsChildOf(root)));
        if (overlaps)
        {
            Debug.LogWarning("Dialogue HUD: duplicate or nested target skipped.", root);
            return null;
        }
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null)
            group = root.gameObject.AddComponent<CanvasGroup>();
        HudSnapshot snapshot = new HudSnapshot
        {
            root = root,
            viewport = (RectTransform)canvas.rootCanvas.transform,
            group = group,
            home = root.anchoredPosition,
            closeStart = root.anchoredPosition,
            moveUp = target.moveUp,
            interactable = group.interactable
        };
        hudSnapshots.Add(snapshot);
        // Preserve raycast blocking to avoid clicks falling through moving UI.
        group.interactable = false;
        return snapshot;
    }

    // Scene-local binders register with the surviving singleton, not a scene copy.
    public void RegisterSceneDialogueHud(UnityEngine.Object owner, RectTransform root, bool moveUp)
    {
        if (owner == null || root == null)
            return;
        dialogueHudTargets.RemoveAll(t => t == null || t.root == null);
        DialogueHudTarget existing = dialogueHudTargets.Find(t => t.root == root);
        if (existing != null && existing.sceneOwner == owner)
            return;
        bool overlaps = dialogueHudTargets.Exists(t =>
            root.IsChildOf(t.root) || t.root.IsChildOf(root));
        if (overlaps)
        {
            Debug.LogWarning("Scene HUD registration skipped: root already registered or nested. Remove scene HUD references from the manager Inspector and register them only in the scene binder.", root);
            return;
        }
        DialogueHudTarget target = new DialogueHudTarget
        {
            root = root,
            moveUp = moveUp,
            sceneOwner = owner
        };
        dialogueHudTargets.Add(target);
        // If a dialogue survives a scene load, keep the new HUD hidden too.
        // During closing, leave newly loaded HUD at home for the next dialogue.
        if (!isActiveAndEnabled || !dialogueSessionActive || dialogueTransitionClosing ||
            (isTutorialDialogueMode && !animateTutorialHud))
            return;
        Canvas.ForceUpdateCanvases();
        HudSnapshot snapshot = CaptureDialogueHudTarget(target);
        if (snapshot != null)
        {
            root.anchoredPosition = GetDialogueHudHiddenPosition(snapshot);
            snapshot.closeStart = root.anchoredPosition;
        }
    }

    public void UnregisterSceneDialogueHud(UnityEngine.Object owner)
    {
        if (owner == null)
            return;

        // 뒤에서부터 제거
        for (int i = dialogueHudTargets.Count - 1; i >= 0; i--)
        {
            DialogueHudTarget target = dialogueHudTargets[i];

            if (target == null)
            {
                dialogueHudTargets.RemoveAt(i);
                continue;
            }

            // 이 Binder가 등록한 HUD만 제거
            if (target.sceneOwner != owner)
                continue;

            RectTransform targetRoot = target.root;

            // 해당 HUD의 Snapshot도 정리
            for (int j = hudSnapshots.Count - 1; j >= 0; j--)
            {
                HudSnapshot snapshot = hudSnapshots[j];

                if (snapshot == null)
                {
                    hudSnapshots.RemoveAt(j);
                    continue;
                }

                bool isSameRoot =
                    ReferenceEquals(snapshot.root, targetRoot);

                // 씬 종료 과정에서 Unity Object가 이미 Destroy되어
                // 둘 다 null처럼 보일 수도 있으므로 owner의 target이면
                // 살아 있는 Snapshot에 대해서만 복구
                if (!isSameRoot)
                    continue;

                if (snapshot.root != null)
                    snapshot.root.anchoredPosition = snapshot.home;

                if (snapshot.group != null)
                    snapshot.group.interactable = snapshot.interactable;

                hudSnapshots.RemoveAt(j);
            }

            dialogueHudTargets.RemoveAt(i);
        }

        // Destroy된 씬 오브젝트 참조가 남아 있다면 추가 정리
        dialogueHudTargets.RemoveAll(
            t => t == null || t.root == null
        );

        hudSnapshots.RemoveAll(
            s => s == null || s.root == null
        );
    }

    private Vector2 GetDialogueHudHiddenPosition(HudSnapshot s)
    {
        if (s.root == null || s.viewport == null || s.root.parent == null)
            return s.home;

        s.root.GetComponentsInChildren<RectTransform>(true, hudBoundsRects);

        Vector2 displacement = s.root.anchoredPosition - s.home;

        Vector3 worldDisplacement = s.root.parent.TransformVector(
            new Vector3(displacement.x, displacement.y, 0f));

        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        foreach (RectTransform rect in hudBoundsRects)
        {
            if (rect == null ||
                (rect != s.root && !rect.gameObject.activeInHierarchy))
            {
                continue;
            }

            rect.GetWorldCorners(hudCorners);

            foreach (Vector3 corner in hudCorners)
            {
                float y = s.viewport.InverseTransformPoint(
                    corner - worldDisplacement).y;

                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }

        float delta = s.moveUp
            ? Mathf.Max(0f, s.viewport.rect.yMax + hudOutsidePadding - minY)
            : Mathf.Min(0f, s.viewport.rect.yMin - hudOutsidePadding - maxY);

        Vector3 localDelta = s.root.parent.InverseTransformVector(
            s.viewport.TransformVector(new Vector3(0f, delta, 0f)));

        return s.home + new Vector2(localDelta.x, localDelta.y);
    }
    private IEnumerator SlideDialogueHud(bool hide)
    {
        if (hudSnapshots.Count == 0)
            yield break;
        foreach (HudSnapshot s in hudSnapshots)
            if (s.root != null)
                s.closeStart = s.root.anchoredPosition;
        float duration = Mathf.Max(0f, hudSlideDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            foreach (HudSnapshot s in hudSnapshots)
            {
                if (s.root == null)
                    continue;
                Vector2 end = hide ? GetDialogueHudHiddenPosition(s) : s.home;
                s.root.anchoredPosition = Vector2.LerpUnclamped(s.closeStart, end, t);
            }
            yield return null;
        }
        foreach (HudSnapshot s in hudSnapshots)
            if (s.root != null)
                s.root.anchoredPosition = hide ? GetDialogueHudHiddenPosition(s) : s.home;
    }

    private void LateUpdate()
    {
        // Keep HUD outside the Canvas if its size changes while dialogue is open.
        if (!dialogueSessionActive || IsDialogueTransitioning)
            return;
        foreach (HudSnapshot s in hudSnapshots)
            if (s.root != null)
                s.root.anchoredPosition = GetDialogueHudHiddenPosition(s);
    }

    private void CancelHudCoroutine()
    {
        if (hudTransitionCoroutine != null)
            StopCoroutine(hudTransitionCoroutine);
        hudTransitionCoroutine = null;
    }

    public void CloseDialogue()
    {
        if (isTutorialDialogueMode)
        {
            CloseTutorialDialogue();
            return;
        }
        if (dialogueTransitionClosing)
            return;
        CancelHudCoroutine();
        dialogueTransitionClosing = true;
        dialogueTransitionOpening = false;
        npcAwaitingHudReturn = currentNpc;
        CloseDialogueCore(); // Stops typing/portrait, hides panel, clears data.
        StartDialogueHudReturn();
    }

    private void CloseTutorialDialogue()
    {
        if (dialogueTransitionClosing)
            return;
        CancelHudCoroutine();
        dialogueTransitionClosing = true;
        dialogueTransitionOpening = false;
        tutorialAwaitingHudReturn = CloseTutorialDialogueCore();
        StartDialogueHudReturn();
    }

    private void StartDialogueHudReturn()
    {
        if (!isActiveAndEnabled || hudSnapshots.Count == 0)
        {
            FinishDialogueHudReturn();
            return;
        }
        hudTransitionCoroutine = StartCoroutine(ReturnDialogueHud());
    }

    private IEnumerator ReturnDialogueHud()
    {
        yield return null;
        yield return SlideDialogueHud(false);
        FinishDialogueHudReturn();
    }

    private void RestoreDialogueHud()
    {
        foreach (HudSnapshot s in hudSnapshots)
        {
            if (s.root != null)
                s.root.anchoredPosition = s.home;
            if (s.group != null)
                s.group.interactable = s.interactable;
        }
        hudSnapshots.Clear();
    }

    private void FinishDialogueHudReturn()
    {
        RestoreDialogueHud();
        hudTransitionCoroutine = null;
        dialogueSessionActive = false;
        dialogueTransitionOpening = false;
        dialogueTransitionClosing = false;
        NPCInteractable npc = npcAwaitingHudReturn;
        Action callback = tutorialAwaitingHudReturn;
        npcAwaitingHudReturn = null;
        tutorialAwaitingHudReturn = null;
        // Clear state first: callbacks may immediately start another dialogue.
        if (npc != null)
            npc.EndDialogue();
        callback?.Invoke();
    }

    private void OnDisable()
    {
        if (Instance != this)
            return;
        CancelHudCoroutine();
        NPCInteractable npc = npcAwaitingHudReturn != null ? npcAwaitingHudReturn : currentNpc;
        npcAwaitingHudReturn = null;
        // Disabling is cancellation, not successful tutorial completion.
        tutorialAwaitingHudReturn = null;
        tutorialDialogueFinishedCallback = null;
        tutorialLines.Clear();
        isTutorialDialogueMode = false;
        CloseDialogueCore();
        RestoreDialogueHud();
        dialogueSessionActive = false;
        dialogueTransitionOpening = false;
        dialogueTransitionClosing = false;
        if (npc != null)
            npc.EndDialogue();
    }
}