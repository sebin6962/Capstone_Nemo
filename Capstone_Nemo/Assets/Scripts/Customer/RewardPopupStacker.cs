using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardPopupStacker : MonoBehaviour
{
    public static RewardPopupStacker Instance;

    [Header("UI References")]
    [SerializeField] private RectTransform container;      // 패널들이 붙을 부모 컨테이너
    [SerializeField] private RewardPopupPanel panelPrefab; // 팝업 패널 프리팹

    [Header("Stack Settings")]
    [SerializeField] private int maxPanels = 3;
    [SerializeField] private float yOffset = 110f;         // 패널 간 간격(위로 쌓이게)
    [SerializeField] private Vector2 basePos = Vector2.zero; // 첫번째(맨 아래) 위치

    [Header("Anim")]
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private float popInDuration = 0.18f;

    private readonly List<RewardPopupPanel> active = new List<RewardPopupPanel>();

    private struct PopupData
    {
        public int exp, star;
        public string dagwa;
        public PopupData(int e, int s, string d) { exp = e; star = s; dagwa = d; }
    }

    private readonly Queue<PopupData> pending = new Queue<PopupData>();
    private Coroutine processCo;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void Show(int exp, int star, string dagwaKeyOrName)
    {
        if (panelPrefab == null || container == null)
        {
            Debug.LogError("[RewardPopupStacker] panelPrefab 또는 container가 비었습니다.");
            return;
        }

        pending.Enqueue(new PopupData(exp, star, dagwaKeyOrName));

        if (processCo == null)
            processCo = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (pending.Count > 0)
        {
            var data = pending.Dequeue();
            yield return StartCoroutine(ShowRoutine(data.exp, data.star, data.dagwa));
        }
        processCo = null;
    }

    private IEnumerator ShowRoutine(int exp, int star, string dagwa)
    {
        if (active.Count >= maxPanels)
        {
            var bottom = active[0];
            active.RemoveAt(0);

            if (bottom != null)
                bottom.FadeOutAndDestroy(fadeOutDuration);

            for (int i = 0; i < active.Count; i++)
            {
                if (active[i] == null) continue;
                active[i].MoveTo(GetPos(i), moveDuration);
            }

            if (moveDuration > 0f)
                yield return new WaitForSeconds(moveDuration * 0.6f);
        }

        var panel = Instantiate(panelPrefab, container);
        panel.gameObject.SetActive(true);
        panel.SetContent(exp, star, dagwa); // :contentReference[oaicite:4]{index=4}

        active.Add(panel);

        int index = active.Count - 1;
        panel.PlayIn(GetPos(index), popInDuration);
    }

    private Vector2 GetPos(int index)
    {
        return basePos + Vector2.up * (yOffset * index);
    }
}
