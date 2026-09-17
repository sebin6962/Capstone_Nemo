using UnityEngine;

/// <summary>
/// 마을 기록 오브젝트에 붙여서 사용할 수 있는 간단한 발견 처리 컴포넌트.
/// 기존 상호작용 스크립트에서 패널을 여는 순간 Discover()를 호출하면 된다.
/// </summary>
public class WorldRecordDiscoverer : MonoBehaviour
{
    [SerializeField] private string recordId;

    public string RecordId => recordId;

    public void Discover()
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            Debug.LogWarning("[WorldRecordDiscoverer] recordId가 비어 있습니다.", this);
            return;
        }

        DoGamRecordProgress.MarkWorldRecordDiscovered(recordId);
    }
}
