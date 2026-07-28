using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NextDayCutscene : MonoBehaviour
{
    [Header("컷 패널 (1개)")]
    [SerializeField] private GameObject cutPanel; // 여기에 CanvasGroup 꼭 붙여주세요

    [Header("페이드용 검은 화면 (전역 씬 전환)")]
    [SerializeField] private Image fadeImage;

    [Header("타이밍")]
    [SerializeField] private float holdSeconds = 3f;   // 컷 유지 시간
    [SerializeField] private float fadeSeconds = 1.5f; // 컷 페이드 인 시간
    [SerializeField] private float blackHoldSeconds = 0.35f; // 완전히 검어진 상태 유지 시간

    [Header("시작 방식")]
    [SerializeField] private bool playOnStart = false;

    [Header("랜덤 세계관 정보")]
    [Tooltip("컷신이 활성화될 때 함께 표시할 TextMeshPro 오브젝트")]
    [SerializeField] private GameObject randomInfoTextObject;

    [Tooltip("Random Info Text Object에 붙어 있는 TextMeshProUGUI")]
    [SerializeField] private TMP_Text randomInfoText;

    [Tooltip("랜덤 정보 텍스트가 사라지는 데 걸리는 시간")]
    [SerializeField] private float randomInfoFadeOutSeconds = 0.5f;

    [TextArea(2, 4)]
    [SerializeField]
    private string[] randomInfoMessages =
    {
        "달마을의 토끼들은 척박한 달을 오랫동안 가꾸어 지금의 마을을 만들었습니다.",
        "달마을의 불빛과 시설은 계수나무에서 얻은 별빛으로 움직입니다.",
        "한때 찬란했던 계수나무는 어느 날부터 조금씩 빛을 잃기 시작했습니다.",
        "토끼들은 계수나무의 빛을 마을을 움직이는 소중한 자원으로 사용해 왔습니다.",
        "계수나무가 약해지자 달마을의 밤도 전보다 조금 더 어두워졌습니다.",
        "사장님의 다과가 외계 손님을 행복하게 할수록 더 많은 별빛이 달마을에 모입니다.",
        "외계 손님이 건네는 별빛에는 다과를 맛본 기쁨과 고마움이 담겨 있습니다.",
        "먼 지구별의 작은 나라, 한국에는 마음과 계절을 담아 만든 전통 다과가 있습니다.",
        "달다담의 다과는 지구의 전통을 달의 재료와 토끼들의 방식으로 이어 만든 음식입니다.",
        "다과를 팔아 모은 별빛은 계수나무에 바쳐져, 잃어버린 빛을 조금씩 되찾게 합니다.",
        "계수나무 광장은 달마을에서 가장 오래된 장소이자 토끼들이 가장 아끼는 공간입니다.",
        "분화구 농장은 메마른 달의 흙을 토끼들이 오랜 시간 가꾸어 만든 밭입니다.",
        "마을의 별등은 계수나무의 빛이 강할수록 더 따뜻하고 환하게 빛납니다.",
        "계수나무의 잎은 빛의 상태에 따라 색과 반짝임이 조금씩 달라집니다.",
        "든해 할머니는 오래전부터 계수나무와 달마을을 지켜봐 왔습니다.",
        "보리는 계수나무 잎의 색과 별빛의 흐름에서 작은 변화도 금세 알아챕니다.",
        "인쇄 종족 하린은 말 대신 무늬로 마음을 전하며, 수와 복 문양이 찍힌 다식을 좋아합니다.",
        "다식의 수(壽) 문양에는 건강과 장수를, 복(福) 문양에는 행운을 바라는 마음을 담습니다.",
        "말이 통하지 않는 외계 손님에게도 정성껏 만든 다과의 마음은 전해집니다.",
        "오늘 모은 작은 별빛 하나가 계수나무와 달마을의 내일을 밝힙니다."
    };

    [Header("Subtitle Gradient")]
    [SerializeField] private Image gradientOverlay;          // 하단 검은 그라디언트 이미지
    [SerializeField] private float gradientHeight = 320f;
    [Range(0, 1f)][SerializeField] private float bottomAlpha = 0.65f;
    [Range(0, 1f)][SerializeField] private float topAlpha = 0f;
    [SerializeField] private float gradientFadeSeconds = 0.3f;
    [SerializeField] private float overlayFadeOutSeconds = 0.35f;

    [SerializeField] private GameObject subtitleTextObject;   // 그라디언트 위에 띄울 텍스트
    [SerializeField] private float textShowDelayAfterGradient = 0.1f;

    [SerializeField] private float textFadeInSeconds = 0.4f;
    [SerializeField] private float textFadeOutSeconds = 0.3f;

    [Header("Subtitle Text Wave / Saved Effect")]
    [SerializeField] private TMP_Text subtitleText; // subtitleTextObject에 붙은 TextMeshProUGUI
    [SerializeField] private float textChangeDelaySeconds = 1.5f; // n초 뒤 변경
    [SerializeField] private string savedMessage = "저장되었습니다";
    [SerializeField] private Color savedMessageColor = new Color(0.35f, 1f, 0.35f, 1f);

    [SerializeField] private float textChangeFadeSeconds = 0.25f;

    [SerializeField] private float waveAmplitude = 6f;  // 위아래 흔들림 크기
    [SerializeField] private float waveSpeed = 6f;      // 웨이브 속도
    [SerializeField] private float waveSpacing = 0.45f; // 글자 간 웨이브 간격

    private Coroutine subtitleWaveCoroutine;
    private string defaultSubtitleText;
    private Color defaultSubtitleColor = Color.white;

    private CanvasGroup gradientGroup;
    private CanvasGroup subtitleTextGroup;
    private CanvasGroup cutCanvasGroup;
    private CanvasGroup randomInfoTextGroup;

    private static int lastRandomInfoIndex = -1;

    private void Awake()
    {
        if (cutPanel != null)
        {
            cutCanvasGroup = cutPanel.GetComponent<CanvasGroup>();
            if (cutCanvasGroup == null)
                cutCanvasGroup = cutPanel.AddComponent<CanvasGroup>();

            cutPanel.SetActive(false);
            cutCanvasGroup.alpha = 0f;
        }

        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }

        SetupRandomInfoText();

        if (gradientOverlay != null)
        {
            var grt = gradientOverlay.rectTransform;
            grt.anchorMin = new Vector2(0f, 0f);
            grt.anchorMax = new Vector2(1f, 0f);
            grt.pivot = new Vector2(0.5f, 0f);
            grt.sizeDelta = new Vector2(grt.sizeDelta.x, gradientHeight);

            gradientGroup = gradientOverlay.GetComponent<CanvasGroup>();
            if (gradientGroup == null)
                gradientGroup = gradientOverlay.gameObject.AddComponent<CanvasGroup>();
            gradientGroup.alpha = 0f;
            gradientOverlay.raycastTarget = false;

            if (gradientOverlay.sprite == null)
            {
                gradientOverlay.sprite = MakeVerticalGradientSprite(
                    4,
                    Mathf.RoundToInt(gradientHeight),
                    new Color(0f, 0f, 0f, topAlpha),      // 위쪽(투명)
                    new Color(0f, 0f, 0f, bottomAlpha)    // 아래쪽(진한 검정)
                );
                gradientOverlay.type = Image.Type.Simple;
            }

            if (subtitleTextObject != null)
            {
                subtitleTextGroup = subtitleTextObject.GetComponent<CanvasGroup>();
                if (subtitleTextGroup == null)
                    subtitleTextGroup = subtitleTextObject.AddComponent<CanvasGroup>();

                subtitleTextGroup.alpha = 0f;
                subtitleTextObject.SetActive(false);

                if (subtitleText == null)
                    subtitleText = subtitleTextObject.GetComponent<TMP_Text>();

                if (subtitleText != null)
                {
                    defaultSubtitleText = subtitleText.text;
                    defaultSubtitleColor = subtitleText.color;
                }
                else
                {
                    Debug.LogWarning("[NextDayCutscene] subtitleTextObject에 TMP_Text(TextMeshProUGUI)가 없습니다.");
                }
            }
        }
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlaySingleCutAndTransition());
    }

    private IEnumerator PlaySingleCutAndTransition()
    {
        if (cutPanel == null || cutCanvasGroup == null)
        {
            Debug.LogWarning("[NextDayCutscene] 컷 패널 세팅 누락");
            yield break;
        }

        // 컷 패널은 검은 화면 뒤에서 켜질 수 있도록 일단 숨겨 둔다.
        cutPanel.SetActive(false);
        cutCanvasGroup.alpha = 0f;

        if (gradientOverlay != null && gradientGroup != null)
        {
            gradientOverlay.gameObject.SetActive(false);
            gradientGroup.alpha = 0f;
        }

        if (fadeImage != null)
        {
            // 명세서 화면 위를 검게 덮는다.
            fadeImage.gameObject.SetActive(true);
            fadeImage.transform.SetAsLastSibling();
            fadeImage.raycastTarget = true;

            yield return FadeImageAlpha(fadeImage, 0f, 1f, fadeSeconds);

            // 완전히 검어진 뒤에만 컷신 패널을 활성화한다.
            cutPanel.SetActive(true);
            cutCanvasGroup.alpha = 1f;
            ShowRandomInfoText();

            if (blackHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(blackHoldSeconds);

            // 검은 화면이 걷히면서 컷신이 보이게 한다.
            yield return FadeImageAlpha(fadeImage, 1f, 0f, fadeSeconds);

            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }
        else
        {
            // fadeImage가 연결되지 않았을 때 기존 방식으로 재생한다.
            cutPanel.SetActive(true);
            ShowRandomInfoText();
            yield return FadeCanvasGroup(cutCanvasGroup, 0f, 1f, fadeSeconds);
        }

        if (gradientOverlay != null && gradientGroup != null)
        {
            yield return new WaitForSeconds(0.2f);                     // 컷신 후 약간의 텀
            gradientOverlay.gameObject.SetActive(true);
            gradientGroup.alpha = 0f;

            yield return FadeCanvasGroup(gradientGroup, 0f, 1f, gradientFadeSeconds);

            if (subtitleTextObject != null)
            {
                if (textShowDelayAfterGradient > 0f)
                    yield return new WaitForSeconds(textShowDelayAfterGradient);

                subtitleTextObject.SetActive(true);
                subtitleTextGroup.alpha = 0f;

                ResetSubtitleTextToDefault();

                // 페이드가 시작되는 순간부터 바로 웨이브 시작
                StartSubtitleWave();

                // 서서히 0 → 1로 나타나는 동안에도 글자가 계속 움직임
                yield return FadeCanvasGroup(subtitleTextGroup, 0f, 1f, textFadeInSeconds);

                // n초 뒤 "저장되었습니다"로 자연스럽게 변경
                if (textChangeDelaySeconds > 0f)
                    yield return new WaitForSecondsRealtime(textChangeDelaySeconds);

                yield return ChangeSubtitleToSavedMessage();
            }
        }

        yield return new WaitForSecondsRealtime(holdSeconds);

        if (subtitleTextObject != null && subtitleTextGroup != null)
        {
            StopSubtitleWave();

            yield return FadeCanvasGroup(subtitleTextGroup, subtitleTextGroup.alpha, 0f, textFadeOutSeconds);
            subtitleTextObject.SetActive(false);
        }

        if (gradientOverlay != null && gradientGroup != null)
        {
            yield return FadeCanvasGroup(gradientGroup, gradientGroup.alpha, 0f, overlayFadeOutSeconds);
            gradientOverlay.gameObject.SetActive(false);
        }

        yield return HideRandomInfoText();

        yield return TransitionToVillage();
    }

    private void SetupRandomInfoText()
    {
        if (randomInfoText == null && randomInfoTextObject != null)
            randomInfoText = randomInfoTextObject.GetComponent<TMP_Text>();

        if (randomInfoTextObject == null && randomInfoText != null)
            randomInfoTextObject = randomInfoText.gameObject;

        if (randomInfoTextObject != null)
        {
            randomInfoTextGroup = randomInfoTextObject.GetComponent<CanvasGroup>();
            if (randomInfoTextGroup == null)
                randomInfoTextGroup = randomInfoTextObject.AddComponent<CanvasGroup>();

            randomInfoTextGroup.alpha = 0f;
            randomInfoTextGroup.interactable = false;
            randomInfoTextGroup.blocksRaycasts = false;
            randomInfoTextObject.SetActive(false);
        }
    }

    private void ShowRandomInfoText()
    {
        if (randomInfoTextObject == null || randomInfoText == null)
            return;

        if (randomInfoMessages == null || randomInfoMessages.Length == 0)
        {
            Debug.LogWarning("[NextDayCutscene] Random Info Messages가 비어 있습니다.");
            randomInfoTextObject.SetActive(false);
            return;
        }

        int randomIndex;

        if (randomInfoMessages.Length == 1)
        {
            randomIndex = 0;
        }
        else
        {
            do
            {
                randomIndex = Random.Range(0, randomInfoMessages.Length);
            }
            while (randomIndex == lastRandomInfoIndex);
        }

        lastRandomInfoIndex = randomIndex;
        randomInfoText.text = randomInfoMessages[randomIndex];
        randomInfoTextObject.SetActive(true);
        randomInfoTextGroup.alpha = 1f;
        randomInfoText.ForceMeshUpdate();
    }

    private IEnumerator HideRandomInfoText()
    {
        if (randomInfoTextObject == null)
            yield break;

        if (randomInfoTextGroup != null)
        {
            yield return FadeCanvasGroup(
                randomInfoTextGroup,
                randomInfoTextGroup.alpha,
                0f,
                randomInfoFadeOutSeconds
            );
        }

        randomInfoTextObject.SetActive(false);
    }

    private IEnumerator TransitionToVillage()
    {
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

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float seconds)
    {
        float t = 0f;
        cg.alpha = from;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            yield return null;
        }

        cg.alpha = to;
    }

    private IEnumerator FadeImageAlpha(Image image, float from, float to, float seconds)
    {
        float t = 0f;
        Color color = image.color;
        color.a = from;
        image.color = color;

        if (seconds <= 0f)
        {
            color.a = to;
            image.color = color;
            yield break;
        }

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            image.color = color;
            yield return null;
        }

        color.a = to;
        image.color = color;
    }

    private void ResetSubtitleTextToDefault()
    {
        StopSubtitleWave();

        if (subtitleText == null)
            return;

        subtitleText.text = defaultSubtitleText;
        subtitleText.color = defaultSubtitleColor;
        subtitleText.ForceMeshUpdate();
    }

    private void StartSubtitleWave()
    {
        StopSubtitleWave();

        if (subtitleText == null)
            return;

        subtitleWaveCoroutine = StartCoroutine(SubtitleWaveCoroutine());
    }

    private void StopSubtitleWave()
    {
        if (subtitleWaveCoroutine != null)
        {
            StopCoroutine(subtitleWaveCoroutine);
            subtitleWaveCoroutine = null;
        }

        if (subtitleText != null)
        {
            subtitleText.ForceMeshUpdate();
            subtitleText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }

    private IEnumerator SubtitleWaveCoroutine()
    {
        if (subtitleText == null)
            yield break;

        while (true)
        {
            subtitleText.ForceMeshUpdate();

            TMP_TextInfo textInfo = subtitleText.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                if (!charInfo.isVisible)
                    continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                float wave = Mathf.Sin(Time.unscaledTime * waveSpeed + i * waveSpacing) * waveAmplitude;
                Vector3 offset = new Vector3(0f, wave, 0f);

                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                subtitleText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }
    }

    private IEnumerator ChangeSubtitleToSavedMessage()
    {
        if (subtitleText == null || subtitleTextGroup == null)
            yield break;

        StopSubtitleWave();

        // 기존 문구 살짝 사라짐
        yield return FadeCanvasGroup(
            subtitleTextGroup,
            subtitleTextGroup.alpha,
            0f,
            textChangeFadeSeconds
        );

        // 문구와 색상 변경
        subtitleText.text = savedMessage;
        subtitleText.color = savedMessageColor;
        subtitleText.ForceMeshUpdate();

        // 새 문구 자연스럽게 등장
        yield return FadeCanvasGroup(
            subtitleTextGroup,
            0f,
            1f,
            textChangeFadeSeconds
        );
    }

    private Sprite MakeVerticalGradientSprite(int width, int height, Color top, Color bottom)
    {
        var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / Mathf.Max(1, height - 1);
            Color c = Color.Lerp(bottom, top, t); // y=0(아래)=bottom, y=height-1(위)=top
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, c);
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }
}




