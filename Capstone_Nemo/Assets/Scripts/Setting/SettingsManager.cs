using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-999)]


public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [SerializeField] private GameObject audioSettingPrefab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]

     static void EnsureCreated()
    {
        if (Instance == null)
        {
            //아니 이거 prefab에서는 못불러오고 Resources에서만 불러올수있음 내가 방법을 못찾는건가?
            GameObject prefab = Resources.Load<GameObject>("SettingsManager");
            if (prefab != null)
            {
                GameObject obj = Object.Instantiate(prefab);
                obj.name = "SettingsManager"; // Clone 제거
                Debug.Log("[SettingsManager] 자동 생성됨 (Resources)");
            }
            else
            {
                Debug.LogError("SettingsManager 프리팹을 찾을 수 없습니다!");
            }
        }
    }

    public float masterVolume = 1f;
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;

    public bool masterMute = false;
    public bool bgmMute = false;
    public bool sfxMute = false;

    public float UIScale = 1f;
    public float brightness = 1f;
    public bool isFullScreen = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSetting();   
            LoadSettings();        
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ApplyToAudio();
    }

    private void EnsureAudioSetting()
    {
        if (AudioSetting.Instance != null)
            return;

        GameObject prefab = Resources.Load<GameObject>("AudioSetting");

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = "AudioSetting";
            DontDestroyOnLoad(obj);
        }
        else
        {
            Debug.LogError("[SettingsManager] Resources/AudioSetting 프리팹을 찾을 수 없습니다.");
        }
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

        PlayerPrefs.SetInt("MasterMute", masterMute ? 1 : 0);
        PlayerPrefs.SetInt("BGMMute", bgmMute ? 1 : 0);
        PlayerPrefs.SetInt("SFXMute", sfxMute ? 1 : 0);

        PlayerPrefs.SetFloat("UIScale", UIScale);
        PlayerPrefs.SetInt("IsFullScreen", isFullScreen ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log($"설정 저장됨");
    }

    public void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        masterMute = PlayerPrefs.GetInt("MasterMute", 0) == 1;
        bgmMute = PlayerPrefs.GetInt("BGMMute", 0) == 1;
        sfxMute = PlayerPrefs.GetInt("SFXMute", 0) == 1;

        UIScale = PlayerPrefs.GetFloat("UIScale", 1f);
        isFullScreen = PlayerPrefs.GetInt("IsFullScreen", 1) == 1;

        if (AudioSetting.Instance != null)
        {
            AudioSetting.Instance.SetAudioVolume(EAudioMixerType.Master, masterVolume);
            AudioSetting.Instance.SetAudioVolume(EAudioMixerType.BGM, bgmVolume);
            AudioSetting.Instance.SetAudioVolume(EAudioMixerType.SFX, sfxVolume);
        }
    }

    private void ApplyToAudio()
    {
        if (AudioSetting.Instance == null) return;

        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.Master, masterVolume);
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.BGM, bgmVolume);
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.SFX, sfxVolume);

        AudioSetting.Instance.SetMasterMute(masterMute);
        AudioSetting.Instance.SetBGMMute(bgmMute);
        AudioSetting.Instance.SetSFXMute(sfxMute);
    }
}
