using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Optional ending credits: vertically scrolling names, freely movable UI spaceship,
/// and a small one-shot star burst when the ship touches each credit.
/// No scores, fail state, physics colliders, or external scene transitions.
/// This object should be OUTSIDE the scrolling/world-space first-cutscene Panel.
/// </summary>
public class EndingCreditsController : MonoBehaviour
{
    public enum CreditContentType
    {
        RoleAndName = 0,
        TeamName = 1
    }

    [System.Serializable]
    public class CreditEntry
    {
        [Header("Content type")]
        public CreditContentType contentType = CreditContentType.RoleAndName;
        public string role = "PROGRAMMING";
        public string name = "Name";
        [Tooltip("TeamName 항목일 때 표시할 단독 텍스트 (\\n으로 줄바꿈)")]
        public string teamName = "TEAM NAME";

        [Header("Optional extra space ABOVE this entry")]
        [Tooltip("체크하면 이 항목 바로 위에 추가 간격을 넣습니다.")]
        public bool useExtraTopPadding = false;
        [Min(0f)] public float extraTopPadding = 100f;
    }

    [Header("UI references (Screen Space - Overlay)")]
    [SerializeField] private Canvas creditsCanvas;
    [SerializeField] private CanvasGroup creditsGroup;
    [SerializeField] private RectTransform screenArea;
    [SerializeField] private RectTransform textRoot;
    [SerializeField] private EndingCreditItemView creditTextPrefab;
    [Tooltip("팀 이름만 표시하는 TextMeshProUGUI 프리팹. 팀 이름 항목이 없으면 연결하지 않아도 됩니다.")]
    [SerializeField] private TextMeshProUGUI teamNameTextPrefab;
    [SerializeField] private RectTransform spaceship;
    [SerializeField] private Image spaceshipImage;

    [Header("Credit content (bottom to top)")]
    [SerializeField] private List<CreditEntry> credits = new List<CreditEntry>();

    [Header("Scrolling")]
    [Min(1f)] public float scrollPixelsPerSecond = 65f;
    [Min(0f)][InspectorName("First Entry Bottom Padding")] public float firstEntryTopPadding = 100f;
    [Min(0f)] public float entrySpacing = 100f;
    [Min(20f)] public float entryHeight = 100f;
    [Min(0f)] public float afterLastEntrySeconds = 1.5f;

    [Header("Spaceship: WASD or arrow keys")]
    [Min(1f)] public float shipPixelsPerSecond = 350f;
    [Min(0f)] public float shipScreenMargin = 50f;

    [Header("Spaceship idle floating (EKeyGuideFloat style)")]
    [SerializeField] private bool enableIdleFloat = true;
    [Min(0f)][SerializeField] private float idleFloatDistance = 6f;
    [Min(0f)][SerializeField] private float idleFloatSpeed = 2f;
    [Min(0f)][SerializeField] private float idleFloatReturnSpeed = 36f;
    private Vector2 shipBasePosition;
    private float shipFloatOffset;
    private float shipFloatElapsed;
    private bool shipWasMoving;

    [Header("Spaceship movement star trail")]
    [SerializeField] private bool enableMovementTrail = true;
    [Tooltip("이동 중 별빛 생성 간격(초). 값이 작을수록 촘촘해집니다.")]
    [Min(0.01f)][SerializeField] private float trailSpawnInterval = 0.045f;
    [Tooltip("우주선 중심에서 이동 반대 방향으로 별빛이 시작되는 거리.")]
    [Min(0f)][SerializeField] private float trailRearOffset = 24f;
    [Tooltip("별빛이 사라질 때까지의 시간(초).")]
    [Min(0.05f)][SerializeField] private float trailLifetime = 0.65f;
    [Tooltip("별빛 이미지의 크기 범위(픽셀).")]
    [Min(1f)][SerializeField] private float trailMinSize = 3f;
    [Min(1f)][SerializeField] private float trailMaxSize = 9f;
    [Tooltip("이동 방향에 수직으로 흩어지는 범위.")]
    [Min(0f)][SerializeField] private float trailSideSpread = 8f;
    [Tooltip("생성된 별빛이 이동 반대 방향으로 천천히 퍼지는 거리.")]
    [Min(0f)][SerializeField] private float trailDriftDistance = 24f;
    [SerializeField] private Color trailColor = new Color(0.85f, 0.93f, 1f, 0.85f);
    private float trailSpawnTimer;
    private readonly List<GameObject> activeTrailStars = new List<GameObject>();

    [Header("Touch feedback")]
    [Min(1)] public int starCount = 9;
    [Min(0.1f)] public float starLifetime = 0.7f;
    [Min(1f)] public float starTravelPixels = 75f;
    [Min(0f)] public float textBrightenSeconds = 0.4f;
    public Color touchedTextColor = new Color(1f, 0.92f, 0.67f, 1f);
    public Color starColor = new Color(1f, 0.88f, 0.55f, 1f);
    [SerializeField] private Sprite starSprite; // Optional: a small star sprite. Generated automatically if empty.

    private readonly List<RectTransform> generatedRects = new List<RectTransform>();
    private readonly List<EndingCreditItemView> generatedTexts = new List<EndingCreditItemView>();
    // TeamName 엔트리는 generatedTexts에 null, generatedTeamTexts에 TMP를 보관합니다.
    // 두 리스트 모두 generatedRects와 동일한 인덱스를 사용합니다.
    private readonly List<TextMeshProUGUI> generatedTeamTexts = new List<TextMeshProUGUI>();
    // Each entry can react again after the ship leaves and re-enters, but not
    // while its preceding light effect is still running.
    private readonly List<bool> shipInsideEntry = new List<bool>();
    private readonly List<float> nextEffectAllowedAt = new List<float>();
    // 호환성 안전장치: 이전 EndingCutSceneScroller는 StartScrolling()을 호출하지 않습니다.
    // 최신 스크립트가 호출하면 즉시 시작하고, 그렇지 않으면 지정한 시간 뒤 자동 시작합니다.
    [Header("Scroll start compatibility")]
    [Tooltip("이전 EndingCutSceneScroller를 쓰는 경우에만 적용되는 자동 시작 안전장치(초). 최신 버전에서는 StartScrolling 호출이 우선합니다.")]
    [Min(0f)] public float fallbackScrollStartDelay = 4f;
    private Coroutine fallbackScrollRoutine;
    private bool creditsScrollEnabled;
    private Sprite generatedStarSprite;
    private Texture2D generatedStarTexture;
    private float elapsedAfterLast;
    private bool playing;

    public bool IsFinished { get; private set; } = true;

    private void Awake()
    {
        if (creditsGroup == null && creditsCanvas != null)
            creditsGroup = creditsCanvas.GetComponent<CanvasGroup>();
        if (spaceshipImage == null && spaceship != null)
            spaceshipImage = spaceship.GetComponent<Image>();

        // Keep the scene's UI hierarchy active so references remain valid;
        // hide with CanvasGroup until EndingCutSceneScroller starts the credits.
        if (creditsGroup != null)
        {
            creditsGroup.alpha = 0f;
            creditsGroup.interactable = false;
            creditsGroup.blocksRaycasts = false;
        }
    }

    public bool BeginCredits()
    {
        if (screenArea == null || textRoot == null || spaceship == null || creditTextPrefab == null || creditsGroup == null)
        {
            Debug.LogError("[EndingCreditsController] Credits Group / Screen Area / Text Root / Spaceship / Credit Text Prefab 연결을 확인하세요.", this);
            IsFinished = true;
            return false;
        }
        if (creditsCanvas == null || creditsCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Debug.LogError("[EndingCreditsController] CreditsCanvas는 Screen Space - Overlay로 설정하세요.", this);
            IsFinished = true;
            return false;
        }
        if (credits.Count == 0)
        {
            Debug.LogWarning("[EndingCreditsController] 크레딧 목록이 비어 있습니다.", this);
            IsFinished = true;
            return false;
        }

        if (textRoot.parent != screenArea || spaceship.parent != screenArea)
        {
            Debug.LogError("[EndingCreditsController] TextRoot와 Spaceship은 ScreenArea 바로 아래 자식이어야 합니다.", this);
            IsFinished = true;
            return false;
        }

        ClearGeneratedCredits();
        shipInsideEntry.Clear();
        nextEffectAllowedAt.Clear();
        elapsedAfterLast = 0f;
        IsFinished = false;
        playing = true;
        // Reveal the starfield and ship first; scrolling starts explicitly after
        // the transition and opening hold in EndingCutSceneScroller.
        creditsScrollEnabled = false;
        if (fallbackScrollRoutine != null)
            StopCoroutine(fallbackScrollRoutine);
        fallbackScrollRoutine = StartCoroutine(FallbackStartScrolling());

        // The overlay's canvas order must remain BELOW the existing black fade (101)
        // and subtitle canvas (100), but ABOVE the world-space cutscenes.
        creditsCanvas.overrideSorting = true;
        creditsCanvas.sortingOrder = 90;
        creditsCanvas.enabled = true;
        creditsCanvas.gameObject.SetActive(true);
        creditsGroup.alpha = 1f;
        creditsGroup.interactable = false;
        creditsGroup.blocksRaycasts = false;
        if (spaceshipImage != null) spaceshipImage.raycastTarget = false;

        Canvas.ForceUpdateCanvases();
        float halfHeight = screenArea.rect.height * 0.5f;
        float accumulatedExtraPadding = 0f;
        for (int i = 0; i < credits.Count; i++)
        {
            CreditEntry entry = credits[i];
            bool isTeamName = entry.contentType == CreditContentType.TeamName;
            EndingCreditItemView item = null;
            TextMeshProUGUI teamLabel = null;
            RectTransform rt;

            if (isTeamName)
            {
                if (teamNameTextPrefab == null)
                {
                    Debug.LogError("[EndingCreditsController] TeamName 항목을 사용하려면 Team Name Text Prefab을 연결하세요.", this);
                    HideCredits();
                    return false;
                }

                teamLabel = Instantiate(teamNameTextPrefab, textRoot);
                teamLabel.gameObject.SetActive(true);
                teamLabel.text = (entry.teamName ?? string.Empty).Replace("\\n", "\n");
                teamLabel.raycastTarget = false;
                rt = teamLabel.rectTransform;
            }
            else
            {
                item = Instantiate(creditTextPrefab, textRoot);
                item.gameObject.SetActive(true);
                if (!item.HasRequiredTexts)
                {
                    Debug.LogError("[EndingCreditsController] CreditText 프리팹의 Role Text / Name Text 연결을 확인하세요.", item);
                    Destroy(item.gameObject);
                    HideCredits();
                    return false;
                }

                string roleWithLineBreaks = (entry.role ?? string.Empty).Replace("\\n", "\n");
                string nameWithLineBreaks = (entry.name ?? string.Empty).Replace("\\n", "\n");
                item.SetText(roleWithLineBreaks, nameWithLineBreaks);
                rt = item.GetComponent<RectTransform>();
            }

            // 읽기 순서상 이전 항목과 이 항목 사이의 기본 간격을 유지합니다.
            // 체크된 항목 앞에만 추가 여백을 누적합니다.
            if (entry.useExtraTopPadding)
                accumulatedExtraPadding += Mathf.Max(0f, entry.extraTopPadding);

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.sizeDelta = new Vector2(
                Mathf.Min(Mathf.Max(100f, rt.sizeDelta.x), Mathf.Max(100f, screenArea.rect.width - 60f)),
                entryHeight);
            // 첫 항목은 화면 아래에서 출발하고, 다음 항목은 더 아래에 배치합니다.
            // 기존 serialized 필드명은 Inspector 데이터 호환을 위해 유지합니다.
            rt.anchoredPosition = new Vector2(0f,
                -halfHeight - firstEntryTopPadding - entryHeight * 0.5f
                - i * (entryHeight + entrySpacing) - accumulatedExtraPadding);

            generatedRects.Add(rt);
            generatedTexts.Add(item);
            generatedTeamTexts.Add(teamLabel);
            shipInsideEntry.Add(false);
            nextEffectAllowedAt.Add(0f);
        }

        // TMP 글자별 접촉 판정에 사용할 메쉬 정보를 생성합니다.
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < generatedRects.Count; i++)
        {
            if (generatedTexts[i] != null && generatedTexts[i].HasRequiredTexts)
            {
                generatedTexts[i].RoleText.ForceMeshUpdate();
                generatedTexts[i].NameText.ForceMeshUpdate();
            }
            if (generatedTeamTexts[i] != null)
                generatedTeamTexts[i].ForceMeshUpdate();
        }

        // Keep the initial Inspector position exactly; floating is an additive visual offset.
        shipBasePosition = spaceship.anchoredPosition;
        shipFloatOffset = 0f;
        shipFloatElapsed = 0f;
        shipWasMoving = false;
        trailSpawnTimer = 0f;
        ClearTrailStars();
        spaceship.SetAsLastSibling();
        return true;
    }

    /// <summary>Start ascending credits after the space background is revealed.</summary>
    public void StartScrolling()
    {
        if (!playing) return;
        creditsScrollEnabled = true;
        if (fallbackScrollRoutine != null)
        {
            StopCoroutine(fallbackScrollRoutine);
            fallbackScrollRoutine = null;
        }
    }

    private IEnumerator FallbackStartScrolling()
    {
        if (fallbackScrollStartDelay > 0f)
            yield return new WaitForSecondsRealtime(fallbackScrollStartDelay);
        fallbackScrollRoutine = null;
        if (playing && !creditsScrollEnabled)
        {
            Debug.LogWarning("[EndingCreditsController] StartScrolling() 호출이 없어 호환성 자동 시작을 사용합니다. EndingCutSceneScroller 최신 버전을 확인하세요.", this);
            StartScrolling();
        }
    }

    private void Update()
    {
        if (!playing) return;
        float dt = Time.unscaledDeltaTime;
        float moveY = creditsScrollEnabled ? scrollPixelsPerSecond * dt : 0f;
        for (int i = 0; i < generatedRects.Count; i++)
        {
            if (generatedRects[i] != null)
                generatedRects[i].anchoredPosition += Vector2.up * moveY;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        bool shipIsMoving = input.sqrMagnitude > 0.0001f;
        if (shipIsMoving && !shipWasMoving)
            shipFloatElapsed = 0f;
        if (!shipIsMoving && shipWasMoving)
            shipFloatElapsed = 0f;
        shipWasMoving = shipIsMoving;

        // Movement always uses a stable base position, never the bobbed visual position.
        Vector2 previousShipBasePosition = shipBasePosition;
        Vector2 next = shipBasePosition + input * shipPixelsPerSecond * dt;
        Rect area = screenArea.rect;
        float halfShipW = spaceship.rect.width * 0.5f;
        float halfShipH = spaceship.rect.height * 0.5f;
        float minX = area.xMin + shipScreenMargin + halfShipW;
        float maxX = area.xMax - shipScreenMargin - halfShipW;
        float minY = area.yMin + shipScreenMargin + halfShipH;
        float maxY = area.yMax - shipScreenMargin - halfShipH;
        next.x = Mathf.Clamp(next.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
        next.y = Mathf.Clamp(next.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
        shipBasePosition = next;
        float targetOffset = 0f;
        if (enableIdleFloat && !shipIsMoving)
        {
            shipFloatElapsed += dt;
            targetOffset = Mathf.Sin(shipFloatElapsed * idleFloatSpeed) * idleFloatDistance;
        }
        // Ease back to the movement baseline instead of snapping when WASD is pressed.
        shipFloatOffset = Mathf.MoveTowards(shipFloatOffset, targetOffset,
            idleFloatReturnSpeed * dt);
        Vector2 displayedPosition = shipBasePosition + Vector2.up * shipFloatOffset;
        displayedPosition.y = Mathf.Clamp(displayedPosition.y,
            Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
        spaceship.anchoredPosition = displayedPosition;

        // 움직인 거리로 실제 이동 여부를 판단해, 화면 가장자리에서 막혔을 때
        // 별빛이 제자리에서 계속 생성되지 않도록 합니다.
        Vector2 actualMovement = shipBasePosition - previousShipBasePosition;
        UpdateMovementTrail(actualMovement, dt);

        if (!creditsScrollEnabled) return;

        Bounds shipBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(screenArea, spaceship);
        for (int i = 0; i < generatedRects.Count; i++)
        {
            if (generatedRects[i] == null) continue;
            // 프리팹 전체 RectTransform이 아닌 실제 표시된 TMP 글자에만 닿았는지 확인.
            bool isInside = generatedTeamTexts[i] != null
                ? IsShipTouchingVisibleCharacters(shipBounds, generatedTeamTexts[i])
                : IsShipTouchingCreditText(shipBounds, generatedTexts[i]);
            bool justEntered = isInside && !shipInsideEntry[i];
            shipInsideEntry[i] = isInside;

            if (!justEntered || Time.unscaledTime < nextEffectAllowedAt[i]) continue;

            // A second touch only triggers after leaving and entering again,
            // and only when the preceding particle/text effect is finished.
            nextEffectAllowedAt[i] = Time.unscaledTime +
                Mathf.Max(starLifetime, textBrightenSeconds);
            if (generatedTeamTexts[i] != null)
                StartCoroutine(BrightenSingleText(generatedTeamTexts[i]));
            else
                StartCoroutine(BrightenText(generatedTexts[i]));
            SpawnStars(spaceship.anchoredPosition);
        }

        if (generatedRects.Count > 0 && generatedRects[generatedRects.Count - 1] != null &&
            generatedRects[generatedRects.Count - 1].anchoredPosition.y - entryHeight * 0.5f > area.yMax)
        {
            elapsedAfterLast += dt;
            if (elapsedAfterLast >= afterLastEntrySeconds)
            {
                playing = false;
                IsFinished = true;
            }
        }
    }

    // 입력이 있는 동안에도 실제로 우주선이 이동한 경우에만 트레일을 생성합니다.
    // 이동 방향의 반대로, UI 우주선 뒤편에 짧은 별빛 잔상을 남깁니다.
    private void UpdateMovementTrail(Vector2 movement, float dt)
    {
        if (!enableMovementTrail || spaceship == null || screenArea == null ||
            movement.sqrMagnitude < 0.000001f)
        {
            trailSpawnTimer = 0f;
            return;
        }

        trailSpawnTimer += dt;
        float interval = Mathf.Max(0.01f, trailSpawnInterval);
        int emitted = 0;
        while (trailSpawnTimer >= interval && emitted < 4)
        {
            trailSpawnTimer -= interval;
            SpawnTrailStar(movement.normalized);
            emitted++;
        }
        // 화면이 잠깐 멈춘 뒤 한 프레임에 너무 많은 별이 생기지 않도록 제한.
        trailSpawnTimer = Mathf.Min(trailSpawnTimer, interval);
    }

    private void SpawnTrailStar(Vector2 movementDirection)
    {
        Sprite sprite = starSprite != null ? starSprite : GetOrMakeStarSprite();
        Vector2 backward = -movementDirection;
        Vector2 side = new Vector2(-movementDirection.y, movementDirection.x);

        // Spaceship의 Anchor/Pivot이 중앙이 아니어도 ScreenArea 로컬 좌표로 변환.
        Vector3 shipCenter = screenArea.InverseTransformPoint(
            spaceship.TransformPoint(spaceship.rect.center));
        Vector2 origin = new Vector2(shipCenter.x, shipCenter.y)
            + backward * trailRearOffset
            + side * Random.Range(-trailSideSpread, trailSideSpread);

        GameObject go = new GameObject("SpaceshipTrailStar", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(screenArea, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        float size = Random.Range(Mathf.Min(trailMinSize, trailMaxSize),
            Mathf.Max(trailMinSize, trailMaxSize));
        rt.sizeDelta = Vector2.one * size;
        // 별빛이 우주선 이미지 위가 아니라 뒤에 표시되도록 합니다.
        rt.SetSiblingIndex(spaceship.GetSiblingIndex());
        rt.anchoredPosition = origin;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = trailColor;
        img.raycastTarget = false;
        activeTrailStars.Add(go);

        Vector2 drift = backward * Random.Range(trailDriftDistance * 0.4f, trailDriftDistance)
            + side * Random.Range(-trailSideSpread, trailSideSpread);
        StartCoroutine(AnimateTrailStar(rt, img, origin, drift, size));
    }

    private IEnumerator AnimateTrailStar(RectTransform rt, Image img,
        Vector2 origin, Vector2 drift, float startSize)
    {
        float elapsed = 0f;
        float lifetime = Mathf.Max(0.05f, trailLifetime);
        while (elapsed < lifetime && rt != null && img != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / lifetime);
            rt.anchoredPosition = origin + drift * p;
            rt.sizeDelta = Vector2.one * Mathf.Lerp(startSize, 0f, p);
            Color color = trailColor;
            color.a *= 1f - p;
            img.color = color;
            yield return null;
        }
        if (rt != null)
        {
            activeTrailStars.Remove(rt.gameObject);
            Destroy(rt.gameObject);
        }
    }

    private void ClearTrailStars()
    {
        for (int i = 0; i < activeTrailStars.Count; i++)
            if (activeTrailStars[i] != null) Destroy(activeTrailStars[i]);
        activeTrailStars.Clear();
    }

    // RoleText / NameText의 실제 글자 사각형을 이용해 접촉을 판정합니다.
    // 공백, 줄바꿈, TMP 오브젝트 및 크레딧 프리팹의 빈 여백은 제외합니다.
    private bool IsShipTouchingCreditText(Bounds shipBounds, EndingCreditItemView item)
    {
        if (item == null || !item.HasRequiredTexts) return false;
        return IsShipTouchingVisibleCharacters(shipBounds, item.RoleText) ||
               IsShipTouchingVisibleCharacters(shipBounds, item.NameText);
    }

    private bool IsShipTouchingVisibleCharacters(Bounds shipBounds, TextMeshProUGUI label)
    {
        if (label == null || !label.isActiveAndEnabled) return false;

        // textInfo의 글자 좌표는 TMP 오브젝트의 로컬 좌표입니다.
        // 화면 스케일 및 부모 Transform을 고려해 ScreenArea 좌표로 변환합니다.
        TMP_TextInfo info = label.textInfo;
        if (info == null || info.characterCount == 0) return false;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible) continue;

            Vector3 a = screenArea.InverseTransformPoint(label.transform.TransformPoint(character.bottomLeft));
            Vector3 b = screenArea.InverseTransformPoint(label.transform.TransformPoint(character.topRight));
            float left = Mathf.Min(a.x, b.x);
            float right = Mathf.Max(a.x, b.x);
            float bottom = Mathf.Min(a.y, b.y);
            float top = Mathf.Max(a.y, b.y);

            if (shipBounds.max.x >= left && shipBounds.min.x <= right &&
                shipBounds.max.y >= bottom && shipBounds.min.y <= top)
                return true;
        }
        return false;
    }

    private IEnumerator BrightenSingleText(TextMeshProUGUI label)
    {
        if (label == null) yield break;
        Color original = label.color;
        float time = 0f;
        while (time < textBrightenSeconds)
        {
            if (label == null) yield break;
            time += Time.unscaledDeltaTime;
            float progress = textBrightenSeconds <= 0f ? 1f : Mathf.Clamp01(time / textBrightenSeconds);
            label.color = Color.Lerp(touchedTextColor, original, progress);
            yield return null;
        }
        if (label != null) label.color = original;
    }

    private IEnumerator BrightenText(EndingCreditItemView item)
    {
        if (item == null || !item.HasRequiredTexts) yield break;
        TextMeshProUGUI role = item.RoleText;
        TextMeshProUGUI name = item.NameText;
        Color originalRole = role.color;
        Color originalName = name.color;
        float time = 0f;
        while (time < textBrightenSeconds)
        {
            if (item == null || role == null || name == null) yield break;
            time += Time.unscaledDeltaTime;
            float progress = textBrightenSeconds <= 0f ? 1f : Mathf.Clamp01(time / textBrightenSeconds);
            role.color = Color.Lerp(touchedTextColor, originalRole, progress);
            name.color = Color.Lerp(touchedTextColor, originalName, progress);
            yield return null;
        }
        if (role != null) role.color = originalRole;
        if (name != null) name.color = originalName;
    }

    private void SpawnStars(Vector2 origin)
    {
        Sprite sprite = starSprite != null ? starSprite : GetOrMakeStarSprite();
        for (int i = 0; i < starCount; i++)
        {
            GameObject go = new GameObject("CreditStar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(screenArea, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * Random.Range(5f, 12f);
            rt.anchoredPosition = origin;
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = starColor;
            img.raycastTarget = false;
            float angle = (i + Random.Range(-0.2f, 0.2f)) / starCount * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            StartCoroutine(AnimateStar(rt, img, origin, direction * Random.Range(starTravelPixels * 0.5f, starTravelPixels)));
        }
    }

    private IEnumerator AnimateStar(RectTransform rt, Image img, Vector2 origin, Vector2 offset)
    {
        float t = 0f;
        while (t < starLifetime && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(0.01f, starLifetime));
            rt.anchoredPosition = origin + offset * (1f - (1f - p) * (1f - p));
            Color c = starColor;
            c.a = 1f - p;
            img.color = c;
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    private Sprite GetOrMakeStarSprite()
    {
        if (generatedStarSprite != null) return generatedStarSprite;
        const int size = 16;
        generatedStarTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        generatedStarTexture.filterMode = FilterMode.Point;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - 7.5f);
                float dy = Mathf.Abs(y - 7.5f);
                bool visible = (dx < 1.7f && dy < 7f) || (dy < 1.7f && dx < 7f) || (dx + dy < 5f);
                generatedStarTexture.SetPixel(x, y, visible ? Color.white : Color.clear);
            }
        generatedStarTexture.Apply();
        generatedStarSprite = Sprite.Create(generatedStarTexture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), 100f);
        return generatedStarSprite;
    }

    public void HideCredits()
    {
        playing = false;
        if (fallbackScrollRoutine != null)
        {
            StopCoroutine(fallbackScrollRoutine);
            fallbackScrollRoutine = null;
        }
        creditsScrollEnabled = false;
        IsFinished = true;
        if (creditsGroup != null) creditsGroup.alpha = 0f;
        if (spaceship != null)
            spaceship.anchoredPosition = shipBasePosition;
        shipFloatOffset = 0f;
        trailSpawnTimer = 0f;
        ClearTrailStars();
        ClearGeneratedCredits();
    }

    private void ClearGeneratedCredits()
    {
        for (int i = 0; i < generatedRects.Count; i++)
            if (generatedRects[i] != null) Destroy(generatedRects[i].gameObject);
        generatedRects.Clear();
        generatedTexts.Clear();
        generatedTeamTexts.Clear();
        shipInsideEntry.Clear();
        nextEffectAllowedAt.Clear();
    }

    private void OnDestroy()
    {
        if (generatedStarSprite != null) Destroy(generatedStarSprite);
        if (generatedStarTexture != null) Destroy(generatedStarTexture);
    }
}
