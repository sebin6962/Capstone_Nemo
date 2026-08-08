using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Cinemachine;

/*public enum TutorialStep
{
    None,
    DogamIntro,
    Village,
    Shop
}*/

public enum VillageSecondStep
{
    GoToField = 0,
    OpenStorage = 1,
    PickUpSeed = 2,
    PlantSeed = 3,
    OpenStorage2 = 4,
    RestoreSeed = 5,
    PickUp_WateringCan = 6,
    Water = 7,
    CropGrowing = 8,
    Restore_WateringCan = 9,
    HarvestCrop = 10,
    GoToMill = 11,
    VillageSecond_Finish = 12
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    //public TutorialStep currentStep = TutorialStep.None;
    private VillageSecondStep villageSecondStep = VillageSecondStep.GoToField;

    [SerializeField] private Button dogamButton;
    [SerializeField] private Button dogamCloseButton;
    [SerializeField] private ParticleSystem dogamHighlightFX;   // FX의 루트 PS
    [SerializeField] private CanvasGroup overlayBlocker;
    [SerializeField] private PlayerManager player;
    [SerializeField] private DoGamUIManager dogamUI;
    [SerializeField] private GameObject tutorialText;

    [SerializeField] private GameObject villageTutorialPanel;

    [SerializeField] private GameObject tutorialBlocker;
    //[SerializeField] private GameObject secondTutorialBlocker;

    [SerializeField] private GameObject[] stepPanels;
    [SerializeField] private GameObject[] fixedStepPanels;
    [SerializeField] private GameObject fixedVillageTutorialPanels;

    [SerializeField] private List<TutorialDialogueLine> villageFirstStartDialogues;

    [SerializeField] private List<TutorialDialogueLine> afterGoToFieldDialogues;
    [SerializeField] private List<TutorialDialogueLine> afterHarvestDialogues;

    private string server;
    private TutorialStateData state;

    public bool IsVillageSecondTutorialRunning;

    private Coroutine showStepPanelRoutine;

    [Header("인트로 자동 시퀀스")]
    [SerializeField] private Transform villageEntryPoint;
    [SerializeField] private Transform grandmaNoticePoint;
    [SerializeField] private Transform grandmaTalkPoint;
    [SerializeField] private GameObject grandmaNpcObject;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private float autoMoveSpeed = 2.2f;
    [SerializeField] private float reactionBubbleDuration = 0.8f;

    [Header("인트로 카메라 연출")]
    [SerializeField] private CinemachineVirtualCamera introVirtualCamera;
    [SerializeField] private float introZoomSize = 4.2f;
    [SerializeField] private float introZoomDuration = 0.6f;
    [SerializeField] private float beforeMoveDelay = 0.5f;
    [SerializeField] private float beforeNoticeBubbleDelay = 1f;
    [SerializeField] private float afterIntroBubbleDelay = 0.7f;
    [SerializeField] private float beforeDialogueDelay = 1f;
    [SerializeField] private CinemachineConfiner2D introConfiner2D;

    private CinemachineFramingTransposer framingTransposer;
    private float originalXDamping;
    private float originalYDamping;
    private float originalDeadZoneWidth;
    private float originalDeadZoneHeight;
    private bool introCameraFollowTuned = false;

    [Header("공용 말풍선")]
    [SerializeField] private GameObject commonBubbleObject;
    [SerializeField] private TMP_Text commonBubbleText;
    [SerializeField] private Transform commonBubbleDefaultAnchor;
    [SerializeField] private Vector3 commonBubbleOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float introBubbleDuration = 3f;
    [SerializeField] private List<string> introBubbleTexts;
    [SerializeField] private string noticeBubbleMessage = "!";

    [SerializeField] private BubblePopupEffect commonBubbleEffect;

    [Header("튜토리얼 중 NPC 제어")]
    [SerializeField] private NPCPatrolRoute[] tutorialStopPatrolNpcs;
    [SerializeField] private GameObject[] tutorialHideNpcObjects;

    private bool isAutoSequenceRunning = false;
    private bool waitingVillageIntroFade = false;

    private float originalCameraSize;
    private bool hasCachedCameraSize = false;

    // 인트로 시작 전 다른 스폰 로직이 플레이어 위치를 덮지 못하게 함
    private bool holdPlayerAtVillageEntry = false;

    [Header("대화 중 비활성화할 버튼들")]
    [SerializeField] private List<Button> dialogueLockButtons = new List<Button>();

    private Dictionary<Button, bool> dialogueButtonStateCache = new Dictionary<Button, bool>();

    [Header("자동 시퀀스 중 비활성화할 UI 오브젝트들")]
    [SerializeField] private List<GameObject> autoSequenceDisableUIObjects = new List<GameObject>();

    private Dictionary<GameObject, bool> autoSequenceUIStateCache = new Dictionary<GameObject, bool>();

    [Header("인트로 시퀀스 레터박스")]
    [SerializeField] private IntroLetterboxAnimator introLetterbox;

    private void Awake()
    {
        Instance = this;

        if (playerAnimator == null && player != null)
            playerAnimator = player.GetComponentInChildren<Animator>();

        if (commonBubbleEffect != null)
            commonBubbleEffect.HideImmediate();
        else if (commonBubbleObject != null)
            commonBubbleObject.SetActive(false);

        if (introVirtualCamera != null)
        {
            framingTransposer = introVirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();

            if (introConfiner2D == null)
                introConfiner2D = introVirtualCamera.GetComponent<CinemachineConfiner2D>();
        }
    }

    private void SetGrandmaVisible(bool visible)
    {
        if (grandmaNpcObject != null)
            grandmaNpcObject.SetActive(visible);
    }

    void Start()
    {
        server = PlayerPrefs.GetString("SelectedSave", "default");
        state = TutorialState.Load(server);

        var flow = TutorialFlowManager.Instance;

        bool isTutorialRunning = (flow != null && flow.currentStep != GlobalTutorialStep.Done);
        SetTutorialNpcState(isTutorialRunning);

        if (state.tutorialDone || flow == null || flow.currentStep == GlobalTutorialStep.Done)
        {
            SetGrandmaVisible(false);
            CleanupTutorialVisuals();
            if (tutorialBlocker) tutorialBlocker.SetActive(false);
            //if (secondTutorialBlocker) secondTutorialBlocker.SetActive(false);
            return;
        }

        var globalStep = TutorialFlowManager.Instance.currentStep;

        Debug.Log($"[TutorialManager] Start in Village, flowStep={flow?.currentStep}, tutorialDone={state.tutorialDone}");

        bool shouldShowGrandma =
            (globalStep == GlobalTutorialStep.DogamIntro ||
             globalStep == GlobalTutorialStep.Village_First);

        SetGrandmaVisible(shouldShowGrandma);

        switch (globalStep)
        {
            //원래 코드
            //case GlobalTutorialStep.DogamIntro:
            //    //currentStep = TutorialStep.DogamIntro;
            //    tutorialBlocker.gameObject.SetActive(true);
            //   // secondTutorialBlocker.gameObject.SetActive(false);
            //    StartTutorial_DogamIntro();
            //    break;

            //새 코드
            case GlobalTutorialStep.DogamIntro:
                if (tutorialBlocker) tutorialBlocker.SetActive(true);
                // secondTutorialBlocker.gameObject.SetActive(false);

                if (TutorialFlowManager.Instance != null)
                    TutorialFlowManager.Instance.RequestTutorialTimePause();

                if (TutorialFlowManager.Instance != null &&
    !TutorialFlowManager.Instance.VillageIntroAutoSequencePlayed)
                {
                    waitingVillageIntroFade = true;
                    holdPlayerAtVillageEntry = true;

                    SetPlayerInput(false);
                    PlacePlayerAtVillageEntry();
                }
                else
                {
                    BeginDogamIntroUI();
                }
                break;

            case GlobalTutorialStep.Village_First:
                StartTutorial_VillageFirst();
                break;

            case GlobalTutorialStep.Village_Second:
                StartTutorial_VillageSecond();
                break;

            default:
                SetGrandmaVisible(false);
                CleanupTutorialVisuals();
                if (tutorialBlocker) tutorialBlocker.SetActive(false);
                break;
        }
    }

    private void LateUpdate()
    {
        if (!holdPlayerAtVillageEntry)
            return;

        if (!waitingVillageIntroFade || isAutoSequenceRunning)
            return;

        PlacePlayerAtVillageEntry();
    }

    private void PlacePlayerAtVillageEntry()
    {
        if (player == null || villageEntryPoint == null)
            return;

        Vector3 startPosition = villageEntryPoint.position;
        startPosition.z = player.transform.position.z;

        player.transform.position = startPosition;

        // Rigidbody가 있다면 이전 이동 속도도 제거
        Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();

        if (playerRigidbody != null)
        {
            playerRigidbody.position = new Vector2(
                startPosition.x,
                startPosition.y
            );

            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
    }

    public void PrepareVillageIntroUnderFade()
    {
        if (!waitingVillageIntroFade || player == null)
            return;

        SetGrandmaVisible(true);
        CleanupTutorialVisuals();
        SetPlayerInput(false);

        SetAutoSequenceUIObjectsHidden(true);
        HideCommonBubble();

        holdPlayerAtVillageEntry = true;
        PlacePlayerAtVillageEntry();

        // 처음 시작 방향도 필요하면 여기서 맞춤
        if (grandmaNoticePoint != null)
            FacePlayerTo(grandmaNoticePoint.position);

        SetPlayerAnimation(Vector2.zero, false);
        CacheOriginalCameraSize();
        PrepareIntroCameraFollow();
    }

    public void BeginVillageIntroAfterFade()
    {
        if (!waitingVillageIntroFade || isAutoSequenceRunning)
            return;

        // 저장 위치 및 씬 스폰 로직이 덮어쓴 경우를 대비해 마지막으로 확정
        PlacePlayerAtVillageEntry();

        waitingVillageIntroFade = false;
        holdPlayerAtVillageEntry = false;

        StartCoroutine(PlayVillageIntroAutoSequence_AfterFade());
    }

    private void SetDialogueButtonsLocked(bool locked)
    {
        if (dialogueLockButtons == null)
            return;

        if (locked)
        {
            dialogueButtonStateCache.Clear();

            foreach (var btn in dialogueLockButtons)
            {
                if (btn == null) continue;

                dialogueButtonStateCache[btn] = btn.interactable;
                btn.interactable = false;
            }
        }
        else
        {
            foreach (var pair in dialogueButtonStateCache)
            {
                if (pair.Key == null) continue;
                pair.Key.interactable = pair.Value;
            }

            dialogueButtonStateCache.Clear();
        }
    }

    private void SetAutoSequenceUIObjectsHidden(bool hidden)
    {
        if (autoSequenceDisableUIObjects == null)
            return;

        if (hidden)
        {
            // 이미 숨김 처리된 상태면 중복 저장 방지
            if (autoSequenceUIStateCache.Count > 0)
                return;

            autoSequenceUIStateCache.Clear();

            foreach (var uiObj in autoSequenceDisableUIObjects)
            {
                if (uiObj == null) continue;

                autoSequenceUIStateCache[uiObj] = uiObj.activeSelf;
                uiObj.SetActive(false);
            }
        }
        else
        {
            if (autoSequenceUIStateCache.Count == 0)
                return;

            foreach (var pair in autoSequenceUIStateCache)
            {
                if (pair.Key == null) continue;
                pair.Key.SetActive(pair.Value);
            }

            autoSequenceUIStateCache.Clear();
        }
    }

    private IEnumerator PlayVillageIntroAutoSequence_AfterFade()
    {
        isAutoSequenceRunning = true;
        SetPlayerInput(false);
        SetGrandmaVisible(true);

        // 인트로 자동 시퀀스와 동시에 위·아래 검은 패널 등장
        if (introLetterbox != null)
            introLetterbox.Show();

        // 0) 시퀀스 시작 후 잠깐 멈춤
        yield return new WaitForSeconds(beforeMoveDelay);

        // 1) 카메라 줌인
        if (introVirtualCamera != null)
            yield return StartCoroutine(ZoomCameraTo(introZoomSize, introZoomDuration));

        // 2) 이동 시작 전 말풍선 텍스트 여러 개
        if (introBubbleTexts != null && introBubbleTexts.Count > 0)
        {
            Transform introAnchor = player != null ? player.transform : commonBubbleDefaultAnchor;
            yield return StartCoroutine(ShowCommonBubbleSequence(introBubbleTexts, introBubbleDuration, introAnchor));
        }

        // 2.5) 말풍선 끝나고 이동 전 텀
        yield return new WaitForSeconds(afterIntroBubbleDelay);

        // 3) 마을로 걸어 들어오기
        if (grandmaNoticePoint != null && player != null)
            yield return MovePlayerTo(grandmaNoticePoint.position);

        // 4) 멈춘 뒤 말풍선 전에 1초 텀
        yield return new WaitForSeconds(beforeNoticeBubbleDelay);

        // 5) 기존 말풍선 (공용 말풍선으로 처리)
        if (!string.IsNullOrEmpty(noticeBubbleMessage))
        {
            Transform noticeAnchor = player != null ? player.transform : commonBubbleDefaultAnchor;
            yield return StartCoroutine(
                ShowCommonBubble(
                    noticeBubbleMessage,
                    reactionBubbleDuration,
                    noticeAnchor,
                    BubblePopupEffect.ShowStyle.NoticeBoing
                )
            );
        }

        // 2.5) 말풍선 끝나고 이동 전 텀
        yield return new WaitForSeconds(afterIntroBubbleDelay);

        // 6) 할머니에게 다가가기
        if (grandmaTalkPoint != null && player != null)
            yield return MovePlayerTo(grandmaTalkPoint.position);

        // 7) 마지막 방향 고정
        if (grandmaNpcObject != null && player != null)
            FacePlayerTo(grandmaNpcObject.transform.position);

        SetPlayerAnimation(Vector2.zero, false);

        // 8) 대화 시작 전 1초 텀
        yield return new WaitForSeconds(beforeDialogueDelay);

        // 9) 첫 대화
        bool dialogueFinished = false;

        PlayDialogueThen(() =>
        {
            dialogueFinished = true;
        }, villageFirstStartDialogues, grandmaNpcObject);

        yield return new WaitUntil(() => dialogueFinished);

        // 자동 인트로가 끝났으므로 레터박스 퇴장
        if (introLetterbox != null)
            introLetterbox.Hide();

        // 할머니 대화 종료 직후, 도감 열기 전에 UI 복원
        SetAutoSequenceUIObjectsHidden(false);

        if (TutorialFlowManager.Instance != null)
            TutorialFlowManager.Instance.VillageIntroAutoSequencePlayed = true;

        isAutoSequenceRunning = false;
        BeginDogamIntroUI();
    }

    void PlayDialogueThen(System.Action onFinished, List<TutorialDialogueLine> lines, GameObject focusNpcObj = null)
    {
        if (lines == null || lines.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        if (NPCDialogueUIManager.Instance == null)
        {
            Debug.LogError("NPCDialogueUIManager가 없습니다.");

            if (DialogueFocusManager.Instance != null)
                DialogueFocusManager.Instance.EndFocusImmediate();

            SetDialogueButtonsLocked(false);
            onFinished?.Invoke();
            return;
        }

        bool useFocus =
            DialogueFocusManager.Instance != null &&
            player != null &&
            focusNpcObj != null;

        if (useFocus)
        {
            DialogueFocusManager.Instance.BeginFocus(player.gameObject, focusNpcObj);
        }

        SetDialogueButtonsLocked(true);

        NPCDialogueUIManager.Instance.OpenTutorialDialogue(lines, () =>
        {
            if (useFocus && DialogueFocusManager.Instance != null)
                DialogueFocusManager.Instance.EndFocus();

            SetDialogueButtonsLocked(false);
            onFinished?.Invoke();
        });
    }

    void ShowVillageSecondStepWithDialogue(VillageSecondStep step)
    {
        switch (step)
        {
            case VillageSecondStep.OpenStorage:
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterGoToFieldDialogues);
                break;

            case VillageSecondStep.GoToMill:
                PlayDialogueThen(() =>
                {
                    ShowStepPanel(step);
                }, afterHarvestDialogues);
                break;

            default:
                ShowStepPanel(step);
                break;
        }
    }

    private void SetTutorialNpcState(bool tutorialRunning)
    {
        if (tutorialStopPatrolNpcs != null)
        {
            foreach (var patrol in tutorialStopPatrolNpcs)
            {
                if (patrol == null) continue;
                patrol.SetActive(!tutorialRunning);
            }
        }

        if (tutorialHideNpcObjects != null)
        {
            foreach (var npcObj in tutorialHideNpcObjects)
            {
                if (npcObj == null) continue;
                npcObj.SetActive(!tutorialRunning);
            }
        }

        // 할머니는 튜토리얼용 NPC라 별도로 다시 제어
        if (tutorialRunning)
        {
            var globalStep = TutorialFlowManager.Instance != null
                ? TutorialFlowManager.Instance.currentStep
                : GlobalTutorialStep.None;

            bool shouldShowGrandma =
                (globalStep == GlobalTutorialStep.DogamIntro ||
                 globalStep == GlobalTutorialStep.Village_First);

            SetGrandmaVisible(shouldShowGrandma);
        }
        else
        {
            SetGrandmaVisible(false);
        }
    }
    void StartTutorial_DogamIntro()
    {
        //시간 정지, 이동X
        SetPlayerInput(false);

        if (TutorialFlowManager.Instance != null)
            TutorialFlowManager.Instance.RequestTutorialTimePause();

        //Time.timeScale = 0f;

        //클릭X
        if (overlayBlocker)
        {
            overlayBlocker.gameObject.SetActive(true);
            overlayBlocker.blocksRaycasts = true;
            overlayBlocker.interactable = true;
            overlayBlocker.alpha = 0.9f;
        }

        if (tutorialText)
        {
            tutorialText.gameObject.SetActive(true);
        }

        if (dogamCloseButton)
        {
            dogamCloseButton.onClick.RemoveListener(OnDogamClicked);
            dogamCloseButton.onClick.AddListener(OnDogamClicked);
            dogamCloseButton.interactable = true;
        }

        //이펙트
        if (dogamHighlightFX)
        {
            dogamHighlightFX.gameObject.SetActive(true);

            var pSystems = dogamHighlightFX.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pSystems)
            {
                var main = ps.main;
                main.useUnscaledTime = true;  // Time.timeScale=0에서도 재생
                ps.Clear(true);
                ps.Play(true);
            }
        }
    }

    void BeginDogamIntroUI()
    {
        //시간 정지, 이동X
        SetPlayerInput(false);

        //클릭X
        if (overlayBlocker)
        {
            overlayBlocker.gameObject.SetActive(true);
            overlayBlocker.blocksRaycasts = true;
            overlayBlocker.interactable = true;
            overlayBlocker.alpha = 0.9f;
        }

        if (tutorialText)
        {
            tutorialText.gameObject.SetActive(true);
        }

        if (dogamCloseButton)
        {
            dogamCloseButton.onClick.RemoveListener(OnDogamClicked);
            dogamCloseButton.onClick.AddListener(OnDogamClicked);
            dogamCloseButton.interactable = true;
        }

        //이펙트
        if (dogamHighlightFX)
        {
            dogamHighlightFX.gameObject.SetActive(true);

            var pSystems = dogamHighlightFX.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pSystems)
            {
                var main = ps.main;
                main.useUnscaledTime = true;  // Time.timeScale=0에서도 재생
                ps.Clear(true);
                ps.Play(true);
            }
        }
    }

    void StartTutorial_VillageFirst()
    {
        Debug.Log("VillageFirst 시작됨");

        CleanupTutorialVisuals();

        if (tutorialBlocker)
            tutorialBlocker.SetActive(true);

        if (villageTutorialPanel)
        {
            SFXManager.Instance.PlayTutorialSFX();
            villageTutorialPanel.SetActive(true);
        }

        if (fixedVillageTutorialPanels)
            fixedVillageTutorialPanels.SetActive(true);
    }

    void StartTutorial_VillageSecond()
    {
        //시간 정지
        if (TutorialFlowManager.Instance != null)
        {
            TutorialFlowManager.Instance.RequestTutorialTimePause();
            TutorialFlowManager.Instance.LockScenePortal();
        }

        CleanupTutorialVisuals();

        if (tutorialBlocker)
            tutorialBlocker.SetActive(false);

        /*if (secondTutorialBlocker)
            secondTutorialBlocker.SetActive(false);*/

        IsVillageSecondTutorialRunning = true;
        villageSecondStep = VillageSecondStep.GoToField;
        ShowStepPanel(villageSecondStep);
    }

    public void GoToNextVillageSecondStep()
    {
        switch (villageSecondStep)
        {
            //TutorialTriggerArea.cs
            case VillageSecondStep.GoToField:
                //secondTutorialBlocker.gameObject.SetActive(true);
                villageSecondStep = VillageSecondStep.OpenStorage;
                break;
            //BoxInventoryManager.cs
            case VillageSecondStep.OpenStorage:
                villageSecondStep = VillageSecondStep.PickUpSeed;
                SetSeedForTutorial("Danhobak_seedBag");
                break;
            //HeldItemManager.cs
            case VillageSecondStep.PickUpSeed:
                villageSecondStep = VillageSecondStep.PlantSeed;
                break;
            case VillageSecondStep.PlantSeed:
                villageSecondStep = VillageSecondStep.OpenStorage2;
                break;
            //FarmManager.cs
            case VillageSecondStep.OpenStorage2:
                villageSecondStep = VillageSecondStep.RestoreSeed;
                break;
            //HeldItemManager.cs
            case VillageSecondStep.RestoreSeed:
                Debug.Log("씨앗잠금해제");
                ClearSeedLock();
                villageSecondStep = VillageSecondStep.PickUp_WateringCan;
                break;
            //WateringCanAnchor.cs
            case VillageSecondStep.PickUp_WateringCan:
                villageSecondStep = VillageSecondStep.Water;
                break;
            //FarmManager.cs
            case VillageSecondStep.Water:
                villageSecondStep = VillageSecondStep.CropGrowing;
                break;
            //FarmManager.cs
            case VillageSecondStep.CropGrowing:
                villageSecondStep = VillageSecondStep.Restore_WateringCan;
                break;
            //WateringCanAnchor.cs
            case VillageSecondStep.Restore_WateringCan:
                villageSecondStep = VillageSecondStep.HarvestCrop;
                break;
            //FarmManager.cs
            case VillageSecondStep.HarvestCrop:
                if (TutorialFlowManager.Instance != null)
                    TutorialFlowManager.Instance.UnlockScenePortal();
                villageSecondStep = VillageSecondStep.GoToMill;
                break;
            case VillageSecondStep.GoToMill:
                villageSecondStep = VillageSecondStep.VillageSecond_Finish;
                break;
            case VillageSecondStep.VillageSecond_Finish:
                CompleteVillageSecondTutorial();
                return;

            default:
                //secondTutorialBlocker.gameObject.SetActive(false);
                return;
        }

        ShowVillageSecondStepWithDialogue(villageSecondStep);

        UpdateTriggerAreas();
    }

    public void UpdateTriggerAreas()
    {
        var areas = FindObjectsOfType<TutorialTriggerArea>(true);

        foreach (var area in areas)
        {
            bool shouldBeActive = IsCurrentStep(area.TargetStep);
            area.gameObject.SetActive(shouldBeActive);
        }
    }

    public void CompleteVillageSecondTutorial()
    {
        if (!IsVillageSecondTutorialRunning)
            return;

        IsVillageSecondTutorialRunning = false;
        {
            HideAllPanels();
        }

        TutorialFlowManager.Instance.SetStep(GlobalTutorialStep.Mill);
        SetTutorialNpcState(false);
    }

    void ShowStepPanel(VillageSecondStep step)
    {
        /*        if (showStepPanelRoutine != null)
                    StopCoroutine(showStepPanelRoutine);

                // 2초 뒤에 패널 켜는 코루틴으로 빼뒀어요!!
                showStepPanelRoutine = StartCoroutine(ShowStepPanelAfterDelay(step, 1.5f));*/

        if (showStepPanelRoutine != null)
            StopCoroutine(showStepPanelRoutine);

        float delay = 0f;

        if (step == VillageSecondStep.GoToMill)
            delay = 3.5f;

        showStepPanelRoutine = StartCoroutine(ShowStepPanelAfterDelay(step, delay));
    }

    private IEnumerator ShowStepPanelAfterDelay(VillageSecondStep step, float delay)
    {
        yield return new WaitForSeconds(delay);

        SFXManager.Instance.PlayTutorialSFX();
        HideAllPanels();

        int index = (int)step;
        if (stepPanels == null || index < 0 || index >= stepPanels.Length)
            yield break;

        if (stepPanels[index])
        {
            stepPanels[index].SetActive(true);
        }

        if (fixedStepPanels != null && index >= 0 && index < fixedStepPanels.Length && fixedStepPanels[index])
            fixedStepPanels[index].SetActive(true);

        // 코루틴 끝남
        showStepPanelRoutine = null;
    }

    void HideAllPanels()
    {
        if (stepPanels == null) return;
        foreach (var panel in stepPanels)
        {
            if (panel) panel.SetActive(false);
        }

        if (fixedStepPanels != null)
        {
            foreach (var panel in fixedStepPanels)
                if (panel) panel.SetActive(false);
        }
    }

    public bool IsCurrentStep(VillageSecondStep step)
    {
        return IsVillageSecondTutorialRunning && villageSecondStep == step;
    }

    void OnDogamClicked()
    {
        if (TutorialFlowManager.Instance.currentStep != GlobalTutorialStep.DogamIntro)
        {
            return;
        }

        if (overlayBlocker)
        {
            overlayBlocker.blocksRaycasts = false;
            overlayBlocker.interactable = false;
            overlayBlocker.alpha = 0f;
            overlayBlocker.gameObject.SetActive(false);
        }

        if (tutorialText)
        {
            tutorialText.gameObject.SetActive(false);
        }

        if (dogamHighlightFX)
        {
            var pSystems = dogamHighlightFX.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pSystems)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            dogamHighlightFX.gameObject.SetActive(false);
        }

        if (TutorialFlowManager.Instance != null)
            TutorialFlowManager.Instance.ReleaseTutorialTimePause();

        if (introVirtualCamera != null && hasCachedCameraSize)
            StartCoroutine(ZoomOutAndRestoreCamera());

        SetPlayerInput(true);

        //if (villageTutorialPanel)
        //{
        //    SFXManager.Instance.PlayBbyongSFX();
        //    villageTutorialPanel.SetActive(true);
        //}

        //currentStep = TutorialStep.Village;

        //지금 시간만 멈추고 플레이어만 움직이게 할 수 없는듯? 추후 수정 예정
        //우선 시간 흐르게...
        //Time.timeScale = 1f;

        if (dogamCloseButton != null)
            dogamCloseButton.onClick.RemoveListener(OnDogamClicked);

        //효과음 넣으니까 겹쳐서 2초 지연 코루틴으로 뺐습니다!
        StartCoroutine(ShowVillageTutorialAfterDelay(1.2f));
    }

    private IEnumerator ZoomOutAndRestoreCamera()
    {
        yield return StartCoroutine(ZoomCameraTo(originalCameraSize, introZoomDuration));
        RestoreIntroCameraFollow();
    }

    private IEnumerator ShowVillageTutorialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        TutorialFlowManager.Instance.SetStep(GlobalTutorialStep.Village_First);

        StartTutorial_VillageFirst();
    }

    private IEnumerator MovePlayerTo(Vector3 target)
    {
        if (player == null)
            yield break;

        Vector3 destination = target;
        destination.z = player.transform.position.z;

        while (Vector2.Distance(player.transform.position, destination) > 0.03f)
        {
            Vector3 current = player.transform.position;
            Vector2 dir = ((Vector2)(destination - current)).normalized;

            player.transform.position = Vector3.MoveTowards(
                current,
                destination,
                autoMoveSpeed * Time.deltaTime
            );

            SetPlayerAnimation(dir, true);
            yield return null;
        }

        player.transform.position = destination;
        SetPlayerAnimation(Vector2.zero, false);
    }

    private void FacePlayerTo(Vector3 target)
    {
        if (player == null)
            return;

        Vector2 dir = ((Vector2)(target - player.transform.position)).normalized;
        SetPlayerAnimation(dir, false);
    }

    private void SetPlayerAnimation(Vector2 dir, bool isMoving)
    {
        if (playerAnimator == null)
            return;

        if (dir.sqrMagnitude > 0.0001f)
        {
            playerAnimator.SetFloat("MoveX", dir.x);
            playerAnimator.SetFloat("MoveY", dir.y);
        }

        playerAnimator.SetBool("IsWalking", isMoving);
    }

    private void PrepareIntroCameraFollow()
    {
        if (introCameraFollowTuned)
            return;

        if (framingTransposer != null)
        {
            originalXDamping = framingTransposer.m_XDamping;
            originalYDamping = framingTransposer.m_YDamping;
            originalDeadZoneWidth = framingTransposer.m_DeadZoneWidth;
            originalDeadZoneHeight = framingTransposer.m_DeadZoneHeight;

            framingTransposer.m_XDamping = 0f;
            framingTransposer.m_YDamping = 0f;
            framingTransposer.m_DeadZoneWidth = 0f;
            framingTransposer.m_DeadZoneHeight = 0f;
        }

        if (introVirtualCamera != null)
            introVirtualCamera.PreviousStateIsValid = false;

        if (introConfiner2D != null)
            introConfiner2D.InvalidateCache();

        introCameraFollowTuned = true;
    }

    private void RestoreIntroCameraFollow()
    {
        if (!introCameraFollowTuned)
            return;

        if (framingTransposer != null)
        {
            framingTransposer.m_XDamping = originalXDamping;
            framingTransposer.m_YDamping = originalYDamping;
            framingTransposer.m_DeadZoneWidth = originalDeadZoneWidth;
            framingTransposer.m_DeadZoneHeight = originalDeadZoneHeight;
        }

        if (introVirtualCamera != null)
            introVirtualCamera.PreviousStateIsValid = false;

        if (introConfiner2D != null)
            introConfiner2D.InvalidateCache();

        introCameraFollowTuned = false;
    }
    private void CacheOriginalCameraSize()
    {
        if (hasCachedCameraSize) return;
        if (introVirtualCamera == null) return;

        originalCameraSize = introVirtualCamera.m_Lens.OrthographicSize;
        hasCachedCameraSize = true;
    }

    private IEnumerator ZoomCameraTo(float targetSize, float duration)
    {
        if (introVirtualCamera == null)
            yield break;

        float startSize = introVirtualCamera.m_Lens.OrthographicSize;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            float size = Mathf.Lerp(startSize, targetSize, lerp);

            var lens = introVirtualCamera.m_Lens;
            lens.OrthographicSize = size;
            introVirtualCamera.m_Lens = lens;

            if (introConfiner2D != null)
                introConfiner2D.InvalidateCache();

            introVirtualCamera.PreviousStateIsValid = false;

            yield return null;
        }

        var finalLens = introVirtualCamera.m_Lens;
        finalLens.OrthographicSize = targetSize;
        introVirtualCamera.m_Lens = finalLens;

        if (introConfiner2D != null)
            introConfiner2D.InvalidateCache();

        introVirtualCamera.PreviousStateIsValid = false;
    }

    private void SetBubblePosition(Transform anchor)
    {
        if (commonBubbleObject == null)
            return;

        Transform target = anchor != null ? anchor : commonBubbleDefaultAnchor;
        if (target == null)
            return;

        commonBubbleObject.transform.position = target.position + commonBubbleOffset;
    }

    private void HideCommonBubble()
    {
        if (commonBubbleEffect != null)
        {
            commonBubbleEffect.HideImmediate();
            return;
        }

        if (commonBubbleObject != null)
            commonBubbleObject.SetActive(false);
    }

    private IEnumerator ShowCommonBubble(string message, float duration, Transform anchor = null, BubblePopupEffect.ShowStyle style = BubblePopupEffect.ShowStyle.IntroSpread)
    {
        SetBubblePosition(anchor);

        if (commonBubbleEffect != null)
        {
            if (commonBubbleObject != null)
                commonBubbleObject.SetActive(true);

            yield return commonBubbleEffect.Play(message, duration, style);
            yield break;
        }

        if (commonBubbleObject == null || commonBubbleText == null)
            yield break;

        commonBubbleText.text = message;
        commonBubbleObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        commonBubbleObject.SetActive(false);
    }

    private IEnumerator ShowCommonBubbleSequence(List<string> messages, float durationPerMessage, Transform anchor = null)
    {
        if (messages == null || messages.Count == 0)
            yield break;

        foreach (var msg in messages)
        {
            SetBubblePosition(anchor);
            yield return StartCoroutine(
                ShowCommonBubble(msg, durationPerMessage, anchor, BubblePopupEffect.ShowStyle.IntroSpread)
            );
        }
    }

    //튜토리얼씨앗잠금
    public void SetSeedForTutorial(string allowedSeed)
    {
        foreach (var slot in BoxInventoryManager.Instance.slots)
        {
            if (slot == null)
                continue;
            if (!slot.IsInfiniteSeedSlot())
                continue;
            if (!slot.HasItem())
                continue;

            string itemName = slot.GetItemName();

            bool isAllowed = slot.GetItemName() == allowedSeed;
            slot.SetTutorialLocked(!isAllowed);
        }
    }

    //튜토리얼씨앗잠금해제
    public void ClearSeedLock()
    {
        foreach (var slot in BoxInventoryManager.Instance.slots)
        {
            if (slot == null)
                continue;
            if (!slot.IsInfiniteSeedSlot())
                continue;

            slot.SetTutorialLocked(false);
        }
    }

    /*void ResumeAndComplete()
    {
        SetPlayerInput(true);
        Time.timeScale = 1f;

        CleanupTutorialVisuals();

        state.tutorialDone = true;
        TutorialState.Save(server, state);

        if (dogamCloseButton != null)
            dogamCloseButton.onClick.RemoveListener(OnDogamClicked);
    }*/

    void CleanupTutorialVisuals()
    {
        if (overlayBlocker)
        {
            overlayBlocker.blocksRaycasts = false;
            overlayBlocker.interactable = false;
            overlayBlocker.alpha = 0f;
            overlayBlocker.gameObject.SetActive(false);
        }

        if (tutorialText)
        {
            tutorialText.gameObject.SetActive(false);
        }

        if (dogamHighlightFX)
        {
            var pSystems = dogamHighlightFX.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pSystems)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            dogamHighlightFX.gameObject.SetActive(false);
        }

        HideCommonBubble();
        SetDialogueButtonsLocked(false);
        SetAutoSequenceUIObjectsHidden(false);
    }

    void SetPlayerInput(bool enable)
    {
        if (player != null) player.enabled = enable;
    }

    /*public void FinishAllTutorial()
    {
        CompleteVillageSecondTutorial();

        state.tutorialDone = true;
        TutorialState.Save(server, state);

        //시간 정지 해제
        if (TutorialFlowManager.Instance != null)
            TutorialFlowManager.Instance.ReleaseTutorialTimePause();

        Debug.Log("튜토리얼 완전히 종료");
    }*/
}
