using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CustomerState
{
    Walking,
    Sit,
    Ordering,
    Waiting,
    Served,
    Displeased,
    Leaving
}

public class Customer : MonoBehaviour

{
    private int currentIndex = 0;
    private SeatManager seatManager;
    private int seatIndex;
    private PlateCheck assignedPlate;

    private Transform[] wayPoints;
    public float speed = 3f;

    [SerializeField] protected OrderUI orderUI;
    [SerializeField] private float orderTimeLimit = 45f;
    protected float remainingTime;
    private string orderedDagwa;
    protected bool isTimerRunning = false;
    protected bool isServed = false;
    protected CustomerState state = CustomerState.Walking;

    public void Initialize(Transform[] path)
    {
        wayPoints = path;
        currentIndex = 0;
    }

    public void SetSeatInfo(int index, SeatManager manager)
    {
        seatIndex = index;
        seatManager = manager;
    }

    protected virtual void Update()
    {
        if (isTimerRunning)
        {
            remainingTime -= Time.deltaTime;
            float ratio = remainingTime / orderTimeLimit;
            orderUI.UpdateTimer(ratio);

            if (remainingTime <= 0)
            {
                HandleTimeOver();
            }
        }

        if (wayPoints == null || currentIndex >= wayPoints.Length)
            return;

        Vector3 targetPos = wayPoints[currentIndex].position;
        Vector3 moveDir = (targetPos - transform.position).normalized;
        transform.position += moveDir * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            currentIndex++;
            if (currentIndex >= wayPoints.Length)
            {
                Sit();
                Debug.Log("�մ� ����");
            }
        }
    }

    void Sit()
    {
        
        state = CustomerState.Sit;
        AssignPlate();
        StartOrdering();
    }

    private void AssignPlate()
    {
        var plates = GameObject.FindGameObjectsWithTag("Plate");
        Debug.Log($"[AssignPlate] �߰ߵ� Plate ����: {plates.Length}");

        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (var plate in plates)
        {
            float dist = Vector3.Distance(transform.position, plate.transform.position);
            /*Debug.Log($"�� Plate �ĺ�: {plate.name}, �Ÿ�: {dist}");*/

            if (dist < minDist)
            {
                minDist = dist;
                closest = plate;
            }
        }

        if (closest != null)
        {
            var plateComp = closest.GetComponent<PlateCheck>();
            if (plateComp == null)
            {
                Debug.LogWarning($"[AssignPlate] {closest.name}�� Plate ������Ʈ ����");
            }
            else
            {
                assignedPlate = plateComp;
                plateComp.SetTargetCustomer(this);
                Debug.Log($"[AssignPlate] ���ÿ� �մ� ���� �Ϸ�: {name} �� {closest.name}");
            }
        }
        else
        {
            Debug.LogWarning("[AssignPlate] ����� ���ø� ã�� ����");
        }
    }

    private GameObject FindClosestPlate()
    {
        var plates = GameObject.FindGameObjectsWithTag("Plate");
        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (var plate in plates)
        {
            float dist = Vector3.Distance(transform.position, plate.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = plate;
            }
        }
        return closest;
    }

    protected virtual void StartOrdering()
    {
        state = CustomerState.Ordering;

        orderedDagwa = OrderManager.Instance.GetRandomDagwaList();
        Debug.Log("�մ��� �ֹ��� �ٰ�:" + orderedDagwa);

        orderUI.ShowOrder(orderedDagwa);

        remainingTime = orderTimeLimit;
        isTimerRunning = true;
    }
    public virtual void Serve(string givenDagwa)
    {
        // (1) �ٰ� ���� ȿ���� ���!
        SFXManager.Instance.PlayPlateSoundSFX();

        if (state != CustomerState.Ordering || isServed) return;

        isTimerRunning = false;
        isServed = true;

        string expected = orderedDagwa.Trim().ToLower();
        string given = givenDagwa.Trim().ToLower();

        Debug.Log($"[��] �ֹ�: {expected} / ����: {given}");

        if (expected == given)
        {
            state = CustomerState.Served;
            orderUI.ShowResult(true);
            orderUI.ShowTimerUI(false);
            Debug.Log($"���� ó����: {givenDagwa}");
            Invoke(nameof(RemoveDagwaOnPlate), 2f);

            // --- ���� ȿ���� ��� �߰� ---
            SFXManager.Instance.PlayCorrectSFX();
        }
        else
        {
            state = CustomerState.Displeased;
            isServed = false;
            isTimerRunning = true;
            remainingTime -= 3f;

            orderUI.ShowResult(false);
            orderUI.ShowTimerUI(false);
            Debug.Log($"���� ó����: {givenDagwa}");

            // --- ���� ȿ���� ��� �߰� ---
            SFXManager.Instance.PlayWrongSFX();
        }

        Invoke(nameof(Leave), 4f);
    }
    protected virtual void HandleTimeOver()
    {
        isTimerRunning = false;
        isServed = true;
        state = CustomerState.Displeased;

        orderUI.ShowResult(false);
        orderUI.ShowTimerUI(false);

        // --- ���� ȿ���� ��� �߰� ---
        SFXManager.Instance.PlayWrongSFX();

        Invoke(nameof(Leave), 4f);
    }
    protected void Leave()
    {
        state = CustomerState.Leaving;
        StartCoroutine(MoveDownAndDestroy());
    }

    protected void RemoveDagwaOnPlate()
    {
        if (assignedPlate == null)
        {
            Debug.LogWarning("[Customer] assignedPlate�� null��");
            return;
        }

        ResultItemUI[] allDagwa = FindObjectsOfType<ResultItemUI>();
        ResultItemUI closest = null;
        float minDist = float.MaxValue;

        foreach (var dagwa in allDagwa)
        {
            float dist = Vector3.Distance(assignedPlate.transform.position, dagwa.transform.position);
            if (dist < 0.5f && dist < minDist)
            {
                closest = dagwa;
                minDist = dist;
            }
        }

        if (closest != null)
        {
            Destroy(closest.gameObject);
            Debug.Log("���� �� �ٰ� ���ŵ�");
        }
        else
        {
            Debug.LogWarning("���� ��ó�� �ٰ��� ����");
        }
    }

    IEnumerator MoveDownAndDestroy()
    {
        Vector3 target = transform.position + Vector3.down * 4f;

        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 2f);
            yield return null;
        }

        yield return new WaitForSeconds(2f);
        Destroy(gameObject);

        if (seatManager != null)
        {
            seatManager.VacateSeat(seatIndex);
            Debug.Log($"�¼� {seatIndex} ���");
        }

        Destroy(gameObject);
    }
}
