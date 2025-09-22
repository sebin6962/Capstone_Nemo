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
    [SerializeField] private GameObject slotPrefab;

    [Header("Sprites")]
    [SerializeField] private SpriteAtlas atlas;
    [SerializeField] private string resourcesPrefix = "";
    [SerializeField] private string[] sheetNames;

    [Header("Timing")]
    [SerializeField] private float panelDelaySeconds = 1f;
    [SerializeField] private float fadeInSeconds = 0.35f;
    [SerializeField] private float slotDelaySeconds = 2f;     // ★ 패널 먼저, 2초 뒤 슬롯
    [SerializeField] private float slotFadeSeconds = 0.25f;  // ★ 슬롯 자체 페이드
    [SerializeField] private float slotsVisibleSeconds = 3f; // ★ 슬롯 보이는 시간
    [SerializeField] private float fadeOutSeconds = 0.35f;
    [SerializeField] private bool crossfadeWithCutscene = true;

    private static readonly Dictionary<string, Sprite> _cache = new();
    private CanvasGroup _cgPanel;
    private CanvasGroup _cgSlots; // 추가

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
            go.SetActive(false); // ★ 처음엔 안 보이게
        }

        StopAllCoroutines();
        BuildUI(level, finishKeys);       // 슬롯은 만들어두되 숨겨둠
        panelRoot.SetActive(true);
        StartCoroutine(ShowFlow(onComplete));
    }


    private IEnumerator ShowFlow(Action onComplete)
    {
        yield return new WaitForSecondsRealtime(panelDelaySeconds);

        // 1) 패널 페이드인
        yield return Fade(_cgPanel, 0f, 1f, fadeInSeconds);

        // 2) 슬롯 지연
        yield return new WaitForSecondsRealtime(slotDelaySeconds);

        // 3) 슬롯 활성 + 슬롯 페이드인
        if (_cgSlots != null)
        {
            _cgSlots.gameObject.SetActive(true);
            yield return Fade(_cgSlots, 0f, 1f, slotFadeSeconds);
        }

        // 4) 슬롯 노출 유지
        yield return new WaitForSecondsRealtime(slotsVisibleSeconds);

        // 5) 컷신 크로스페이드 시작
        if (crossfadeWithCutscene) onComplete?.Invoke();

        // 6) 패널 페이드아웃 (슬롯도 함께 사라짐)
        yield return Fade(_cgPanel, 1f, 0f, fadeOutSeconds);

        // 7) 컷신을 나중에 시작하고 싶으면(크로스페이드 X)
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
        if (levelText != null) levelText.text = $"{level} 레벨로 레벨업!";

        for (int i = slotsParent.childCount - 1; i >= 0; i--)
            Destroy(slotsParent.GetChild(i).gameObject);

        if (keys == null) return;
        foreach (var key in keys)
        {
            var go = Instantiate(slotPrefab, slotsParent);
            var img = go.GetComponentInChildren<Image>(true);
            if (img != null) img.sprite = ResolveSprite(key);
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

