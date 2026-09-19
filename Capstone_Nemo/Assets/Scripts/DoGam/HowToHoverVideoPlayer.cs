using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 게임 방법 도감용 영상 플레이어.
///
/// 기본 상태:
/// - PNG 썸네일만 즉시 표시
/// - VideoPlayer는 준비하지 않음
///
/// 마우스를 처음 올렸을 때:
/// - 해당 영상만 Prepare
/// - 준비가 끝나면 썸네일을 숨기고 영상 재생
///
/// 영상 종료 후:
/// - 마지막 프레임에서 정지
/// - 다시 마우스를 올리면 처음부터 재생
///
/// 마우스가 영상 영역에서 나가더라도
/// 이미 시작한 영상은 끝까지 재생.
/// </summary>
public class HowToHoverVideoPlayer : MonoBehaviour, IPointerEnterHandler
{
    private const int MaxRenderTextureSize = 512;

    private VideoPlayer videoPlayer;

    private RawImage videoImage;
    private Image thumbnailImage;

    private AspectRatioFitter videoAspectRatioFitter;
    private AspectRatioFitter thumbnailAspectRatioFitter;

    private RenderTexture renderTexture;

    private VideoClip currentClip;
    private Sprite currentThumbnail;

    // Prepare가 이미 요청되었는지
    private bool prepareRequested;

    // Prepare가 끝나는 즉시 재생해야 하는지
    private bool playWhenPrepared;


    public void Setup(VideoClip clip, Sprite thumbnail)
    {
        EnsureComponents();

        // 이전 영상 정리
        ResetVideo();

        currentClip = clip;
        currentThumbnail = thumbnail;

        // -------------------------
        // 썸네일 즉시 표시
        // -------------------------
        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = thumbnail;
            thumbnailImage.enabled = thumbnail != null;
            thumbnailImage.preserveAspect = true;
        }

        // 영상은 처음에는 숨김
        if (videoImage != null)
        {
            videoImage.enabled = false;
            videoImage.texture = null;
        }

        // 아직 영상 Prepare 하지 않음
        if (videoPlayer != null)
        {
            videoPlayer.clip = clip;
        }

        // 썸네일 비율 설정
        if (thumbnail != null && thumbnailAspectRatioFitter != null)
        {
            Rect spriteRect = thumbnail.rect;

            float width = Mathf.Max(1f, spriteRect.width);
            float height = Mathf.Max(1f, spriteRect.height);

            thumbnailAspectRatioFitter.aspectRatio = width / height;
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentClip == null)
            return;

        // 이미 재생 중이면 아무것도 하지 않음
        if (videoPlayer != null && videoPlayer.isPlaying)
            return;

        // 이미 Prepare 완료된 영상이라면
        // 바로 처음부터 다시 재생
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            ShowVideo();
            PlayFromBeginning();
            return;
        }

        // Prepare 중이라면 완료되는 순간 재생
        if (prepareRequested)
        {
            playWhenPrepared = true;
            return;
        }

        // 최초 마우스 오버
        PrepareVideo();
    }


    private void PrepareVideo()
    {
        if (currentClip == null || videoPlayer == null)
            return;

        prepareRequested = true;
        playWhenPrepared = true;

        // 실제 영상을 재생하려는 순간에만
        // RenderTexture 생성
        CreateRenderTexture(currentClip);

        videoPlayer.clip = currentClip;
        videoPlayer.targetTexture = renderTexture;

        if (videoImage != null)
        {
            videoImage.texture = renderTexture;
        }

        // 영상 비율 설정
        if (videoAspectRatioFitter != null)
        {
            float width = Mathf.Max(1f, currentClip.width);
            float height = Mathf.Max(1f, currentClip.height);

            videoAspectRatioFitter.aspectRatio = width / height;
        }

        // 여기서 처음으로 영상 준비
        videoPlayer.Prepare();
    }


    private void OnPrepared(VideoPlayer source)
    {
        prepareRequested = false;

        // 첫 프레임부터 시작
        source.frame = 0;

        if (playWhenPrepared)
        {
            playWhenPrepared = false;

            ShowVideo();
            PlayFromBeginning();
        }
    }


    private void ShowVideo()
    {
        // PNG 썸네일 숨김
        if (thumbnailImage != null)
            thumbnailImage.enabled = false;

        // 실제 영상 표시
        if (videoImage != null)
            videoImage.enabled = true;
    }


    private void PlayFromBeginning()
    {
        if (videoPlayer == null ||
            videoPlayer.clip == null ||
            !videoPlayer.isPrepared)
        {
            return;
        }

        videoPlayer.frame = 0;
        videoPlayer.Play();
    }


    private void OnPlaybackCompleted(VideoPlayer source)
    {
        // 마지막 프레임에서 정지
        source.Pause();

        // 다시 마우스를 올리면
        // OnPointerEnter에서 처음부터 재생
    }


    private void EnsureComponents()
    {
        // ==================================
        // VideoPlayer
        // ==================================
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();

            if (videoPlayer == null)
                videoPlayer = gameObject.AddComponent<VideoPlayer>();

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            videoPlayer.renderMode =
                VideoRenderMode.RenderTexture;

            videoPlayer.audioOutputMode =
                VideoAudioOutputMode.None;

            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;

            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.loopPointReached += OnPlaybackCompleted;
        }


        // ==================================
        // ThumbnailImage
        // ==================================
        if (thumbnailImage == null)
        {
            Transform existing =
                transform.Find("ThumbnailImage");

            if (existing != null)
                thumbnailImage =
                    existing.GetComponent<Image>();


            if (thumbnailImage == null)
            {
                GameObject thumbnailObject =
                    new GameObject(
                        "ThumbnailImage",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image),
                        typeof(AspectRatioFitter)
                    );

                thumbnailObject.transform.SetParent(
                    transform,
                    false
                );

                thumbnailImage =
                    thumbnailObject.GetComponent<Image>();

                RectTransform rect =
                    thumbnailImage.rectTransform;

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;

                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            thumbnailImage.raycastTarget = true;
            thumbnailImage.preserveAspect = true;

            thumbnailAspectRatioFitter =
                thumbnailImage.GetComponent<AspectRatioFitter>();

            if (thumbnailAspectRatioFitter == null)
            {
                thumbnailAspectRatioFitter =
                    thumbnailImage.gameObject
                        .AddComponent<AspectRatioFitter>();
            }

            thumbnailAspectRatioFitter.aspectMode =
                AspectRatioFitter.AspectMode.FitInParent;
        }


        // ==================================
        // VideoImage
        // ==================================
        if (videoImage == null)
        {
            Transform existing =
                transform.Find("VideoImage");

            if (existing != null)
                videoImage =
                    existing.GetComponent<RawImage>();


            if (videoImage == null)
            {
                GameObject videoObject =
                    new GameObject(
                        "VideoImage",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(RawImage),
                        typeof(AspectRatioFitter)
                    );

                videoObject.transform.SetParent(
                    transform,
                    false
                );

                videoImage =
                    videoObject.GetComponent<RawImage>();

                RectTransform rect =
                    videoImage.rectTransform;

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;

                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            videoImage.raycastTarget = true;

            videoAspectRatioFitter =
                videoImage.GetComponent<AspectRatioFitter>();

            if (videoAspectRatioFitter == null)
            {
                videoAspectRatioFitter =
                    videoImage.gameObject
                        .AddComponent<AspectRatioFitter>();
            }

            videoAspectRatioFitter.aspectMode =
                AspectRatioFitter.AspectMode.FitInParent;
        }

        // 영상이 썸네일보다 위에 오도록
        if (thumbnailImage != null)
            thumbnailImage.transform.SetAsLastSibling();

        if (videoImage != null)
            videoImage.transform.SetAsLastSibling();
    }


    private void CreateRenderTexture(VideoClip clip)
    {
        ReleaseRenderTexture();

        int sourceWidth =
            Mathf.Max(1, (int)clip.width);

        int sourceHeight =
            Mathf.Max(1, (int)clip.height);


        float scale =
            Mathf.Min(
                1f,
                MaxRenderTextureSize /
                (float)Mathf.Max(
                    sourceWidth,
                    sourceHeight
                )
            );


        int width =
            Mathf.Max(
                16,
                Mathf.RoundToInt(
                    sourceWidth * scale
                )
            );

        int height =
            Mathf.Max(
                16,
                Mathf.RoundToInt(
                    sourceHeight * scale
                )
            );


        renderTexture =
            new RenderTexture(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32
            )
            {
                name =
                    $"HowToVideo_{clip.name}",

                filterMode =
                    FilterMode.Bilinear,

                wrapMode =
                    TextureWrapMode.Clamp
            };

        renderTexture.Create();
    }


    private void ResetVideo()
    {
        playWhenPrepared = false;
        prepareRequested = false;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.targetTexture = null;
            videoPlayer.clip = null;
        }

        ReleaseRenderTexture();

        if (videoImage != null)
        {
            videoImage.texture = null;
            videoImage.enabled = false;
        }
    }


    private void ReleaseRenderTexture()
    {
        if (videoPlayer != null)
            videoPlayer.targetTexture = null;

        if (videoImage != null)
            videoImage.texture = null;

        if (renderTexture == null)
            return;

        if (renderTexture.IsCreated())
            renderTexture.Release();

        Destroy(renderTexture);
        renderTexture = null;
    }


    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.loopPointReached -=
                OnPlaybackCompleted;
        }

        ReleaseRenderTexture();
    }
}