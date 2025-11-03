using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMPlayer : MonoBehaviour
{
    private static BGMPlayer instance;
    public AudioSource audioSource;

    public AudioClip startBGM;
    public AudioClip cutsceneBGM;
    public AudioClip mainBGM;
    public AudioClip treeBGM;

    public float fadeDuration = 1.5f;
    public float targetVolume = 0.7f;

    private Coroutine currentFade;

    void Awake()
    {
        // 이미 존재하는 BGMPlayer가 있으면 새로 생성된 건 파괴
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않도록 함

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneBGM(scene.name);
    }

    //BGM 교체 함수(예: 특정 씬에서 음악 바꾸기)도 이후에 추가
    void PlaySceneBGM(string sceneName)
    {
        AudioClip newClip = null;

        switch (sceneName)
        {
            case "IntroScene":
            case "SaveSelectScene":
                newClip = startBGM;
                break;
            case "CutScene":
                newClip = cutsceneBGM;
                break;
            case "MarketScene":
            case "MillScene":
            case "PlayerStoreScene":
            case "VillageScene":
                newClip = mainBGM;
                break;
            case "TreeScene":
                newClip = treeBGM;
                break;
        }
        if (audioSource.clip == newClip && audioSource.isPlaying)
            return;

        PlayBGM(newClip);
    }

    public void PlayBGM(AudioClip newClip)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeAndPlay(newClip));
    }

    private IEnumerator FadeAndPlay(AudioClip newClip)
    {
        //페이드아웃
        if (audioSource.isPlaying && audioSource.clip != null && fadeDuration > 0f)
        {
            float start = audioSource.volume;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(start, 0f, t / fadeDuration);
                yield return null;
            }
            audioSource.volume = 0f;
            audioSource.Stop();
        }
        else
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }

        audioSource.clip = newClip;
        if (newClip != null) audioSource.Play();

        if (fadeDuration > 0f && newClip != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeDuration);
                yield return null;
            }
        }
        audioSource.volume = newClip != null ? targetVolume : 0f;

        currentFade = null;
    }
}
