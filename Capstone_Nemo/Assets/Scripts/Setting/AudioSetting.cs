using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum EAudioMixerType 
{ 
    Master, 
    BGM, 
    SFX 
}

public class AudioSetting : MonoBehaviour
{
    public static AudioSetting Instance;
    [SerializeField] private AudioMixer audioMixer;

    private bool[] isMute = new bool[3];
    private float[] audioVolumes = new float[3];

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsMuted(EAudioMixerType audioMixerType)
    {
        return isMute[(int)audioMixerType];
    }

    public void SetAudioVolume(EAudioMixerType audioMixerType, float volume)
    {
        int type = (int)audioMixerType;

        float safeVolume = Mathf.Clamp(volume, 0.001f, 1f);

        //마지막 볼륨 기억
        audioVolumes[type] = safeVolume;

        //뮤트 상태면 믹서값 건들X
        if (isMute[type])
            return;

        audioMixer.SetFloat(audioMixerType.ToString(), Mathf.Log10(safeVolume) * 20);
    }


    public void SetAudioMute(EAudioMixerType audioMixerType)
    {
        int type = (int)audioMixerType;
        bool nextMute = !isMute[type];

        if (nextMute)
        {
            //현재 설정값을 기억한 뒤 음소거
            if (audioMixer.GetFloat(audioMixerType.ToString(), out float currentDb))
            {
                audioVolumes[type] = Mathf.Pow(10f, currentDb / 20f);
            }

            isMute[type] = true;
            audioMixer.SetFloat(audioMixerType.ToString(), Mathf.Log10(0.001f) * 20);
        }
        else
        {
            isMute[type] = false;
            float restore = audioVolumes[type] > 0 ? audioVolumes[type] : 1f;
            audioMixer.SetFloat(audioMixerType.ToString(), Mathf.Log10(restore) * 20);
        }
    }

    public void MuteMaster()
    {
        SFXManager.Instance.PlayBtnClickSFX();
        AudioSetting.Instance.SetAudioMute(EAudioMixerType.Master);
    }

    public void ChangeMasterVolume(float volume)
    {
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.Master, volume);
    }

    public void MuteBGM()
    {
        SFXManager.Instance.PlayBtnClickSFX();
        AudioSetting.Instance.SetAudioMute(EAudioMixerType.BGM);
    }

    public void ChangeBGMVolume(float volume)
    {
        Debug.Log($"슬라이더 변경됨: {volume}");
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.BGM, volume);
    }

    public void MuteSFX()
    {
        SFXManager.Instance.PlayBtnClickSFX();
        AudioSetting.Instance.SetAudioMute(EAudioMixerType.SFX);
    }

    public void ChangeSFXVolume(float volume)
    {
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.SFX, volume);
    }

    public void SetMasterMute(bool isOn)
    {
        int type = (int)EAudioMixerType.Master;
        isMute[type] = !isOn;

        if (isOn)
        {
            float restore = audioVolumes[type] > 0 ? audioVolumes[type] : Mathf.Clamp(SettingsManager.Instance.masterVolume, 0.001f, 1f);
            audioMixer.SetFloat(EAudioMixerType.Master.ToString(), Mathf.Log10(restore) * 20);
        }
        else
        {
            audioMixer.SetFloat(EAudioMixerType.Master.ToString(), Mathf.Log10(0.001f) * 20);
        }
    }

    public void SetBGMMute(bool isOn)
    {
        int type = (int)EAudioMixerType.BGM;
        isMute[type] = !isOn;

        if (isOn)
        {
            float restore = audioVolumes[type] > 0 ? audioVolumes[type] : Mathf.Clamp(SettingsManager.Instance.bgmVolume, 0.001f, 1f);
            audioMixer.SetFloat(EAudioMixerType.BGM.ToString(), Mathf.Log10(restore) * 20);
        }
        else
        {
            audioMixer.SetFloat(EAudioMixerType.BGM.ToString(), Mathf.Log10(0.001f) * 20);
        }
    }

    public void SetSFXMute(bool isOn)
    {
        int type = (int)EAudioMixerType.SFX;
        isMute[type] = !isOn;

        if (isOn)
        {
            float restore = audioVolumes[type] > 0 ? audioVolumes[type] : Mathf.Clamp(SettingsManager.Instance.sfxVolume, 0.001f, 1f);
            audioMixer.SetFloat(EAudioMixerType.SFX.ToString(), Mathf.Log10(restore) * 20);
        }
        else
        {
            audioMixer.SetFloat(EAudioMixerType.SFX.ToString(), Mathf.Log10(0.001f) * 20);
        }
    }
}
