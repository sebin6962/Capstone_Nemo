using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MakerDetailImageAnimator : MonoBehaviour
{
    [Header("출력 대상")]
    [SerializeField] private Image targetImage;

    [Header("애니메이션 설정")]
    [Min(1)]
    [SerializeField] private int frameCount = 4;

    [Min(0.01f)]
    [SerializeField] private float frameInterval = 0.15f;

    [Tooltip("제작대를 선택한 뒤 첫 재생까지의 시간")]
    [Min(0f)]
    [SerializeField] private float firstPlayDelay = 0.3f;

    [Tooltip("한 번 재생한 뒤 다음 재생까지의 시간")]
    [Min(0f)]
    [SerializeField] private float repeatDelay = 3.5f;

    [Tooltip("한 번 재생할 때 애니메이션을 반복할 횟수")]
    [Min(1)]
    [SerializeField] private int loopsPerPlay = 3;

    [Header("Resources 경로 설정")]
    [SerializeField]
    private string spriteFolder = "Sprites/restaurant/Maker/anim";

    [SerializeField]
    private string animationPrefix = "restaurant_anim_";

    private readonly List<Sprite> currentFrames = new();

    private Sprite currentIdleSprite;
    private string currentMakerImageName;

    private Coroutine playbackCoroutine;

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(currentMakerImageName) &&
            currentFrames.Count > 0)
        {
            StartPlayback();
        }
    }

    private void OnDisable()
    {
        StopPlayback();
        SetIdleSprite();
    }

    /// <summary>
    /// 새로운 제작대가 선택됐을 때 호출
    /// </summary>
    public void SetMaker(
        string makerImageName,
        Sprite idleSprite)
    {
        StopPlayback();

        currentMakerImageName = makerImageName;
        currentIdleSprite = idleSprite;

        LoadAnimationFrames(makerImageName);
        SetIdleSprite();

        if (isActiveAndEnabled && currentFrames.Count > 0)
            StartPlayback();
    }

    private void LoadAnimationFrames(string makerImageName)
    {
        currentFrames.Clear();

        if (string.IsNullOrWhiteSpace(makerImageName))
        {
            Debug.LogWarning(
                "[MakerDetailImageAnimator] 제작대 이미지 이름이 비어 있습니다."
            );
            return;
        }

        string normalizedName = NormalizeMakerName(makerImageName);

        // _0을 붙이지 않고 원본 PNG 파일 경로를 지정
        string sheetPath =
            spriteFolder + animationPrefix + normalizedName;

        // 한 PNG 안에 잘린 모든 서브 스프라이트 불러오기
        Sprite[] loadedSprites =
            Resources.LoadAll<Sprite>(sheetPath);

        if (loadedSprites == null || loadedSprites.Length == 0)
        {
            Debug.LogWarning(
                $"[MakerDetailImageAnimator] 스프라이트 시트를 찾지 못했습니다: " +
                $"{sheetPath}"
            );
            return;
        }

        // _0, _1, _2, _3 순서로 정확한 서브 스프라이트 검색
        for (int i = 0; i < frameCount; i++)
        {
            string expectedSpriteName =
                animationPrefix + normalizedName + "_" + i;

            Sprite foundFrame = null;

            foreach (Sprite loadedSprite in loadedSprites)
            {
                if (loadedSprite.name == expectedSpriteName)
                {
                    foundFrame = loadedSprite;
                    break;
                }
            }

            if (foundFrame == null)
            {
                Debug.LogWarning(
                    $"[MakerDetailImageAnimator] 서브 스프라이트를 찾지 못했습니다: " +
                    $"{expectedSpriteName}"
                );
                continue;
            }

            currentFrames.Add(foundFrame);
        }

        if (currentFrames.Count == 0)
        {
            Debug.LogWarning(
                $"[MakerDetailImageAnimator] " +
                $"{normalizedName}의 애니메이션 프레임이 없습니다."
            );
        }
        else
        {
            Debug.Log(
                $"[MakerDetailImageAnimator] {normalizedName} 프레임 " +
                $"{currentFrames.Count}개 로드 완료"
            );
        }
    }

    private string NormalizeMakerName(string makerImageName)
    {
        string normalized = makerImageName.Trim();

        int extensionIndex = normalized.LastIndexOf('.');

        if (extensionIndex >= 0)
            normalized = normalized.Substring(0, extensionIndex);

        return normalized
            .Replace(" ", "")
            .ToLowerInvariant();
    }

    private void StartPlayback()
    {
        StopPlayback();

        playbackCoroutine =
            StartCoroutine(PlaybackRoutine());
    }

    private void StopPlayback()
    {
        if (playbackCoroutine == null)
            return;

        StopCoroutine(playbackCoroutine);
        playbackCoroutine = null;
    }

    private IEnumerator PlaybackRoutine()
    {
        if (firstPlayDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                firstPlayDelay
            );
        }

        while (true)
        {
            yield return PlayOnceRoutine();

            SetIdleSprite();

            if (repeatDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    repeatDelay
                );
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator PlayOnceRoutine()
    {
        if (targetImage == null || currentFrames.Count == 0)
            yield break;

        // 한 번의 재생 요청마다 전체 프레임을 3회 반복
        for (int loop = 0; loop < loopsPerPlay; loop++)
        {
            foreach (Sprite frame in currentFrames)
            {
                if (targetImage == null)
                    yield break;

                if (frame != null)
                {
                    targetImage.sprite = frame;
                    targetImage.enabled = true;
                }

                yield return new WaitForSecondsRealtime(frameInterval);
            }
        }
    }

    private void SetIdleSprite()
    {
        if (targetImage == null)
            return;

        targetImage.sprite = currentIdleSprite;
        targetImage.enabled = currentIdleSprite != null;
        targetImage.preserveAspect = true;
        targetImage.color = Color.white;
    }

    public void ClearMaker()
    {
        StopPlayback();

        currentMakerImageName = null;
        currentIdleSprite = null;
        currentFrames.Clear();

        if (targetImage != null)
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
        }
    }
}
