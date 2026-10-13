
using System.Collections;
using UnityEngine;

public class TreeInteract : MonoBehaviour
{
    public static TreeInteract Instance;

    private bool isPlayerNear = false;

    [Header("나무 해금 패널")]
    public GameObject popupUI;

    [Header("패널 등장 별빛 효과")]
    public GameObject panelOpenEffectRoot;

    [Header("최종 해금 후 석상 상호작용")]
    [Tooltip("별도의 Altar 오브젝트 연결")]
    [SerializeField] private Transform altarTransform;

    [SerializeField, Min(0f)]
    private float firstShakeAngle = 2f;

    [SerializeField, Min(0f)]
    private float secondShakeAngle = 5f;

    [SerializeField, Min(0.01f)]
    private float shakeDuration = 0.5f;

    [SerializeField, Min(0f)]
    private float interactionCooldown = 1f;

    [SerializeField, Min(1f)]
    private float shakeCycles = 4f;

    private int endingInteractionCount;
    private bool endingInteractionBusy;
    private bool endingInteractionCommitted;
    private Coroutine shakeRoutine;
    private Quaternion altarOriginalRotation;

    private void Awake()
    {
        Instance = this;

        if (altarTransform != null)
            altarOriginalRotation = altarTransform.localRotation;
    }

    private void Update()
    {
        if (!isPlayerNear || !Input.GetKeyDown(KeyCode.E))
            return;

        if (endingInteractionBusy || endingInteractionCommitted)
            return;

        TreeLevelUnlocker unlocker = TreeLevelUnlocker.Instance;

        if (unlocker != null && unlocker.IsPlayingUnlockSequence)
            return;

        bool canStartEnding =
            unlocker != null && unlocker.CanInteractForEnding;

        if (canStartEnding)
        {
            // After the final unlock, the unlock panel remains open.
            // The first E should close that panel, not count as an altar interaction.
            if (popupUI != null && popupUI.activeSelf)
            {
                ClosePopup();
                return;
            }

            if (endingInteractionCount < 2)
            {
                float angle = endingInteractionCount == 0
                    ? firstShakeAngle
                    : secondShakeAngle;

                endingInteractionCount++;

                shakeRoutine = StartCoroutine(
                    ShakeAltarAndCooldown(angle)
                );
            }
            else
            {
                endingInteractionCommitted = true;
                unlocker.StartEndingByInteraction();
            }

            return;
        }

        TogglePopup();
    }

    private IEnumerator ShakeAltarAndCooldown(float angle)
    {
        endingInteractionBusy = true;

        if (altarTransform != null)
        {
            Quaternion originalRotation = altarTransform.localRotation;

            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / shakeDuration);
                float damp = 1f - t;

                float offset =
                    Mathf.Sin(t * shakeCycles * Mathf.PI * 2f)
                    * angle * damp;

                altarTransform.localRotation =
                    originalRotation * Quaternion.Euler(0f, 0f, offset);

                yield return null;
            }

            altarTransform.localRotation = originalRotation;
        }
        else
        {
            Debug.LogWarning(
                "[TreeInteract] Altar Transform이 연결되지 않았습니다.",
                this
            );
        }

        if (interactionCooldown > 0f)
            yield return new WaitForSeconds(interactionCooldown);

        endingInteractionBusy = false;
        shakeRoutine = null;
    }

    private void OnDisable()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (altarTransform != null)
            altarTransform.localRotation = altarOriginalRotation;

        endingInteractionBusy = false;
    }

    private void TogglePopup()
    {
        if (popupUI == null)
            return;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayTreeInterectSFX();

        bool willOpen = !popupUI.activeSelf;
        popupUI.SetActive(willOpen);

        if (willOpen)
        {
            if (TreeLevelUnlocker.Instance != null)
                TreeLevelUnlocker.Instance.ApplyPanelSprite();

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
            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            particle.Play(true);
        }
    }

    public void ClosePopup()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayFileSelectSFX();

        if (popupUI != null)
            popupUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = false;
    }

    public bool IsOpen()
    {
        return popupUI != null && popupUI.activeSelf;
    }
}
