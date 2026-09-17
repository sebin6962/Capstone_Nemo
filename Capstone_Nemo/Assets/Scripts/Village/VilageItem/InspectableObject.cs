using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Put this on the same object as the single interaction trigger Collider2D.
[RequireComponent(typeof(Collider2D))]
public sealed class InspectableObject : MonoBehaviour
{
    internal static readonly HashSet<InspectableObject> All = new HashSet<InspectableObject>();
    [SerializeField] private string panelTitle = "방앗간 장부";
    [TextArea(3, 10)] [SerializeField] private string description =
        "계수나무가 시들기 시작한 뒤로 손님이 부쩍 줄었다.\n별에서 오던 단골들도 요즘은 영 보이지 않는다.";
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private Button hintButton;
    private readonly HashSet<Collider2D> contacts = new HashSet<Collider2D>();
    public string Title => panelTitle;
    public string Description => description;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { All.Clear(); }

    public bool IsInRange
    {
        get
        {
            contacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            return isActiveAndEnabled && contacts.Count > 0;
        }
    }

    internal float DistanceSquared
    {
        get
        {
            float distance = float.PositiveInfinity;
            foreach (Collider2D c in contacts)
                if (c != null)
                    distance = Mathf.Min(distance, ((Vector2)transform.position - (Vector2)c.bounds.center).sqrMagnitude);
            return distance;
        }
    }

    private void Awake()
    {
        SetHint(false);
        if (!GetComponent<Collider2D>().isTrigger)
            Debug.LogError("InspectableObject requires an Is Trigger Collider2D.", this);
        if (hintButton != null) hintButton.onClick.AddListener(Interact);
    }
    private void OnEnable() { All.Add(this); }
    private void OnDisable()
    {
        All.Remove(this);
        contacts.Clear();
        SetHint(false);
        if (InspectionUIManager.Instance != null)
            InspectionUIManager.Instance.CloseIfOwner(this);
    }
    private void OnDestroy()
    {
        if (hintButton != null) hintButton.onClick.RemoveListener(Interact);
    }
    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") ||
            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"));
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActiveAndEnabled && IsPlayer(other)) contacts.Add(other);
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        // Re-register after this component is re-enabled while the player is inside.
        if (isActiveAndEnabled && IsPlayer(other)) contacts.Add(other);
    }
    private void OnTriggerExit2D(Collider2D other) { contacts.Remove(other); }
    internal void SetHint(bool visible)
    {
        if (hintRoot != null && hintRoot.activeSelf != visible) hintRoot.SetActive(visible);
    }
    public void Interact()
    {
        if (InspectionUIManager.Instance != null)
            InspectionUIManager.Instance.TryOpen(this);
    }
}
