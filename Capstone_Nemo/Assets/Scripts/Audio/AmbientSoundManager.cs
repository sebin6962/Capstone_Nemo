using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AmbientSoundManager : MonoBehaviour
{
    public static AmbientSoundManager Instance { get; private set; }

    public enum TimeRule
    {
        Always,
        DayOnly,
        NightOnly,
        CustomRange
    }

    public enum StarRainBehavior
    {
        Ignore,
        DisableDuringStarRain,
        OnlyDuringStarRain,
        ReduceVolumeDuringStarRain
    }

    [Serializable]
    public class AmbientSound
    {
        [Header("Basic")]
        public string soundName = "Ambient";
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 0.25f;

        [Tooltip("긴 환경음 파일은 보통 체크합니다.")]
        public bool loop = true;

        [Header("Play Duration")]
        [Min(0.1f)]
        public float minPlayDuration = 8f;

        [Min(0.1f)]
        public float maxPlayDuration = 20f;

        [Header("Silent Interval")]
        [Min(0f)]
        public float minInterval = 4f;

        [Min(0f)]
        public float maxInterval = 12f;

        [Tooltip("씬 진입/조건 활성 직후 바로 울리지 않고 랜덤 텀부터 둡니다.")]
        public bool waitBeforeFirstPlay = true;

        [Header("Fade")]
        [Min(0f)]
        public float fadeInDuration = 2f;

        [Min(0f)]
        public float fadeOutDuration = 2f;

        [Header("Variation")]
        [Tooltip("매번 클립의 다른 지점에서 시작해서 반복감을 줄입니다.")]
        public bool randomStartPosition = true;

        [Tooltip("재생마다 피치를 약간 랜덤하게 변경합니다.")]
        public bool randomPitch = false;

        public Vector2 pitchRange = new Vector2(0.97f, 1.03f);

        [Header("Time Condition")]
        public TimeRule timeRule = TimeRule.Always;

        [Range(0, 26)]
        public int customStartHour = 9;

        [Range(0, 26)]
        public int customEndHour = 26;

        [Header("Star Rain")]
        public StarRainBehavior starRainBehavior = StarRainBehavior.Ignore;

        [Range(0f, 1f)]
        [Tooltip("ReduceVolumeDuringStarRain일 때 적용되는 음량 배율입니다.")]
        public float starRainVolumeMultiplier = 0.45f;

        [Header("Scene Condition")]
        [Tooltip("비워두면 모든 씬에서 허용됩니다. 정확한 씬 이름을 입력하세요.")]
        public List<string> allowedScenes = new List<string>();

        [NonSerialized] public AudioSource runtimeSource;
        [NonSerialized] public Coroutine runtimeRoutine;
    }

    [Header("Mixer")]
    [Tooltip("기존 SFX와 같은 AudioMixerGroup을 넣으면 효과음 설정을 함께 따릅니다.")]
    [SerializeField] private AudioMixerGroup ambientMixerGroup;

    [Header("Day / Night")]
    [Tooltip("체크하면 BGMPlayer의 nightStartHour를 그대로 사용합니다.")]
    [SerializeField] private bool syncNightStartWithBGMPlayer = true;

    [Range(0, 26)]
    [SerializeField] private int fallbackNightStartHour = 20;

    [Header("Runtime")]
    [SerializeField] private bool ambientEnabled = true;

    [Tooltip("조건이 꺼져 있을 때 재확인하는 간격입니다.")]
    [Min(0.05f)]
    [SerializeField] private float inactiveCheckInterval = 0.25f;

    [Header("Ambient Sounds")]
    [SerializeField] private List<AmbientSound> ambientSounds = new List<AmbientSound>();

    private bool isStarRainActive;

    public bool IsStarRainActive => isStarRainActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateRuntimeAudioSources();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        StartAllRoutines();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 각 환경음 루틴이 현재 씬 이름을 계속 확인하므로
        // 여기서는 별도 재생 명령이 필요하지 않습니다.
    }

    private void CreateRuntimeAudioSources()
    {
        for (int i = 0; i < ambientSounds.Count; i++)
        {
            AmbientSound sound = ambientSounds[i];

            if (sound == null)
                continue;

            GameObject sourceObject =
                new GameObject($"Ambient_{GetSafeName(sound, i)}");

            sourceObject.transform.SetParent(transform);

            AudioSource source =
                sourceObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = sound.loop;
            source.volume = 0f;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = ambientMixerGroup;

            sound.runtimeSource = source;
        }
    }

    private void StartAllRoutines()
    {
        foreach (AmbientSound sound in ambientSounds)
        {
            if (sound == null ||
                sound.runtimeSource == null)
            {
                continue;
            }

            if (sound.runtimeRoutine != null)
                StopCoroutine(sound.runtimeRoutine);

            sound.runtimeRoutine =
                StartCoroutine(AmbientRoutine(sound));
        }
    }

    private IEnumerator AmbientRoutine(AmbientSound sound)
    {
        bool needInitialInterval =
            sound.waitBeforeFirstPlay;

        while (true)
        {
            if (sound.clip == null)
            {
                yield return WaitRealtime(
                    inactiveCheckInterval
                );
                continue;
            }

            // 씬 / 시간 / 날씨 조건이 맞을 때까지 대기
            while (!IsSoundAllowed(sound))
            {
                if (sound.runtimeSource.isPlaying)
                    yield return FadeOutAndStop(sound);

                needInitialInterval =
                    sound.waitBeforeFirstPlay;

                yield return WaitRealtime(
                    inactiveCheckInterval
                );
            }

            // 처음 조건이 맞았을 때도 바로 울리지 않게 랜덤 텀
            if (needInitialInterval)
            {
                float firstInterval =
                    GetRandomInterval(sound);

                yield return WaitIntervalWhileAllowed(
                    sound,
                    firstInterval
                );

                if (!IsSoundAllowed(sound))
                    continue;

                needInitialInterval = false;
            }

            if (!IsSoundAllowed(sound))
                continue;

            PrepareSource(sound);

            sound.runtimeSource.Play();

            // Fade In
            yield return FadeInWhileAllowed(sound);

            if (!IsSoundAllowed(sound))
            {
                yield return FadeOutAndStop(sound);
                needInitialInterval =
                    sound.waitBeforeFirstPlay;
                continue;
            }

            // 실제 재생 유지 시간
            float minPlay =
                Mathf.Max(
                    0.1f,
                    Mathf.Min(
                        sound.minPlayDuration,
                        sound.maxPlayDuration
                    )
                );

            float maxPlay =
                Mathf.Max(
                    minPlay,
                    Mathf.Max(
                        sound.minPlayDuration,
                        sound.maxPlayDuration
                    )
                );

            float playDuration =
                UnityEngine.Random.Range(
                    minPlay,
                    maxPlay
                );

            float elapsed = 0f;

            while (elapsed < playDuration &&
                   IsSoundAllowed(sound))
            {
                // 별빛 비 시작/종료로 목표 음량이 바뀌면
                // 재생을 끊지 않고 부드럽게 이동
                float targetVolume =
                    GetEffectiveVolume(sound);

                sound.runtimeSource.volume =
                    Mathf.MoveTowards(
                        sound.runtimeSource.volume,
                        targetVolume,
                        Time.unscaledDeltaTime / 1.5f
                    );

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // 텀 전에 반드시 Fade Out
            yield return FadeOutAndStop(sound);

            if (!IsSoundAllowed(sound))
            {
                needInitialInterval =
                    sound.waitBeforeFirstPlay;
                continue;
            }

            // 랜덤 휴식
            float interval =
                GetRandomInterval(sound);

            yield return WaitIntervalWhileAllowed(
                sound,
                interval
            );

            if (!IsSoundAllowed(sound))
            {
                needInitialInterval =
                    sound.waitBeforeFirstPlay;
            }
        }
    }

    private void PrepareSource(AmbientSound sound)
    {
        AudioSource source = sound.runtimeSource;

        source.Stop();
        source.clip = sound.clip;
        source.loop = sound.loop;
        source.volume = 0f;

        if (sound.randomPitch)
        {
            float minPitch =
                Mathf.Min(
                    sound.pitchRange.x,
                    sound.pitchRange.y
                );

            float maxPitch =
                Mathf.Max(
                    sound.pitchRange.x,
                    sound.pitchRange.y
                );

            source.pitch =
                UnityEngine.Random.Range(
                    minPitch,
                    maxPitch
                );
        }
        else
        {
            source.pitch = 1f;
        }

        if (sound.randomStartPosition &&
            sound.clip != null &&
            sound.clip.length > 0.05f)
        {
            float maxStart =
                Mathf.Max(
                    0f,
                    sound.clip.length - 0.05f
                );

            source.time =
                UnityEngine.Random.Range(
                    0f,
                    maxStart
                );
        }
        else
        {
            source.time = 0f;
        }
    }

    private IEnumerator FadeInWhileAllowed(
        AmbientSound sound)
    {
        AudioSource source = sound.runtimeSource;

        if (source == null)
            yield break;

        float duration =
            Mathf.Max(
                0f,
                sound.fadeInDuration
            );

        if (duration <= 0f)
        {
            if (IsSoundAllowed(sound))
            {
                source.volume =
                    GetEffectiveVolume(sound);
            }

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!IsSoundAllowed(sound))
                yield break;

            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            source.volume =
                Mathf.Lerp(
                    0f,
                    GetEffectiveVolume(sound),
                    t
                );

            yield return null;
        }

        if (IsSoundAllowed(sound))
        {
            source.volume =
                GetEffectiveVolume(sound);
        }
    }

    private IEnumerator FadeOutAndStop(
        AmbientSound sound)
    {
        AudioSource source = sound.runtimeSource;

        if (source == null)
            yield break;

        if (!source.isPlaying)
        {
            source.volume = 0f;
            yield break;
        }

        float duration =
            Mathf.Max(
                0f,
                sound.fadeOutDuration
            );

        if (duration <= 0f)
        {
            source.volume = 0f;
            source.Stop();
            yield break;
        }

        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            source.volume =
                Mathf.Lerp(
                    startVolume,
                    0f,
                    Mathf.Clamp01(
                        elapsed / duration
                    )
                );

            yield return null;
        }

        source.volume = 0f;
        source.Stop();
    }

    private IEnumerator WaitIntervalWhileAllowed(
        AmbientSound sound,
        float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!IsSoundAllowed(sound))
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitRealtime(float duration)
    {
        if (duration <= 0f)
        {
            yield return null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private bool IsSoundAllowed(
        AmbientSound sound)
    {
        if (!ambientEnabled)
            return false;

        if (sound == null ||
            sound.clip == null)
        {
            return false;
        }

        if (!IsSceneAllowed(sound))
            return false;

        if (!IsTimeAllowed(sound))
            return false;

        if (!IsWeatherAllowed(sound))
            return false;

        return true;
    }

    private bool IsSceneAllowed(
        AmbientSound sound)
    {
        if (sound.allowedScenes == null ||
            sound.allowedScenes.Count == 0)
        {
            return true;
        }

        string currentScene =
            SceneManager.GetActiveScene().name;

        for (int i = 0;
             i < sound.allowedScenes.Count;
             i++)
        {
            if (string.Equals(
                sound.allowedScenes[i],
                currentScene,
                StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTimeAllowed(
        AmbientSound sound)
    {
        if (sound.timeRule == TimeRule.Always)
            return true;

        TimeManager timeManager =
            TimeManager.Instance;

        if (timeManager == null)
            return false;

        int hour = timeManager.hour;
        int nightStartHour =
            GetNightStartHour();

        switch (sound.timeRule)
        {
            case TimeRule.DayOnly:
                return hour < nightStartHour;

            case TimeRule.NightOnly:
                return hour >= nightStartHour;

            case TimeRule.CustomRange:
                return IsHourInRange(
                    hour,
                    sound.customStartHour,
                    sound.customEndHour
                );
        }

        return true;
    }

    private bool IsWeatherAllowed(
        AmbientSound sound)
    {
        switch (sound.starRainBehavior)
        {
            case StarRainBehavior.DisableDuringStarRain:
                return !isStarRainActive;

            case StarRainBehavior.OnlyDuringStarRain:
                return isStarRainActive;

            case StarRainBehavior.Ignore:
            case StarRainBehavior.ReduceVolumeDuringStarRain:
            default:
                return true;
        }
    }

    private float GetEffectiveVolume(
        AmbientSound sound)
    {
        float result =
            Mathf.Clamp01(sound.volume);

        if (isStarRainActive &&
            sound.starRainBehavior ==
            StarRainBehavior.ReduceVolumeDuringStarRain)
        {
            result *=
                Mathf.Clamp01(
                    sound.starRainVolumeMultiplier
                );
        }

        return result;
    }

    private int GetNightStartHour()
    {
        if (syncNightStartWithBGMPlayer &&
            BGMPlayer.Instance != null)
        {
            return BGMPlayer.Instance.nightStartHour;
        }

        return fallbackNightStartHour;
    }

    private bool IsHourInRange(
        int currentHour,
        int startHour,
        int endHour)
    {
        startHour =
            Mathf.Clamp(startHour, 0, 26);

        endHour =
            Mathf.Clamp(endHour, 0, 26);

        if (startHour == endHour)
            return true;

        if (startHour < endHour)
        {
            return currentHour >= startHour &&
                   currentHour < endHour;
        }

        // 22 -> 6처럼 자정을 넘기는 범위도 지원
        return currentHour >= startHour ||
               currentHour < endHour;
    }

    private float GetRandomInterval(
        AmbientSound sound)
    {
        float min =
            Mathf.Max(
                0f,
                Mathf.Min(
                    sound.minInterval,
                    sound.maxInterval
                )
            );

        float max =
            Mathf.Max(
                min,
                Mathf.Max(
                    sound.minInterval,
                    sound.maxInterval
                )
            );

        return UnityEngine.Random.Range(
            min,
            max
        );
    }

    private string GetSafeName(
        AmbientSound sound,
        int index)
    {
        if (sound == null ||
            string.IsNullOrWhiteSpace(
                sound.soundName
            ))
        {
            return index.ToString();
        }

        return sound.soundName.Replace(
            " ",
            "_"
        );
    }

    // ---------------------------------------------------------
    // External Control
    // ---------------------------------------------------------

    public void SetStarRainActive(bool active)
    {
        isStarRainActive = active;
    }

    public void SetAmbientEnabled(bool enabled)
    {
        ambientEnabled = enabled;
    }

    public void StopAllAmbientImmediate()
    {
        foreach (AmbientSound sound in ambientSounds)
        {
            if (sound == null ||
                sound.runtimeSource == null)
            {
                continue;
            }

            sound.runtimeSource.volume = 0f;
            sound.runtimeSource.Stop();
        }
    }
}

