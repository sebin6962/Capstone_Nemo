using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIPageCapture : MonoBehaviour
{
    [Header("캡처 기준 캔버스")]
    [SerializeField] private Canvas targetCanvas;

    public void CapturePage(RectTransform targetRect, Action<Sprite> onCaptured)
    {
        StartCoroutine(CapturePageRoutine(targetRect, onCaptured));
    }

    private IEnumerator CapturePageRoutine(RectTransform targetRect, Action<Sprite> onCaptured)
    {
        if (targetRect == null)
        {
            onCaptured?.Invoke(null);
            yield break;
        }

        // UI가 다 그려진 뒤 캡처해야 함
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        Camera cam = null;
        if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = targetCanvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(cam, corners[0]); // Bottom Left
        Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(cam, corners[2]); // Top Right

        int x = Mathf.RoundToInt(screenBL.x);
        int y = Mathf.RoundToInt(screenBL.y);
        int width = Mathf.RoundToInt(screenTR.x - screenBL.x);
        int height = Mathf.RoundToInt(screenTR.y - screenBL.y);

        // 화면 밖으로 나가는 값 보정
        x = Mathf.Clamp(x, 0, Screen.width - 1);
        y = Mathf.Clamp(y, 0, Screen.height - 1);
        width = Mathf.Clamp(width, 1, Screen.width - x);
        height = Mathf.Clamp(height, 1, Screen.height - y);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(x, y, width, height), 0, 0);
        tex.Apply();

        Sprite capturedSprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        onCaptured?.Invoke(capturedSprite);
    }
}
