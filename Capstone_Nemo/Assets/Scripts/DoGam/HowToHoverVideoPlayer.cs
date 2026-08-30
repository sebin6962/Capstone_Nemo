using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 마우스가 영상 영역에 들어오면 처음부터 한 번 재생한다.
/// 마우스가 나가도 멈추지 않고 끝까지 재생한다.
/// 재생이 끝난 뒤 다시 마우스를 올리면 다시 한 번 재생한다.
/// </summary>
public class HowToHoverVideoPlayer : MonoBehaviour, IPointerEnterHandler
{
    private const int MaxRenderTextureSize = 512;

    private VideoPlayer videoPlayer;
    private RawImage videoImage;
    private AspectRatioFitter aspectRatioFitter;
    private RenderTexture renderTexture;
    private bool playWhenPrepared;

    public void Setup(VideoClip clip)
    {
        EnsureComponents();
        ReleaseRenderTexture();

        playWhenPrepared = false;
        videoImage.enabled = false;
        videoPlayer.Stop();
        videoPlayer.clip = clip;

        if (clip == null)
            return;

        CreateRenderTexture(clip);

        videoPlayer.targetTexture = renderTexture;
        videoImage.texture = renderTexture;
        videoImage.enabled = true;

        float width = Mathf.Max(1f, clip.width);
        float height = Mathf.Max(1f, clip.height);
        aspectRatioFitter.aspectRatio = width / height;

        videoPlayer.Prepare();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (videoPlayer == null || videoPlayer.clip == null || videoPlayer.isPlaying)
            return;

        if (!videoPlayer.isPrepared)
        {
            playWhenPrepared = true;
            return;
        }

        PlayFromBeginning();
    }

    private void EnsureComponents()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
                videoPlayer = gameObject.AddComponent<VideoPlayer>();

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.loopPointReached += OnPlaybackCompleted;
        }

        if (videoImage == null)
        {
            Transform existing = transform.Find("VideoImage");
            if (existing != null)
                videoImage = existing.GetComponent<RawImage>();

            if (videoImage == null)
            {
                GameObject imageObject = new GameObject(
                    "VideoImage",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage),
                    typeof(AspectRatioFitter));

                imageObject.transform.SetParent(transform, false);
                videoImage = imageObject.GetComponent<RawImage>();

                RectTransform rect = videoImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            videoImage.raycastTarget = true;
            aspectRatioFitter = videoImage.GetComponent<AspectRatioFitter>();
            if (aspectRatioFitter == null)
                aspectRatioFitter = videoImage.gameObject.AddComponent<AspectRatioFitter>();
            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        }
    }

    private void CreateRenderTexture(VideoClip clip)
    {
        int sourceWidth = Mathf.Max(1, (int)clip.width);
        int sourceHeight = Mathf.Max(1, (int)clip.height);
        float scale = Mathf.Min(1f, MaxRenderTextureSize / (float)Mathf.Max(sourceWidth, sourceHeight));
        int width = Mathf.Max(16, Mathf.RoundToInt(sourceWidth * scale));
        int height = Mathf.Max(16, Mathf.RoundToInt(sourceHeight * scale));

        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = $"HowToVideo_{clip.name}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        renderTexture.Create();
    }

    private void OnPrepared(VideoPlayer source)
    {
        source.frame = 0;

        if (playWhenPrepared)
        {
            playWhenPrepared = false;
            PlayFromBeginning();
        }
        else
        {
            // 첫 호버 전에는 첫 프레임을 썸네일처럼 보여준다.
            source.Pause();
        }
    }

    private void PlayFromBeginning()
    {
        videoPlayer.frame = 0;
        videoPlayer.Play();
    }

    private void OnPlaybackCompleted(VideoPlayer source)
    {
        // 마지막 프레임에서 멈춘다. 반복 재생하지 않는다.
        source.Pause();
    }

    private void ReleaseRenderTexture()
    {
        if (videoPlayer != null)
            videoPlayer.targetTexture = null;

        if (videoImage != null)
            videoImage.texture = null;

        if (renderTexture == null)
            return;

        renderTexture.Release();
        Destroy(renderTexture);
        renderTexture = null;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.loopPointReached -= OnPlaybackCompleted;
        }

        ReleaseRenderTexture();
    }
}

