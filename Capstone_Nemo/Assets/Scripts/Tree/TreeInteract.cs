using UnityEngine;

public class TreeInteract : MonoBehaviour
{
    public static TreeInteract Instance;

    private bool isPlayerNear = false;

    [Header("나무 해금 패널")]
    public GameObject popupUI;

    [Header("패널 등장 별빛 효과")]
    public GameObject panelOpenEffectRoot;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (TreeLevelUnlocker.Instance != null &&
                TreeLevelUnlocker.Instance.IsPlayingUnlockSequence)
            {
                return;
            }

            TogglePopup();
        }
    }

    private void TogglePopup()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayTreeInterectSFX();

        bool isActive = popupUI.activeSelf;
        bool willOpen = !isActive;

        popupUI.SetActive(willOpen);

        if (willOpen)
        {
            if (TreeLevelUnlocker.Instance != null)
            {
                // 패널이 열릴 때 최신 해금 상태 반영
                TreeLevelUnlocker.Instance.ApplyPanelSprite();
            }

            PlayPanelOpenEffect();
        }
    }

    private void PlayPanelOpenEffect()
    {
        if (panelOpenEffectRoot == null)
            return;

        panelOpenEffectRoot.SetActive(true);

        ParticleSystem[] particles =
            panelOpenEffectRoot.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            // 이전에 남아 있던 파티클 제거
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 처음부터 다시 재생
            particle.Play(true);
        }
    }

    public void ClosePopup()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayFileSelectSFX();

        popupUI.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }

    public bool IsOpen()
    {
        return popupUI != null && popupUI.activeSelf;
    }
}
