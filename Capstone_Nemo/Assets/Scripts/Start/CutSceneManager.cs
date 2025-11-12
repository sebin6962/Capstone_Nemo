using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CutSceneManager : MonoBehaviour
{
    [Header("컷 패널 (순서대로)")]
    public List<GameObject> cutPanels = new List<GameObject>();

    [Header("페이드용 검은 화면")]
    public Image fadeImage;

    [Header("타이밍(컷)")]
    [Tooltip("자막이 전혀 없을 때 컷을 유지하는 시간(초)")]
    public float panelHoldSeconds = 3f;
    [Tooltip("검은 화면 페이드 시간(초)")]
    public float fadeSeconds = 1.5f;

    [Tooltip("원본 호환용 변수(표시/참조용)")]
    public float cutSceneDuration = 3f;

    // ===== 자막 구조 =====
    [System.Serializable]
    public class SubtitleEntry
    {
        [TextArea(2, 10)]
        public string content; // 멀티라인 입력 가능, "||"로 라인(블록) 분할
    }

    [Header("자막(스택 방식)")]
    [Tooltip("컷 패널과 동일한 길이. content 내 '||'로 라인(블록) 분할")]
    public List<SubtitleEntry> subtitles = new List<SubtitleEntry>();

    [Tooltip("자막 줄들을 담는 컨테이너 (VerticalLayoutGroup + ContentSizeFitter 권장)")]
    public RectTransform subtitleContainer;

    [Tooltip("자막 라인 프리팹 (Text + CanvasGroup, 알파 0으로 저장)")]
    public TextMeshProUGUI subtitleLinePrefab; // TMP 쓰면 TMP_Text로 교체

    [Tooltip("컨테이너 전체 투명도 제어용(없으면 자동 추가)")]
    public CanvasGroup subtitleGroup;

    [Header("자막 줄 스타일")]
    [Tooltip("VerticalLayoutGroup.spacing을 코드에서 강제 적용")]
    public float lineSpacingPx = 6f;   // 줄 간 간격(px)

    [Tooltip("Text 컴포넌트의 lineSpacing (1.0이 기본)")]
    public float textLineSpacing = 1.0f;

    [Tooltip("라인 텍스트 좌상단 정렬 강제")]
    public bool forceTopLeftAlign = true;

    [Header("타이밍(자막)")]
    [Tooltip("첫 줄이 나타나기 전 지연(컷 등장 후)")]
    public float firstLineDelay = 0.5f;
    [Tooltip("줄과 줄 사이 지연")]
    public float betweenLinesDelay = 0.8f;
    [Tooltip("한 줄 페이드 인 시간")]
    public float lineFadeSeconds = 0.5f;
    [Tooltip("모든 줄이 표시된 뒤 추가로 머무는 시간")]
    public float afterAllLinesHoldSeconds = 0.8f;

    [Header("표기 규칙")]
    [Tooltip("자막 content에서 라인(블록)을 나누는 구분자")]
    public string segmentDelimiter = "||";

    [Header("Subtitle Gradient")]
    public Image gradientOverlay;              // 하단 그라데이션 Image
    public float gradientHeight = 320f;        // 오버레이 높이(px, RT에서 설정해도 됨)
    [Range(0, 1)] public float bottomAlpha = 0.65f;
    [Range(0, 1)] public float topAlpha = 0.0f;
    public float gradientFadeSeconds = 0.3f;
    private CanvasGroup gradientGroup;

    [Header("컷 종료시 자막/그라데이션 동시 페이드 아웃 시간")]
    public float overlayFadeOutSeconds = 0.35f;

    [Header("타자기(스르륵) 옵션")]
    [Tooltip("초당 나타나는 글자 수 (커질수록 빠름)")]
    public float charsPerSecond = 28f;

    [Tooltip("줄이 완성되기 전 클릭하면 해당 줄을 즉시 완성")]
    public bool clickCompletesCurrentLine = true;

    [Tooltip("해당 컷의 모든 자막이 완성된 상태에서 클릭하면 다음 컷으로 즉시 진행")]
    public bool clickToSkipAfterAllLines = true;

    [Tooltip("클릭 안내용 화살표 ui")]
    public GameObject nextArrowIndicator;

    [Tooltip("컷신 씬 진입 후 화살표 활성화 딜레이(초)")]
    public float arrowDelaySeconds = 2f;

    [Header("컷신 건너뛰기 버튼")]
    public GameObject skipButton;

    private bool isSkipping = false;

    private void Awake()
    {
        // 컷 패널 비활성화
        foreach (var p in cutPanels) if (p != null) p.SetActive(false);

        // 검은 화면 시작 알파 = 1
        if (fadeImage != null)
        {
            var c = fadeImage.color; c.a = 1f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = false;
        }

        if (subtitleContainer != null)
        {
            var rt = subtitleContainer;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            var vlg = rt.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = false;
                vlg.spacing = lineSpacingPx; // 인스펙터 값 보정용
            }

            if (subtitleGroup == null)
                subtitleGroup = subtitleContainer.GetComponent<CanvasGroup>();
            if (subtitleGroup == null)
                subtitleGroup = subtitleContainer.gameObject.AddComponent<CanvasGroup>();
            subtitleGroup.alpha = 1f;

            ClearSubtitleContainer();
        }
        // 하단 그라데이션 배치 (없으면 NRE 방지)
        if (gradientOverlay != null)
        {
            var grt = gradientOverlay.rectTransform;
            grt.anchorMin = new Vector2(0f, 0f);
            grt.anchorMax = new Vector2(1f, 0f);
            grt.pivot = new Vector2(0.5f, 0f);
            grt.sizeDelta = new Vector2(grt.sizeDelta.x, gradientHeight); // 높이만 관리
        }
        // 캔버스 그룹(페이드용)
        gradientGroup = gradientOverlay.GetComponent<CanvasGroup>();
        if (!gradientGroup) gradientGroup = gradientOverlay.gameObject.AddComponent<CanvasGroup>();
        gradientGroup.alpha = 0f;
        gradientOverlay.raycastTarget = false;

        // 스프라이트가 없으면 런타임 생성
        if (gradientOverlay.sprite == null)
        {
            gradientOverlay.sprite = MakeVerticalGradientSprite(
                4, Mathf.RoundToInt(gradientHeight),
                new Color(0f, 0f, 0f, topAlpha),     // 위(투명)
                new Color(0f, 0f, 0f, bottomAlpha)   // 아래(진한)
            );
            gradientOverlay.type = Image.Type.Simple;
        }
    }


    private void Start()
    {
        // 화살표
        if (nextArrowIndicator != null)
            nextArrowIndicator.SetActive(false);

        //건너뛰기
        if (skipButton != null)
            skipButton.SetActive(false);

        StartCoroutine(PlayCutAndTransition());

        // 일정 시간 후 화살표 표시
        if (nextArrowIndicator != null || skipButton != null)
            StartCoroutine(ShowArrowAfterDelay());
    }

    private IEnumerator ShowArrowAfterDelay()
    {
        yield return new WaitForSeconds(arrowDelaySeconds);

        if (nextArrowIndicator != null)
            nextArrowIndicator.SetActive(true);

        if (skipButton != null)
            skipButton.SetActive(true);
    }

    private IEnumerator PlayCutAndTransition()
    {
        if (fadeImage == null || cutPanels == null || cutPanels.Count == 0)
        {
            yield return TransitionToVillage();
            yield break;
        }

        for (int i = 0; i < cutPanels.Count; i++)
        {
            cutPanels[i].SetActive(true);

            // [검은 화면] 1 -> 0 (컷 보이기)
            yield return Fade(1f, 0f, fadeSeconds);

            // 이 컷 자막 실행(스택 append)
            bool hasLines = HasSubtitle(i);
            if (hasLines)
            {
                ClearSubtitleContainer();
                yield return ShowSubtitleStackRoutine(i);
            }
            else
            {
                // 자막이 없으면 컷만 잠시 유지
                yield return new WaitForSeconds(panelHoldSeconds);
            }

            if (subtitleGroup != null)
                yield return FadeCanvasGroup(subtitleGroup, subtitleGroup.alpha, 0f, 0.4f);
            if (gradientGroup != null)
                yield return FadeCanvasGroup(gradientGroup, gradientGroup.alpha, 0f, gradientFadeSeconds);

            // 마지막 컷이면 씬 전환
            if (i == cutPanels.Count - 1)
            {
                if (nextArrowIndicator != null)
                    nextArrowIndicator.SetActive(false);

                if (skipButton != null)
                    skipButton.SetActive(false);

                // 자막 전체를 부드럽게 걷어내기
                if (subtitleGroup != null)
                    yield return FadeCanvasGroup(subtitleGroup, subtitleGroup.alpha, 0f, 0.4f);

                yield return TransitionToVillage();
                yield break;
            }

            //if (subtitleGroup != null)
            //    StartCoroutine(FadeCanvasGroup(subtitleGroup, subtitleGroup.alpha, 0f, 0.35f));
            //if (gradientGroup != null)
            //    StartCoroutine(FadeCanvasGroup(gradientGroup, gradientGroup.alpha, 0f, gradientFadeSeconds));

            yield return FadeOutSubsAndGradient(overlayFadeOutSeconds);

            // 다음 컷 전: 자막 전체 페이드 아웃 + 화면 어둡게
            if (subtitleGroup != null)
                StartCoroutine(FadeCanvasGroup(subtitleGroup, subtitleGroup.alpha, 0f, 0.35f));

            yield return Fade(0f, 1f, fadeSeconds); // 화면 어둡게

            // 현재 컷 정리
            cutPanels[i].SetActive(false);
            ClearSubtitleContainer();
            if (subtitleGroup != null) subtitleGroup.alpha = 1f; // 다음 컷에서 새 줄 표시 대비
        }
    }

    private Sprite MakeVerticalGradientSprite(int width, int height, Color top, Color bottom)
    {
        var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / Mathf.Max(1, height - 1);
            Color c = Color.Lerp(bottom, top, t); // y=0(아래)=bottom, y=height-1(위)=top
            for (int x = 0; x < width; x++) tex.SetPixel(x, y, c);
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private IEnumerator FadeOutSubsAndGradient(float duration)
    {
        Coroutine c1 = null, c2 = null;
        if (subtitleGroup != null) c1 = StartCoroutine(FadeCanvasGroup(subtitleGroup, subtitleGroup.alpha, 0f, duration));
        if (gradientGroup != null) c2 = StartCoroutine(FadeCanvasGroup(gradientGroup, gradientGroup.alpha, 0f, duration));
        if (c1 != null) yield return c1;
        if (c2 != null) yield return c2;
    }

    private IEnumerator TransitionToVillage()
    {
        if (fadeImage != null)
        {
            yield return Fade(fadeImage.color.a, 1f, fadeSeconds * 0.5f);
        }

        // === 기존 로직 유지 ===
        if (VillageSceneManager.Instance != null)
        {
            Destroy(VillageSceneManager.Instance.gameObject);
            VillageSceneManager.Instance = null;
        }

        if (VillageSceneManager.Instance != null)
        {
            VillageSceneManager.Instance.ResetData();
        }

        SceneTransitionInfo.Instance.entranceID = "FromPlayerStore";
        FadeManager.Instance.FadeToScene("VillageScene");
        PlayerPrefs.SetInt("StartTimeOnEnter", 1);
        yield return null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        var color = fadeImage.color;
        color.a = from;
        fadeImage.color = color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            color.a = a;
            fadeImage.color = color;
            yield return null;
        }
        color.a = to;
        fadeImage.color = color;
    }

    // ===== 자막(스택 append) =====
    private bool HasSubtitle(int index)
    {
        if (subtitles == null || index >= subtitles.Count || subtitles[index] == null) return false;
        var raw = subtitles[index].content;
        return !string.IsNullOrWhiteSpace(raw);
    }

    //private IEnumerator ShowSubtitleStackRoutine(int index)
    //{
    //    if (gradientGroup != null)
    //        StartCoroutine(FadeCanvasGroup(gradientGroup, gradientGroup.alpha, 1f, gradientFadeSeconds));

    //    if (subtitleContainer == null || subtitleLinePrefab == null) yield break;

    //    string raw = subtitles[index]?.content ?? "";
    //    if (string.IsNullOrWhiteSpace(raw)) yield break;

    //    // 1) 블록 분리 (한 블록 = 한 줄)
    //    string[] segments = raw.Split(new string[] { segmentDelimiter }, System.StringSplitOptions.None);

    //    // 2) 모든 줄을 '미리' 생성해 배치 (알파=0)
    //    ClearSubtitleContainer();
    //    var lineGroups = new List<CanvasGroup>(segments.Length);

    //    for (int s = 0; s < segments.Length; s++)
    //    {
    //        string seg = (segments[s] ?? "").Replace("\\n", "\n").TrimEnd('\r', '\n', ' ');

    //        var line = Instantiate(subtitleLinePrefab, subtitleContainer);

    //        var tmp = line as TMP_Text;
    //        tmp.alignment = TextAlignmentOptions.Top; // 'Top' = 상단 중앙
    //        tmp.enableWordWrapping = true;
    //        tmp.enableAutoSizing = false;
    //        tmp.lineSpacing = textLineSpacing;

    //        // CanvasGroup으로 투명하게 자리만 잡아둠
    //        var cg = line.GetComponent<CanvasGroup>();
    //        if (cg == null) cg = line.gameObject.AddComponent<CanvasGroup>();
    //        cg.alpha = 0f;

    //        line.text = seg;
    //        lineGroups.Add(cg);

    //        // 과도한 높이 강제 방지
    //        var le = line.GetComponent<LayoutElement>();
    //        if (le != null) { le.minHeight = 0f; le.preferredHeight = -1f; le.flexibleHeight = 0f; }
    //    }


    //    // 레이아웃 즉시 갱신: 실행 시 Spacing/높이 반영
    //    LayoutRebuilder.ForceRebuildLayoutImmediate(subtitleContainer);

    //    // 4) 순차적으로 페이드 인 (첫 줄은 처음부터 최종 위치에서 등장)
    //    yield return new WaitForSeconds(firstLineDelay);

    //    for (int s = 0; s < lineGroups.Count; s++)
    //    {
    //        yield return FadeCanvasGroup(lineGroups[s], 0f, 1f, lineFadeSeconds);
    //        if (s < lineGroups.Count - 1)
    //            yield return new WaitForSeconds(betweenLinesDelay);
    //    }

    //    if (afterAllLinesHoldSeconds > 0f)
    //        yield return new WaitForSeconds(afterAllLinesHoldSeconds);
    //}
    private IEnumerator ShowSubtitleStackRoutine(int index)
    {
        if (gradientGroup != null)
            StartCoroutine(FadeCanvasGroup(gradientGroup, gradientGroup.alpha, 1f, gradientFadeSeconds));

        if (subtitleContainer == null || subtitleLinePrefab == null) yield break;

        string raw = subtitles[index]?.content ?? "";
        if (string.IsNullOrWhiteSpace(raw)) yield break;

        // 1 블록 분리 (한 블록 = 한 줄)
        string[] segments = raw.Split(new string[] { segmentDelimiter }, System.StringSplitOptions.None);

        // 2 모든 줄을 미리 만들되, 알파=1로 보이게 하고 글자만 0부터 보여줌
        ClearSubtitleContainer();
        var lines = new List<TextMeshProUGUI>(segments.Length);

        for (int s = 0; s < segments.Length; s++)
        {
            string seg = (segments[s] ?? "").Replace("\\n", "\n").TrimEnd('\r', '\n', ' ');

            var line = Instantiate(subtitleLinePrefab, subtitleContainer);
            var tmp = line as TMP_Text;
            tmp.alignment = TextAlignmentOptions.Top;    // 상단 정렬
            tmp.enableWordWrapping = true;
            tmp.enableAutoSizing = false;
            tmp.lineSpacing = textLineSpacing;

            // “보임 상태”로 두되, 글자 수만 0부터 증가시킴
            var cg = line.GetComponent<CanvasGroup>();
            if (!cg) cg = line.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            line.text = seg;
            line.ForceMeshUpdate();
            line.maxVisibleCharacters = 0;   // 핵심!

            lines.Add(line);

            // 레이아웃 제한(선택)
            var le = line.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 0f; le.preferredHeight = -1f; le.flexibleHeight = 0f; }
        }

        // 레이아웃 즉시 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(subtitleContainer);

        // 3 순차적으로 “타자기” 재생
        yield return new WaitForSeconds(firstLineDelay);

        for (int s = 0; s < lines.Count; s++)
        {
            yield return TypeLine(lines[s]);

            // 줄 사이 지연 ? 단, 다음 줄로 넘어가기 전에 클릭해서 ‘즉시 진행’하길 원한다면 여기서도 클릭 체크 가능
            if (s < lines.Count - 1 && betweenLinesDelay > 0f)
            {
                float t = 0f;
                while (t < betweenLinesDelay)
                {
                    if (clickCompletesCurrentLine && Input.GetMouseButtonDown(0))
                        break; // 사용자 클릭 시 다음 줄 바로 진행
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // 4 모든 줄이 끝난 상태: 클릭하면 즉시 컷 종료(다음 컷 전환)
        if (clickToSkipAfterAllLines)
        {
            float hold = afterAllLinesHoldSeconds;
            float t = 0f;
            while (t < hold)
            {
                if (Input.GetMouseButtonDown(0))
                    break; // 클릭으로 대기 스킵
                t += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            if (afterAllLinesHoldSeconds > 0f)
                yield return new WaitForSeconds(afterAllLinesHoldSeconds);
        }
    }

    // 한 줄을 왼→오 표시. 진행 중 클릭하면 즉시 완성
    private IEnumerator TypeLine(TextMeshProUGUI line)
    {
        if (line == null) yield break;

        line.ForceMeshUpdate();
        int total = line.textInfo.characterCount;
        // 공백/개행 포함한 전체 글자 수 기준. 필요하면 가시문자만 카운트하도록 커스터마이즈 가능.

        // 속도 → 글자당 시간
        float cps = Mathf.Max(1f, charsPerSecond);
        float perChar = 1f / cps;

        int visible = 0;
        while (visible < total)
        {
            // 클릭하면 즉시 완성
            if (clickCompletesCurrentLine && Input.GetMouseButtonDown(0))
            {
                visible = total;
                line.maxVisibleCharacters = visible;
                break;
            }

            visible++;
            line.maxVisibleCharacters = visible;

            // 다음 글자까지 대기
            float t = 0f;
            while (t < perChar)
            {
                // 대기 중에도 클릭 체크해서 즉시 완성 허용
                if (clickCompletesCurrentLine && Input.GetMouseButtonDown(0))
                {
                    visible = total;
                    line.maxVisibleCharacters = visible;
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        // 안전하게 최종 값 보정
        line.maxVisibleCharacters = total;
    }


    private IEnumerator FadeCanvasGroup(CanvasGroup grp, float from, float to, float duration)
    {
        if (grp == null) yield break;

        float t = 0f;
        grp.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            grp.alpha = Mathf.SmoothStep(from, to, t / duration);
            yield return null;
        }
        grp.alpha = to;
    }

    private void ClearSubtitleContainer()
    {
        if (subtitleContainer == null) return;
        for (int i = subtitleContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(subtitleContainer.GetChild(i).gameObject);
        }
    }

    //=========================건너뛰기 버튼========================
    public void OnClickSkipButton()
    {
        if (isSkipping) return;
        isSkipping = true;

        // 이 CutSceneManager에서 돌고 있는 모든 코루틴 정지
        StopAllCoroutines();

        // UI 끄기
        if (nextArrowIndicator != null)
            nextArrowIndicator.SetActive(false);
        if (skipButton != null)
            skipButton.SetActive(false);

        // 스킵용 전환 코루틴 시작
        StartCoroutine(SkipCutSceneRoutine());
    }

    private IEnumerator SkipCutSceneRoutine()
    {
       
        // 마을로 전환
        yield return TransitionToVillage();
    }

}




