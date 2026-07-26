using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMPlayer : MonoBehaviour
{
    public static BGMPlayer Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Scene BGM")]
    public AudioClip startBGM;
    public AudioClip cutsceneBGM;
    public AudioClip mainBGM;
    public AudioClip treeBGM;

    [Header("Night BGM")]
    public AudioClip nightBGM;

    [Range(0, 25)]
    public int nightStartHour = 20;

    [Header("Dialogue BGM")]
    public AudioClip dialogueBGM;

    [Range(0f, 1f)]
    public float dialogueTargetVolume = 0.9f;

    [Header("Fade")]
    public float fadeDuration = 1.5f;

    [Range(0f, 1f)]
    public float targetVolume = 0.7f;

    private Coroutine currentFade;

    // 현재 요청된 BGM과 볼륨
    private AudioClip requestedClip;
    private float requestedTargetVolume;

    // 현재 씬의 기본 낮 BGM
    private AudioClip currentSceneDayBGM;

    // 현재 씬이 낮/밤 BGM 전환을 사용하는지
    private bool currentSceneUsesDayNightBGM;

    private bool isNightBGMPlaying;
    private bool isDialogueBGMPlaying;

    // 대화 시작 전에 재생 중이던 BGM 정보
    private AudioClip pausedSceneClip;
    private int pausedSceneTimeSamples;
    private bool hasPausedScenePosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        // 대화 중에는 낮/밤 BGM이 대화 BGM을 덮어쓰지 않음
        if (isDialogueBGMPlaying)
            return;

        if (!currentSceneUsesDayNightBGM)
            return;

        TimeManager timeManager = TimeManager.Instance;

        if (timeManager == null)
            return;

        // 시간 정지 중에는 낮/밤 전환하지 않음
        if (!timeManager.isTimeFlow)
            return;

        bool shouldPlayNightBGM = IsNightTime(timeManager);

        if (shouldPlayNightBGM == isNightBGMPlaying)
            return;

        isNightBGMPlaying = shouldPlayNightBGM;

        AudioClip nextClip = shouldPlayNightBGM
            ? nightBGM
            : currentSceneDayBGM;

        PlayBGM(nextClip, targetVolume);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneBGM(scene.name);
    }

    private void PlaySceneBGM(string sceneName)
    {
        AudioClip sceneDayBGM = null;
        bool usesDayNightBGM = false;

        switch (sceneName)
        {
            case "IntroScene":
            case "SaveSelectScene":
                sceneDayBGM = startBGM;
                break;

            case "CutScene":
                sceneDayBGM = cutsceneBGM;
                break;

            case "MarketScene":
            case "MillScene":
            case "PlayerStoreScene":
            case "VillageScene":
                sceneDayBGM = mainBGM;
                usesDayNightBGM = true;
                break;

            case "TreeScene":
                sceneDayBGM = treeBGM;
                usesDayNightBGM = false;
                break;
        }

        currentSceneDayBGM = sceneDayBGM;
        currentSceneUsesDayNightBGM = usesDayNightBGM;

        bool shouldPlayNightBGM =
            usesDayNightBGM &&
            TimeManager.Instance != null &&
            IsNightTime(TimeManager.Instance);

        isNightBGMPlaying = shouldPlayNightBGM;

        AudioClip nextClip;
        float nextVolume;

        // 대화 중 씬이 전환되더라도 대화 BGM 유지
        if (isDialogueBGMPlaying && dialogueBGM != null)
        {
            nextClip = dialogueBGM;
            nextVolume = dialogueTargetVolume;
        }
        else
        {
            nextClip = shouldPlayNightBGM
                ? nightBGM
                : sceneDayBGM;

            nextVolume = targetVolume;
        }

        PlayBGM(nextClip, nextVolume);
    }

    private bool IsNightTime(TimeManager timeManager)
    {
        return nightBGM != null &&
               timeManager.hour >= nightStartHour;
    }

    // 대화창이 열릴 때 호출
    public void StartDialogueBGM()
    {
        if (dialogueBGM == null)
            return;

        if (isDialogueBGMPlaying)
        {
            PlayBGM(dialogueBGM, dialogueTargetVolume);
            return;
        }

        isDialogueBGMPlaying = true;

        // 현재 재생 중인 원래 BGM과 재생 위치 저장
        if (audioSource.clip != null &&
            audioSource.clip != dialogueBGM)
        {
            pausedSceneClip = audioSource.clip;
            pausedSceneTimeSamples = audioSource.timeSamples;
            hasPausedScenePosition = true;
        }
        else
        {
            ClearPausedScenePosition();
        }

        requestedClip = dialogueBGM;
        requestedTargetVolume = dialogueTargetVolume;

        if (currentFade != null)
        {
            StopCoroutine(currentFade);
            currentFade = null;
        }

        currentFade = StartCoroutine(
            FadeToDialogueBGM()
        );
    }

    private IEnumerator FadeToDialogueBGM()
    {
        // 원래 BGM 페이드아웃
        if (audioSource.isPlaying &&
            audioSource.clip != null &&
            fadeDuration > 0f)
        {
            AudioClip originalClip = audioSource.clip;
            float startVolume = audioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                audioSource.volume = Mathf.Lerp(
                    startVolume,
                    0f,
                    Mathf.Clamp01(elapsed / fadeDuration)
                );

                // 페이드아웃되는 동안 계속 진행된 위치도 저장
                if (audioSource.clip == originalClip &&
                    originalClip == pausedSceneClip)
                {
                    pausedSceneTimeSamples =
                        audioSource.timeSamples;
                }

                yield return null;
            }
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        audioSource.clip = dialogueBGM;

        if (dialogueBGM != null)
        {
            audioSource.timeSamples = 0;
            audioSource.Play();
        }

        // 대화 BGM 페이드인
        if (fadeDuration > 0f && dialogueBGM != null)
        {
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                audioSource.volume = Mathf.Lerp(
                    0f,
                    dialogueTargetVolume,
                    Mathf.Clamp01(elapsed / fadeDuration)
                );

                yield return null;
            }
        }

        audioSource.volume = dialogueBGM != null
            ? dialogueTargetVolume
            : 0f;

        currentFade = null;
    }

    // 대화창이 닫힐 때 호출
    public void StopDialogueBGM()
    {
        if (!isDialogueBGMPlaying)
            return;

        isDialogueBGMPlaying = false;

        AudioClip returnClip = GetCurrentSceneBGM();

        if (currentFade != null)
        {
            StopCoroutine(currentFade);
            currentFade = null;
        }

        /*
         * 대화 BGM으로 완전히 넘어가기 전에
         * 대화창이 바로 닫힌 경우입니다.
         *
         * 아직 원래 BGM이 재생 중이므로 재시작하지 않고
         * 현재 위치에서 볼륨만 다시 올립니다.
         */
        if (hasPausedScenePosition &&
            audioSource.clip == pausedSceneClip &&
            audioSource.clip != dialogueBGM &&
            returnClip == pausedSceneClip)
        {
            requestedClip = returnClip;
            requestedTargetVolume = targetVolume;

            currentFade = StartCoroutine(
                FadeCurrentVolumeTo(targetVolume)
            );

            ClearPausedScenePosition();
            return;
        }

        bool canResume =
            hasPausedScenePosition &&
            returnClip != null &&
            returnClip == pausedSceneClip;

        int resumeTimeSamples = canResume
            ? pausedSceneTimeSamples
            : 0;

        ClearPausedScenePosition();

        StartBGMTransition(
            returnClip,
            targetVolume,
            canResume,
            resumeTimeSamples
        );
    }

    private AudioClip GetCurrentSceneBGM()
    {
        bool shouldPlayNightBGM =
            currentSceneUsesDayNightBGM &&
            TimeManager.Instance != null &&
            IsNightTime(TimeManager.Instance);

        isNightBGMPlaying = shouldPlayNightBGM;

        if (shouldPlayNightBGM)
            return nightBGM;

        return currentSceneDayBGM;
    }

    private void ClearPausedScenePosition()
    {
        pausedSceneClip = null;
        pausedSceneTimeSamples = 0;
        hasPausedScenePosition = false;
    }

    // 일반 BGM 볼륨으로 재생
    public void PlayBGM(AudioClip newClip)
    {
        PlayBGM(newClip, targetVolume);
    }

    // 지정한 볼륨으로 재생
    public void PlayBGM(
        AudioClip newClip,
        float newTargetVolume)
    {
        newTargetVolume =
            Mathf.Clamp01(newTargetVolume);

        // 같은 음악과 같은 볼륨으로 전환 중이면 중복 방지
        if (currentFade != null &&
            requestedClip == newClip &&
            Mathf.Approximately(
                requestedTargetVolume,
                newTargetVolume
            ))
        {
            return;
        }

        // 같은 음악이 이미 재생 중이면 재시작하지 않음
        if (currentFade == null &&
            audioSource.clip == newClip &&
            audioSource.isPlaying)
        {
            audioSource.volume = newTargetVolume;

            requestedClip = newClip;
            requestedTargetVolume = newTargetVolume;

            return;
        }

        StartBGMTransition(
            newClip,
            newTargetVolume,
            false,
            0
        );
    }

    private void StartBGMTransition(
        AudioClip newClip,
        float newTargetVolume,
        bool resumeFromSavedPosition,
        int startTimeSamples)
    {
        newTargetVolume =
            Mathf.Clamp01(newTargetVolume);

        requestedClip = newClip;
        requestedTargetVolume = newTargetVolume;

        if (currentFade != null)
        {
            StopCoroutine(currentFade);
            currentFade = null;
        }

        currentFade = StartCoroutine(
            FadeAndPlay(
                newClip,
                newTargetVolume,
                resumeFromSavedPosition,
                startTimeSamples
            )
        );
    }

    private IEnumerator FadeAndPlay(
        AudioClip newClip,
        float newTargetVolume,
        bool resumeFromSavedPosition,
        int startTimeSamples)
    {
        // 현재 BGM 페이드아웃
        if (audioSource.isPlaying &&
            audioSource.clip != null &&
            fadeDuration > 0f)
        {
            float startVolume = audioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                audioSource.volume = Mathf.Lerp(
                    startVolume,
                    0f,
                    Mathf.Clamp01(elapsed / fadeDuration)
                );

                yield return null;
            }
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        audioSource.clip = newClip;

        if (newClip != null)
        {
            if (resumeFromSavedPosition &&
                newClip.samples > 0)
            {
                audioSource.timeSamples = Mathf.Clamp(
                    startTimeSamples,
                    0,
                    newClip.samples - 1
                );
            }
            else
            {
                audioSource.timeSamples = 0;
            }

            audioSource.Play();
        }

        // 새로운 BGM 페이드인
        if (fadeDuration > 0f &&
            newClip != null)
        {
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                audioSource.volume = Mathf.Lerp(
                    0f,
                    newTargetVolume,
                    Mathf.Clamp01(elapsed / fadeDuration)
                );

                yield return null;
            }
        }

        audioSource.volume = newClip != null
            ? newTargetVolume
            : 0f;

        currentFade = null;
    }

    // 음악을 재시작하지 않고 현재 위치에서 볼륨만 복구
    private IEnumerator FadeCurrentVolumeTo(
        float newTargetVolume)
    {
        newTargetVolume =
            Mathf.Clamp01(newTargetVolume);

        if (!audioSource.isPlaying ||
            fadeDuration <= 0f)
        {
            audioSource.volume = newTargetVolume;
            currentFade = null;
            yield break;
        }

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            audioSource.volume = Mathf.Lerp(
                startVolume,
                newTargetVolume,
                Mathf.Clamp01(elapsed / fadeDuration)
            );

            yield return null;
        }

        audioSource.volume = newTargetVolume;
        currentFade = null;
    }
}