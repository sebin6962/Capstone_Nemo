using UnityEngine;

public class PotMakerVisual : MonoBehaviour
{
    [Header("ø¨∞· ¥ÎªÛ")]
    [SerializeField] private MakerInfo makerInfo;
    [SerializeField] private SpriteRenderer potSpriteRenderer;

    [Header("≥ø∫Ò Ω∫«¡∂Û¿Ã∆Æ")]
    [SerializeField] private Sprite openPotSprite;    // ∂—≤± ø≠∏∞ ≥ø∫Ò
    [SerializeField] private Sprite closedPotSprite;  // ∂—≤± ¥›»˘ ≥ø∫Ò

    [Header("√ ±‚»≠ ø…º«")]
    [SerializeField] private bool setOpenSpriteOnEnable = true;

    private void Reset()
    {
        makerInfo = GetComponent<MakerInfo>();
        potSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (makerInfo == null)
            makerInfo = GetComponent<MakerInfo>();

        if (potSpriteRenderer == null)
            potSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (makerInfo == null)
            makerInfo = GetComponent<MakerInfo>();

        if (makerInfo != null)
        {
            makerInfo.CraftVisualStarted += OnCraftStarted;
            makerInfo.CraftVisualEnded += OnCraftEnded;
        }

        // æ¿ ∫πø¯ Ω√ ¿ÃπÃ ¡¶¿€ ¡ﬂ¿Ã∏È ¥›»˘ ≥ø∫Ò∑Œ «•Ω√
        if (makerInfo != null && makerInfo.isProducing)
        {
            SetClosedPot();
        }
        else if (setOpenSpriteOnEnable)
        {
            SetOpenPot();
        }
    }

    private void OnDisable()
    {
        if (makerInfo != null)
        {
            makerInfo.CraftVisualStarted -= OnCraftStarted;
            makerInfo.CraftVisualEnded -= OnCraftEnded;
        }

        SetOpenPot();
    }

    private void OnCraftStarted()
    {
        SetClosedPot();
    }

    private void OnCraftEnded()
    {
        SetOpenPot();
    }

    private void SetOpenPot()
    {
        if (potSpriteRenderer == null || openPotSprite == null)
            return;

        potSpriteRenderer.sprite = openPotSprite;
    }

    private void SetClosedPot()
    {
        if (potSpriteRenderer == null || closedPotSprite == null)
            return;

        potSpriteRenderer.sprite = closedPotSprite;
    }
}
