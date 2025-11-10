using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;
    public AudioSource audioSource;

    //public AudioClip sieveClip;
    //public AudioClip siruClip;
    //public AudioClip mixingClip;
    public AudioClip plateSoundClip;
    public AudioClip bbyongClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip farmWaterClip;
    public AudioClip farmSeedClip;
    public AudioClip boxOpenClip;

    // makerId별 효과음 관리용 (Inspector에서 할당)
    public AudioClip sieveMakerInputClip;
    public AudioClip siruMakerInputClip;
    public AudioClip mixingMakerInputClip;

    [Header("게임 인트로")]
    public AudioClip introClickClip;
    public AudioClip btnClickSFX;
    public AudioClip fileSelectSFX;

    [Header("Dogam")]
    public AudioClip pageFlipSFX;
    public AudioClip dogamOpenSFX;

    [Header("Scene Change")]
    public AudioClip doorOpenSFX;

    [Header("Trash Can Sounds")]
    public AudioClip trashDiscardClip;   // 정상 폐기

    [Header("Tree Interect")]
    public AudioClip treeInterectClip;
    public AudioClip treeOpenClip;

    [Header("Statement")]
    public AudioClip moneyCountClip;
    public AudioClip totalMoneyClip;

    [Header("Level UP")]
    public AudioClip levelRevealClip;
    public AudioClip unlockSlotClip;

    [Header("Player")]
    public AudioSource walkAudioSource;
    public AudioClip playerWalkClip;

    [Header("농사")]
    public AudioClip harvestingClip; // 작물 수확 소리
    public AudioClip harvestItemClip; //수확한 아이템 인벤 들어가는 소리
   
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else Destroy(gameObject);
    }

    //public void PlaySieveSFX()
    //{
    //    if (audioSource && sieveClip)
    //        audioSource.PlayOneShot(sieveClip);
    //}

    //public void PlayMixingSFX()
    //{
    //    if (audioSource && mixingClip)
    //        audioSource.PlayOneShot(mixingClip);
    //}

    //public void PlaySiruSFX()
    //{
    //    if (audioSource && siruClip)
    //        audioSource.PlayOneShot(siruClip);
    //}

    public void PlayPlateSoundSFX()
    {
        if (audioSource && plateSoundClip)
            audioSource.PlayOneShot(plateSoundClip);
    }

    public void PlayIntroClickSFX()
    {
        if (audioSource && introClickClip)
            audioSource.PlayOneShot(introClickClip);
    }

    public void PlayCorrectSFX()
    {
        if (audioSource && correctClip)
            audioSource.PlayOneShot(correctClip);
    }

    public void PlayWrongSFX()
    {
        if (audioSource && wrongClip)
            audioSource.PlayOneShot(wrongClip);
    }

    public void PlayBbyongSFX()
    {
        if (audioSource && bbyongClip)
            audioSource.PlayOneShot(bbyongClip);
    }

    public void PlayFarmWaterSFX()
    {
        if (audioSource && farmWaterClip)
            audioSource.PlayOneShot(farmWaterClip);
    }

    public void PlayFarmSeedSFX()
    {
        if (audioSource && farmSeedClip)
            audioSource.PlayOneShot(farmSeedClip);
    }

    public void PlayBoxOpenSFX()
    {
        if (audioSource && boxOpenClip)
            audioSource.PlayOneShot(boxOpenClip);
       
    }

    public void PlayMakerProgressSFX(string makerId)
    {
        AudioClip targetClip = null;
        switch (makerId)
        {
            case "Sieve01":
                audioSource.PlayOneShot(sieveMakerInputClip);
                break;
            case "Sieve02":
                audioSource.PlayOneShot(sieveMakerInputClip);
                break;
            case "Siru01":
                audioSource.PlayOneShot(siruMakerInputClip);
                break;
            case "Siru02":
                audioSource.PlayOneShot(siruMakerInputClip);
                break;
            case "Mixing01":
                audioSource.PlayOneShot(mixingMakerInputClip);
                break;
            case "Mixing02":
                audioSource.PlayOneShot(mixingMakerInputClip);
                break;
        }

        if (audioSource && targetClip)
        {
            audioSource.clip = targetClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    // 진행바 끝나면
    public void StopMakerProgressSFX()
    {
        if (audioSource)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }
    }

    public void PlayBtnClickSFX()
    {
        if (audioSource != null && btnClickSFX != null)
        {
            audioSource.PlayOneShot(btnClickSFX);
        }
    }

    public void PlayFileSelectSFX()
    {
        if (audioSource != null && fileSelectSFX != null)
        {
            audioSource.PlayOneShot(fileSelectSFX);
        }
    }

    public void PlayDogamOpenSFX()
    {
        if (audioSource != null && dogamOpenSFX != null)
        {
            audioSource.PlayOneShot(dogamOpenSFX);
        }
    }

    public void PlayPageFlipSFX()
    {
        if (audioSource != null && pageFlipSFX != null)
        {
            audioSource.PlayOneShot(pageFlipSFX);
        }
    }

    public void PlayDoorOpenSFX()
    {
        if (audioSource != null && doorOpenSFX != null)
        {
            audioSource.PlayOneShot(doorOpenSFX);
        }
    }

    public void PlayTrashDiscardSFX()
    {
        if (audioSource != null && trashDiscardClip != null)
            audioSource.PlayOneShot(trashDiscardClip);
    }

    public void PlayTreeInterectSFX()
    {
        if (audioSource != null && treeInterectClip != null)
            audioSource.PlayOneShot(treeInterectClip);
    }

    public void PlayTreeOpenSFX()
    {
        if (audioSource != null && treeOpenClip != null)
            audioSource.PlayOneShot(treeOpenClip);
    }

    public void PlayMoneyCountSFX()
    {
        if (audioSource != null && moneyCountClip != null)
            audioSource.PlayOneShot(moneyCountClip);
    }

    public void PlayTotalMoneySFX()
    {
        if (audioSource != null && totalMoneyClip != null)
            audioSource.PlayOneShot(totalMoneyClip);
    }

    public void PlayLevelUpSFX()
    {
        if (audioSource != null && levelRevealClip != null)
            audioSource.PlayOneShot(levelRevealClip);
    }

    public void PlayUnlockSlotSFX()
    {
        if (audioSource != null && unlockSlotClip != null)
            audioSource.PlayOneShot(unlockSlotClip);
    }

    //플레이어 걷는 효과음
    public void SetPlayerWalkClip(AudioClip clip)
    {
        playerWalkClip = clip;

        if (walkAudioSource != null)
        {
            walkAudioSource.clip = playerWalkClip;
        }
    }

    public void PlayPlayerWalkLoop()
    {
        if (walkAudioSource != null && playerWalkClip != null)
        {
            if (!walkAudioSource.isPlaying)
            {
                walkAudioSource.clip = playerWalkClip;
                walkAudioSource.loop = true;
                walkAudioSource.Play();
            }
        }
    }

    public void StopPlayerWalkLoop()
    {
        if (walkAudioSource != null && walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
            walkAudioSource.loop = false;
            walkAudioSource.clip = null;
        }
    }

    public void HarvestingSFX()
    {
        if (audioSource != null && harvestingClip != null)
            audioSource.PlayOneShot(harvestingClip);
    }

    public void HarvestItemSFX()
    {
        if (audioSource != null && harvestItemClip != null)
            audioSource.PlayOneShot(harvestItemClip);
    }
}
