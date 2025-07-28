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
        audioMixer.SetFloat(audioMixerType.ToString(), Mathf.Log10(volume) * 20);
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
}
