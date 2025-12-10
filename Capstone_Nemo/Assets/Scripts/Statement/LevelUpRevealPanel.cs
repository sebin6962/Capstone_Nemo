// LevelUpRevealPanel.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using TMPro;

public class LevelUpRevealPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Slots")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private Transform slotBackgroundParent;
    [SerializeField] private GameObject slotPrefab;

    [Header("Sprites")]
    [SerializeField] private SpriteAtlas atlas;
    [SerializeField] private string resourcesPrefix = "";
    [SerializeField] private string[] sheetNames;

    [Header("Level Title (Sprite Mode)")]
    // 공통 타이틀 스프라이트 사용
    [SerializeField] private bool useCommonLevelUpSprite = true;
    [SerializeField] private Image levelTitleImage;
    // 1순위: 직접 드래그한 스프라이트
    [SerializeField] private Sprite commonLevelUpSprite;

    // 2순위: 아틀라스/리소스 키 
    [SerializeField] private string commonLevelUpKey = "ui_level_up";

    // 스프라이트 성공 시 텍스트 숨김
    [SerializeField] private bool hideLevelTextWhenSprite = true;

    //레벨업 숫자
    [Header("Level Number")]
    [SerializeField] private Image levelNumberImage1;
    [SerializeField] private Image levelNumberImage10;
    [SerializeField] private Sprite[] levelNumberSprites;

    //이펙트
    [Header("Effect")]
    [SerializeField] private ParticleSystemRenderer[] preplacedFxRenderers;

    [Header("Timing")]
    [SerializeField] private float panelDelaySeconds = 1f;
    [SerializeField] private float fadeInSeconds = 0.35f;
    [SerializeField] private float slotDelaySeconds = 2f;     // 패널 먼저, 2초 뒤 슬롯
    [SerializeField] private float slotFadeSeconds = 0.25f;  // 슬롯 자체 페이드
    [SerializeField] private float slotsVisibleSeconds = 3f; // 슬롯 보이는 시간
    [SerializeField] private float fadeOutSeconds = 0.35f;
    [SerializeField] private bool crossfadeWithCutscene = true;
    [SerializeField] private float delayBetweenBgAndSlots = 0.3f;

    private static readonly Dictionary<string, Sprite> _cache = new();
    private CanvasGroup _cgPanel;
    private CanvasGroup _cgSlots;
    private CanvasGroup _cgSlotBackground; // 추가

    void Awake()
    {
        SetFxRenderers(false); 
    }

    private void SetFxRenderers(bool on)
    {
        if (preplacedFxRenderers == null) return;
        foreach (var r in preplacedFxRenderers)
            if (r) r.enabled = on;
    }

    public void Show(int level, IList<string> finishKeys, Action onComplete)
    {
        if (panelRoot == null) panelRoot = this.gameObject;

        // Panel CG
        _cgPanel = panelRoot.GetComponent<CanvasGroup>() ?? panelRoot.AddComponent<CanvasGroup>();
        _cgPanel.alpha = 0f;
        _cgPanel.interactable = false;
        _cgPanel.blocksRaycasts = false;

        // Slots CG (컨테이너에 추가)
        if (slotsParent != null)
        {
            var go = slotsParent.gameObject;
            _cgSlots = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            _cgSlots.alpha = 0f;
            go.SetActive(false); // 처음엔 안 보이게

        }

        if (slotBackgroundParent != null)
        {
            var goBg = slotBackgroundParent.gameObject;
            _cgSlotBackground = goBg.GetComponent<CanvasGroup>() ?? goBg.AddComponent<CanvasGroup>();
            _cgSlotBackground.alpha = 0f;
            goBg.SetActive(false);
        }

        //이펙트
        if (preplacedFxRenderers != null)
        {
            foreach (var r in preplacedFxRenderers)
                if (r) r.enabled = false;
        }

        StopAllCoroutines();
        BuildUI(level, finishKeys);       // 슬롯은 만들어두되 숨겨둠
        panelRoot.SetActive(true);
        StartCoroutine(ShowFlow(onComplete));
    }


    private IEnumerator ShowFlow(Action onComplete)
    {
        yield return new WaitForSecondsRealtime(panelDelaySeconds);

        /*// 1) 패널 페이드인
        yield return Fade(_cgPanel, 0f, 1f, fadeInSeconds);*/

        //1) 패널 페이드인 + 이펙트 
        var panelIn = Fade(_cgPanel, 0f, 1f, fadeInSeconds);

        if (SFXManager.Instance) SFXManager.Instance.PlayLevelUpSFX();

        if (preplacedFxRenderers != null)
        {
            foreach (var r in preplacedFxRenderers)
                if (r) r.enabled = true; 
        }
        yield return panelIn;

        // 2) 슬롯 지연
        yield return new WaitForSecondsRealtime(slotDelaySeconds);

        //슬롯 배경 페이드인
        if (_cgSlotBackground != null)
        {
            _cgSlotBackground.gameObject.SetActive(true);
            yield return Fade(_cgSlotBackground, 0f, 1f, slotFadeSeconds * 0.5f);
        }

        yield return new WaitForSecondsRealtime(delayBetweenBgAndSlots);

        // 3) 슬롯 활성 + 슬롯 페이드인
        if (_cgSlots != null)
        {
            if (SFXManager.Instance) SFXManager.Instance.PlayUnlockSlotSFX();
            _cgSlots.gameObject.SetActive(true);
            yield return Fade(_cgSlots, 0f, 1f, slotFadeSeconds);

        }

        // 4) 슬롯 노출 유지
        yield return new WaitForSecondsRealtime(slotsVisibleSeconds);

        // 5) 컷신 크로스페이드 시작
        if (crossfadeWithCutscene) onComplete?.Invoke();

        /*// 6) 패널 페이드아웃 (슬롯도 함께 사라짐)
        yield return Fade(_cgPanel, 1f, 0f, fadeOutSeconds);*/

        //6) 페이드아웃 + 이펙트
        var panelOut = Fade(_cgPanel, 1f, 0f, fadeOutSeconds);

        if (preplacedFxRenderers != null)
        {
            foreach (var r in preplacedFxRenderers)
                if (r) r.enabled = false;
        }
        yield return panelOut;

        // 7) 컷신을 나중에 시작하고 싶으면
        if (!crossfadeWithCutscene) onComplete?.Invoke();

        panelRoot.SetActive(false);
    }


    private static IEnumerator Fade(CanvasGroup cg, float from, float to, float sec)
    {
        if (cg == null) yield break;
        if (sec <= 0f) { cg.alpha = to; yield break; }

        float t = 0f;
        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.SmoothStep(from, to, Mathf.Clamp01(t / sec));
            yield return null;
        }
        cg.alpha = to;
    }

    private void BuildUI(int level, IList<string> keys)
    {
        SetupLevelTitle(level);

        for (int i = slotsParent.childCount - 1; i >= 0; i--)
            Destroy(slotsParent.GetChild(i).gameObject);

        if (keys == null) return;
        foreach (var key in keys)
        {
            var go = Instantiate(slotPrefab, slotsParent);

            var unlock = go.transform.Find("UnlockImage");
            if(unlock != null)
            {
                var img = unlock.GetComponent<Image>();
                if (img != null)
                    img.sprite = ResolveSprite(key);
            }

            var nameText = go.transform.Find("UnlockName");
            if (nameText != null)
            {
                var txt = nameText.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    string displayName = key;
                    if (ItemTooltipDB.TooltipTexts.TryGetValue(key, out var localizedName))
                        displayName = localizedName;

                    txt.text = displayName;
                }
            }
        }
    }

    private void SetupLevelTitle(int level)
    {
        Sprite titleSprite = null;

        // 1) 인스펙터에 직접 넣은 스프라이트가 있으면 그걸 사용
        if (useCommonLevelUpSprite && commonLevelUpSprite != null)
            titleSprite = commonLevelUpSprite;

        // 2) 키로 찾기
        if (useCommonLevelUpSprite && titleSprite == null && !string.IsNullOrEmpty(commonLevelUpKey))
            titleSprite = ResolveSprite(commonLevelUpKey);

        bool hasSprite = (titleSprite != null);

        if (useCommonLevelUpSprite && levelTitleImage != null && hasSprite)
        {
            levelTitleImage.sprite = titleSprite;
            levelTitleImage.enabled = true;
            if (hideLevelTextWhenSprite && levelText != null)
                levelText.gameObject.SetActive(false);
        }
        else
        {
            // 폴백: 기존 텍스트
            if (levelText != null)
            {
                levelText.text = "LEVEL UP!";
                levelText.gameObject.SetActive(true);
            }
            if (levelTitleImage != null)
                levelTitleImage.enabled = false;
        }

        //레벨
        /*if (levelNumberImage != null && levelNumberSprites != null)
        {
            int idx = Mathf.Clamp(level - 1, 0, levelNumberSprites.Length - 1);
            var numSprite = levelNumberSprites[idx];

            if (numSprite != null)
            {
                levelNumberImage.sprite = numSprite;
                levelNumberImage.enabled = true;
            }
            else
            {
                levelNumberImage.enabled = false; // 스프라이트 없으면 숨김
            }
        }*/
        SetLevelNumber(level);
    }

    public void SetLevelNumber(int level)
    {
        if (levelNumberSprites == null || levelNumberSprites.Length < 10)
            return;

        int ones = level % 10;       
        int tens = (level / 10) % 10;   

        if (levelNumberImage1 != null)
        {
            levelNumberImage1.sprite = levelNumberSprites[ones];
            levelNumberImage1.enabled = true;
        }

        if (levelNumberImage10 != null)
        {
            if (level >= 10)
            {
                levelNumberImage10.sprite = levelNumberSprites[tens];
                levelNumberImage10.enabled = true; 
            }
            else
            {
                levelNumberImage10.gameObject.SetActive(false);
                levelNumberImage10.enabled = false; 
            }
        }
    }

    private Sprite ResolveSprite(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (_cache.TryGetValue(key, out var sc)) return sc;

        Sprite s = null;
        if (atlas != null) { s = atlas.GetSprite(key); if (s != null) return _cache[key] = s; }
        if (!string.IsNullOrEmpty(resourcesPrefix))
        {
            s = Resources.Load<Sprite>($"{resourcesPrefix}/{key}");
            if (s != null) return _cache[key] = s;
        }
        if (sheetNames != null)
        {
            foreach (var sheet in sheetNames)
            {
                if (string.IsNullOrEmpty(sheet)) continue;
                var all = Resources.LoadAll<Sprite>(
                    string.IsNullOrEmpty(resourcesPrefix) ? sheet : $"{resourcesPrefix}/{sheet}"
                );
                if (all != null && all.Length > 0)
                {
                    s = all.FirstOrDefault(sp => sp != null && sp.name == key);
                    if (s != null) return _cache[key] = s;
                }
            }
        }
        Debug.LogWarning($"[LevelUpRevealPanel] Sprite not found: {key}");
        return null;
    }
}

