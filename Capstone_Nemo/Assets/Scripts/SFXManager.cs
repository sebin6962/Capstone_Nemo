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
}
