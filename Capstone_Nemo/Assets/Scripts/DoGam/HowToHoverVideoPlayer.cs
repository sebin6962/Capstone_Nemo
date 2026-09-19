using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Hover video player for the How-To page.
/// Videos are loaded from StreamingAssets/Videos/Guide by URL so WebGL can play them.
/// A PNG thumbnail is shown immediately; the video is prepared only on first hover.
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

    private string currentVideoFileName;
    private Sprite currentThumbnail;

    private bool prepareRequested;
    private bool playWhenPrepared;

    /// <summary>
    /// videoFileName example: "Ctrl_guide_01.mp4"
    /// The file must exist under Assets/StreamingAssets/Videos/Guide/.
    /// </summary>
    public void Setup(string videoFileName, Sprite thumbnail)
    {
        EnsureComponents();
        ResetVideo();

        currentVideoFileName = NormalizeVideoFileName(videoFileName);
        currentThumbnail = thumbnail;

        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = thumbnail;
            thumbnailImage.enabled = thumbnail != null;
            thumbnailImage.preserveAspect = true;
        }

        if (videoImage != null)
        {
            videoImage.enabled = false;
            videoImage.texture = null;
        }

        if (thumbnail != null && thumbnailAspectRatioFitter != null)
        {
            Rect spriteRect = thumbnail.rect;
            float width = Mathf.Max(1f, spriteRect.width);
            float height = Mathf.Max(1f, spriteRect.height);
            thumbnailAspectRatioFitter.aspectRatio = width / height;
        }

        if (videoPlayer != null)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = string.Empty;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(currentVideoFileName))
            return;

        if (videoPlayer != null && videoPlayer.isPlaying)
            return;

        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            ShowVideo();
            PlayFromBeginning();
            return;
        }

        if (prepareRequested)
        {
            playWhenPrepared = true;
            return;
        }

        PrepareVideo();
    }

    private void PrepareVideo()
    {
        if (string.IsNullOrWhiteSpace(currentVideoFileName) || videoPlayer == null)
            return;

        prepareRequested = true;
        playWhenPrepared = true;

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = BuildVideoUrl(currentVideoFileName);
        videoPlayer.targetTexture = null;

        // For URL playback, width/height are reliable after Prepare completes.
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer source)
    {
        prepareRequested = false;

        int sourceWidth = Mathf.Max(1, (int)source.width);
        int sourceHeight = Mathf.Max(1, (int)source.height);

        // Some platforms may not report dimensions. Fall back to thumbnail ratio.
        if (sourceWidth <= 1 || sourceHeight <= 1)
        {
            GetThumbnailSize(out sourceWidth, out sourceHeight);
        }

        CreateRenderTexture(sourceWidth, sourceHeight);
        source.targetTexture = renderTexture;

        if (videoImage != null)
            videoImage.texture = renderTexture;

        if (videoAspectRatioFitter != null)
            videoAspectRatioFitter.aspectRatio = sourceWidth / (float)Mathf.Max(1, sourceHeight);

        source.frame = 0;

        if (playWhenPrepared)
        {
            playWhenPrepared = false;
            ShowVideo();
            PlayFromBeginning();
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        prepareRequested = false;
        playWhenPrepared = false;

        Debug.LogWarning(
            $"[HowTo] Video load failed: {currentVideoFileName}\n" +
            $"URL: {source.url}\n" +
            $"Reason: {message}"
        );

        if (thumbnailImage != null)
            thumbnailImage.enabled = currentThumbnail != null;

        if (videoImage != null)
            videoImage.enabled = false;
    }

    private void ShowVideo()
    {
        if (thumbnailImage != null)
            thumbnailImage.enabled = false;

        if (videoImage != null)
            videoImage.enabled = true;
    }

    private void PlayFromBeginning()
    {
        if (videoPlayer == null ||
            videoPlayer.source != VideoSource.Url ||
            string.IsNullOrEmpty(videoPlayer.url) ||
            !videoPlayer.isPrepared)
        {
            return;
        }

        videoPlayer.frame = 0;
        videoPlayer.Play();
    }

    private void OnPlaybackCompleted(VideoPlayer source)
    {
        // Keep the final frame visible.
        source.Pause();
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
            videoPlayer.source = VideoSource.Url;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;

            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.loopPointReached += OnPlaybackCompleted;
            videoPlayer.errorReceived += OnVideoError;
        }

        if (thumbnailImage == null)
        {
            Transform existing = transform.Find("ThumbnailImage");

            if (existing != null)
                thumbnailImage = existing.GetComponent<Image>();

            if (thumbnailImage == null)
            {
                GameObject thumbnailObject = new GameObject(
                    "ThumbnailImage",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(AspectRatioFitter)
                );

                thumbnailObject.transform.SetParent(transform, false);
                thumbnailImage = thumbnailObject.GetComponent<Image>();

                RectTransform rect = thumbnailImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            thumbnailImage.raycastTarget = true;
            thumbnailImage.preserveAspect = true;

            thumbnailAspectRatioFitter = thumbnailImage.GetComponent<AspectRatioFitter>();
            if (thumbnailAspectRatioFitter == null)
                thumbnailAspectRatioFitter = thumbnailImage.gameObject.AddComponent<AspectRatioFitter>();

            thumbnailAspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        }

        if (videoImage == null)
        {
            Transform existing = transform.Find("VideoImage");

            if (existing != null)
                videoImage = existing.GetComponent<RawImage>();

            if (videoImage == null)
            {
                GameObject videoObject = new GameObject(
                    "VideoImage",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage),
                    typeof(AspectRatioFitter)
                );

                videoObject.transform.SetParent(transform, false);
                videoImage = videoObject.GetComponent<RawImage>();

                RectTransform rect = videoImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            videoImage.raycastTarget = true;

            videoAspectRatioFitter = videoImage.GetComponent<AspectRatioFitter>();
            if (videoAspectRatioFitter == null)
                videoAspectRatioFitter = videoImage.gameObject.AddComponent<AspectRatioFitter>();

            videoAspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        }

        if (thumbnailImage != null)
            thumbnailImage.transform.SetAsLastSibling();

        if (videoImage != null)
            videoImage.transform.SetAsLastSibling();
    }

    private void CreateRenderTexture(int sourceWidth, int sourceHeight)
    {
        ReleaseRenderTexture();

        sourceWidth = Mathf.Max(1, sourceWidth);
        sourceHeight = Mathf.Max(1, sourceHeight);

        float scale = Mathf.Min(
            1f,
            MaxRenderTextureSize / (float)Mathf.Max(sourceWidth, sourceHeight)
        );

        int width = Mathf.Max(16, Mathf.RoundToInt(sourceWidth * scale));
        int height = Mathf.Max(16, Mathf.RoundToInt(sourceHeight * scale));

        renderTexture = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32
        )
        {
            name = $"HowToVideo_{System.IO.Path.GetFileNameWithoutExtension(currentVideoFileName)}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        renderTexture.Create();
    }

    private void GetThumbnailSize(out int width, out int height)
    {
        width = 512;
        height = 288;

        if (currentThumbnail == null)
            return;

        Rect rect = currentThumbnail.rect;
        width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
        height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
    }

    private string BuildVideoUrl(string fileName)
    {
        string basePath = Application.streamingAssetsPath.TrimEnd('/', '\\');
        string relativePath = "Videos/Guide/" + fileName;
        return basePath + "/" + relativePath.Replace("\\", "/");
    }

    private static string NormalizeVideoFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        fileName = System.IO.Path.GetFileName(fileName.Trim());

        if (string.IsNullOrEmpty(System.IO.Path.GetExtension(fileName)))
            fileName += ".mp4";

        return fileName;
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
            videoPlayer.url = string.Empty;
            videoPlayer.source = VideoSource.Url;
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
            videoPlayer.loopPointReached -= OnPlaybackCompleted;
            videoPlayer.errorReceived -= OnVideoError;
        }

        ReleaseRenderTexture();
    }
}
