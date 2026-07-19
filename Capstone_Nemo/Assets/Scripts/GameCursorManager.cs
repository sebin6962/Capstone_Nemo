using System.Collections.Generic;
using UnityEngine;

public class GameCursorManager : MonoBehaviour
{
    public static GameCursorManager Instance { get; private set; }

    [Header("기본 커서")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Vector2 defaultHotspot = Vector2.zero;

    [Header("상호작용 손 커서")]
    [SerializeField] private Texture2D handCursor;
    [SerializeField] private Vector2 handHotspot = Vector2.zero;

    [Header("커서 모드")]
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    // 현재 손 커서를 요청하고 있는 오브젝트 목록
    private readonly HashSet<int> handCursorRequesters =
        new HashSet<int>();

    // 같은 커서를 반복 적용하지 않기 위한 상태
    private bool? currentHandCursorState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyCursor(false, true);
    }

    /// <summary>
    /// 상호작용 대상에 마우스가 진입했을 때 호출
    /// </summary>
    public void EnterHandCursor(Object requester)
    {
        if (requester == null)
            return;

        handCursorRequesters.Add(requester.GetInstanceID());
        RefreshCursor();
    }

    /// <summary>
    /// 상호작용 대상에서 마우스가 빠졌을 때 호출
    /// </summary>
    public void ExitHandCursor(Object requester)
    {
        if (requester == null)
            return;

        handCursorRequesters.Remove(requester.GetInstanceID());
        RefreshCursor();
    }

    private void RefreshCursor()
    {
        bool useHandCursor = handCursorRequesters.Count > 0;
        ApplyCursor(useHandCursor);
    }

    private void ApplyCursor(bool useHandCursor, bool force = false)
    {
        if (!force &&
            currentHandCursorState.HasValue &&
            currentHandCursorState.Value == useHandCursor)
        {
            return;
        }

        currentHandCursorState = useHandCursor;

        if (useHandCursor)
        {
            Cursor.SetCursor(
                handCursor,
                handHotspot,
                cursorMode
            );
        }
        else
        {
            Cursor.SetCursor(
                defaultCursor,
                defaultHotspot,
                cursorMode
            );
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            return;

        // 게임 창을 다시 선택하면 현재 상태의 커서를 강제 재적용
        ApplyCursor(handCursorRequesters.Count > 0, true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
