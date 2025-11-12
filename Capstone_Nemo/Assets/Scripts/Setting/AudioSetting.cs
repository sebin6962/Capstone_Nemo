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

    public void SetAudioVolume(EAudioMixerType audioMixerType, float volume)
    {
        float safeVolume = Mathf.Clamp(volume, 0.001f, 1f);
        audioMixer.SetFloat(audioMixerType.ToString(), Mathf.Log10(safeVolume) * 20);
    }


    public void SetAudioMute(EAudioMixerType audioMixerType)
    {
        int type = (int)audioMixerType;
        if (!isMute[type]) 
        {
            isMute[type] = true;
            audioMixer.GetFloat(audioMixerType.ToString(), out float curVolume);
            audioVolumes[type] = curVolume;
            SetAudioVolume(audioMixerType, 0.001f);
        }
        else 
        {
            isMute[type] = false;
            SetAudioVolume(audioMixerType, audioVolumes[type]);
        }
    }

    public void MuteMaster()
    {
        AudioSetting.Instance.SetAudioMute(EAudioMixerType.Master);
    }

    public void ChangeMasterVolume(float volume)
    {
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.Master, volume);
    }

    public void MuteBGM()
    {
        AudioSetting.Instance.SetAudioMute(EAudioMixerType.BGM);
    }

    public void ChangeBGMVolume(float volume)
    {
        Debug.Log($"슬라이더 변경됨: {volume}");
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.BGM, volume);
    }

    public void MuteSFX()
    {
        AudioSetting.Instance.SetAudioMute(EAudioMixerType.SFX);
    }

    public void ChangeSFXVolume(float volume)
    {
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.SFX, volume);
    }

    public void SetMasterMute(bool isOn)
    {
        int type = (int)EAudioMixerType.Master;
        isMute[type] = isOn;

        if (!isOn)
        {
            if (audioMixer.GetFloat(EAudioMixerType.Master.ToString(), out float currentDb))
            {
                audioVolumes[type] = Mathf.Pow(10f, currentDb / 20f);
            }

            SetAudioVolume(EAudioMixerType.Master, 0.001f);
        }
        else
        {
            float restore = audioVolumes[type] > 0 ? audioVolumes[type] : 1f;
            SetAudioVolume(EAudioMixerType.Master, restore);
        }
    }

    public void SetBGMMute(bool isOn)
    {
        int type = (int)EAudioMixerType.BGM;
        isMute[type] = isOn;

        if (!isOn) 
        {
            if (audioMixer.GetFloat(EAudioMixerType.BGM.ToString(), out float currentDb))
            {
                audioVolumes[type] = Mathf.Pow(10f, currentDb / 20f);
            }

            SetAudioVolume(EAudioMixerType.BGM, 0.001f); 
        }
        else 
        {
            float restore = audioVolumes[type] > 0 ? audioVolumes[type] : 1f;
            SetAudioVolume(EAudioMixerType.BGM, restore);
        }
    }

    public void SetSFXMute(bool isOn)
    {
        int type = (int)EAudioMixerType.SFX;
        isMute[type] = isOn;

        if (!isOn)
        {
            if (audioMixer.GetFloat(EAudioMixerType.SFX.ToString(), out float currentDb))
            {
                audioVolumes[type] = Mathf.Pow(10f, currentDb / 20f);
            }

            SetAudioVolume(EAudioMixerType.SFX, 0.001f);
        }
        else
        {
            float restore = audioVolumes[type] > 0 ? audioVolumes[type] : 1f;
            SetAudioVolume(EAudioMixerType.SFX, restore);
        }
    }
}
