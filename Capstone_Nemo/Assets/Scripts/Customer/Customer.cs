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
    [SerializeField] private float orderTimeLimit;
    protected float remainingTime;
    private string orderedDagwa;
    protected bool isTimerRunning = false;
    protected bool isServed = false;
    protected CustomerState state = CustomerState.Walking;

    [SerializeField] private bool isTutorialCustomer = false;
    [SerializeField] private string tutorialDagwaId;

    [SerializeField] private Animator animator;
    private Vector2 lastMoveDir = Vector2.down;

    public int prefabIndex;
    public float OrderTimeLimit => orderTimeLimit;

    public void SetTutorialCustomer(string fixedDagwaId)
    {
        isTutorialCustomer = true;
        tutorialDagwaId = fixedDagwaId;
    }

    public void SetPrefabIndex(int index)
    {
        prefabIndex = index;
    }

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

        if (state != CustomerState.Walking)
        {
            UpdateAnimation(Vector3.zero);
            return;
        }

        if (wayPoints == null || currentIndex >= wayPoints.Length)
        {
            UpdateAnimation(Vector3.zero);
            return;
        }


        Vector3 targetPos = wayPoints[currentIndex].position;
        Vector3 toTarget = targetPos - transform.position;

        if (toTarget.sqrMagnitude < 0.01f)
        {
            transform.position = targetPos;
            currentIndex++;
            if (currentIndex >= wayPoints.Length)
            {
                Sit();
                Debug.Log("손님 착석");
            }
            UpdateAnimation(Vector3.zero);
            return;
        }

        Vector3 moveDir = toTarget.normalized;
        transform.position += moveDir * speed * Time.deltaTime;

        UpdateAnimation(moveDir);
    }

    private void UpdateAnimation(Vector3 moveDir)
    {
        if (animator == null)
            return;

        bool movingState = (state == CustomerState.Walking || state == CustomerState.Leaving);

        bool hasMove = movingState && moveDir.sqrMagnitude > 0.01f;

        if (hasMove)
        {
            Vector2 dir = new Vector2(moveDir.x, moveDir.y);

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                dir.y = 0f;
                dir.x = Mathf.Sign(dir.x);  
            }
            else
            {
                dir.x = 0f;
                dir.y = Mathf.Sign(dir.y);  
            }

            lastMoveDir = dir; 
        }

        animator.SetBool("IsMoving", hasMove);

        animator.SetFloat("MoveX", lastMoveDir.x);
        animator.SetFloat("MoveY", lastMoveDir.y);
    }

    void Sit()
    {
        
        state = CustomerState.Sit;

        AssignPlate();
        StartOrdering();
    }

    public void AssignPlate()
    {
        var plates = GameObject.FindGameObjectsWithTag("Plate");
        Debug.Log($"[AssignPlate] 발견된 Plate 개수: {plates.Length}");

        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (var plate in plates)
        {
            float dist = Vector3.Distance(transform.position, plate.transform.position);
            /*Debug.Log($"→ Plate 후보: {plate.name}, 거리: {dist}");*/

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
                Debug.LogWarning($"[AssignPlate] {closest.name}에 Plate 컴포넌트 없음");
            }
            else
            {
                assignedPlate = plateComp;
                plateComp.SetTargetCustomer(this);
                Debug.Log($"[AssignPlate] 접시에 손님 연동 완료: {name} → {closest.name}");
            }
        }
        else
        {
            Debug.LogWarning("[AssignPlate] 가까운 접시를 찾지 못함");
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

        //튜토리얼구분(다과 종류)
        if(isTutorialCustomer && !string.IsNullOrEmpty(tutorialDagwaId))
        {
            orderedDagwa = tutorialDagwaId;
        }

        else
        {
            orderedDagwa = OrderManager.Instance.GetRandomDagwaList();
        }

        Debug.Log("손님이 주문한 다과:" + orderedDagwa);

        orderUI.ShowOrder(orderedDagwa);

        //튜토리얼구분(시간 제한)
        if (isTutorialCustomer)
        {
            isTimerRunning = false;
            orderUI.ShowTimerUI(false);
        }

        else
        {
            remainingTime = orderTimeLimit;
            isTimerRunning = true;
            orderUI.ShowTimerUI(true);
        }

       /* remainingTime = orderTimeLimit;
        isTimerRunning = true;*/
    }


    public virtual void Serve(string givenDagwa)
    {
        // 다과 제공 효과음 재생!
        SFXManager.Instance.PlayPlateSoundSFX();

        if (state != CustomerState.Ordering || isServed) return;

        isTimerRunning = false;
        isServed = true;

        string expected = orderedDagwa.Trim().ToLower();
        string given = givenDagwa.Trim().ToLower();

        Debug.Log($"[비교] 주문: {expected} / 전달: {given}");

        if (expected == given)
        {
            state = CustomerState.Served;
            orderUI.ShowResult(true);
            orderUI.ShowTimerUI(false);
            Debug.Log($"정답 처리됨: {givenDagwa}");

            // 기본값(대비용)
            int expAmount = 20;
            int starAmount = 10;

            // 다과별 보상 테이블에서 가져오기
            var reward = DagwaRewardManager.Instance?.GetReward(expected);
            if (reward != null)
            {
                expAmount = reward.exp;
                starAmount = reward.starlight;
            }

            // 경험치 지급
            PlayerLevelManager.Instance?.AddExp(expAmount);

            // 별빛 지급
            //StarDataManager.Instance?.AddStarlight(10);
            StarDataManager.Instance?.AddStarlightFromNormal(starAmount);
            SFXManager.Instance.PlayTotalMoneySFX();

            Invoke(nameof(RemoveDagwaOnPlate), 2f);

            // --- 정답 효과음 재생 추가 ---
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
            Debug.Log($"오답 처리됨: {givenDagwa}");

            // --- 오답 효과음 재생 추가 ---
            SFXManager.Instance.PlayWrongSFX();
        }

        Invoke(nameof(Leave), 4f);
    }
    protected virtual void HandleTimeOver()
    {
        if (isTutorialCustomer)
        {
            return;
        }

        isTimerRunning = false;
        isServed = true;
        state = CustomerState.Displeased;

        orderUI.ShowResult(false);
        orderUI.ShowTimerUI(false);

        // --- 오답 효과음 재생 추가 ---
        SFXManager.Instance.PlayWrongSFX();

        Invoke(nameof(Leave), 4f);
    }
    protected void Leave()
    {
        state = CustomerState.Leaving;
        StartCoroutine(MoveDownAndDestroy());
    }

    public void ForceLeaveFromSave()
    {
        state = CustomerState.Leaving;
        StartCoroutine(MoveDownAndDestroy());
    }

    protected void RemoveDagwaOnPlate()
    {
        if (assignedPlate == null)
        {
            Debug.LogWarning("[Customer] assignedPlate가 null임");
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
            Debug.Log("접시 위 다과 제거됨");
        }
        else
        {
            Debug.LogWarning("접시 근처에 다과가 없음");
        }
    }


    IEnumerator MoveDownAndDestroy()
    {
        Vector3 startPos = transform.position;
        Vector3 target = startPos + Vector3.down * 3f;
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        CanvasGroup[] canvasGroups = GetComponentsInChildren<CanvasGroup>();

        //내려가는데걸리는시간
        float duration = 2.5f;  
        //언제부터페이드?
        float fadeDelay = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, target, Mathf.SmoothStep(0f, 1f, t));

            UpdateAnimation(Vector3.down);

            if (t > fadeDelay)
            {
                float fadeT = Mathf.InverseLerp(fadeDelay, 1f, t);
                float alpha = Mathf.Lerp(1f, 0f, fadeT);

                //손님 스프라이트 페이드
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    Color c = r.color;
                    c.a = alpha;
                    r.color = c;
                }

                //말풍선(CanvasGroup) 페이드
                foreach (var cg in canvasGroups)
                {
                    if (cg == null) continue;
                    cg.alpha = alpha;
                }
            }
            
            yield return null;
        }

        // 완전히 사라진 후 좌석 비움
        if (seatManager != null)
        {
            seatManager.VacateSeat(seatIndex);
            Debug.Log($"좌석 {seatIndex} 비움");
        }

        var tutorMgr = StoreTutorialManager.Instance;

        if (tutorMgr != null)
        {
            // 튜토리얼 진행 트리거 6
            if (isTutorialCustomer && tutorMgr.IsCurrentStep(StoreTutorialStep.Serve))
            {
                Debug.Log("[Tutorial] Serve 단계 튜토리얼 손님 퇴장 → NextOrder 단계로 이동");
                tutorMgr.GoToNextStep();
            }
        }

        Destroy(gameObject);
    }

    public CustomerSave ToSave()
    {
        return new CustomerSave
        {
            seatIndex = this.seatIndex,
            state = this.state,

            isTutorialCustomer = this.isTutorialCustomer,
            tutorialDagwaId = this.tutorialDagwaId,

            orderedDagwa = this.orderedDagwa,
            orderTimeLimit = this.orderTimeLimit,
            remainingTime = this.remainingTime,

            currentWaypointIndex = this.currentIndex,
            position = this.transform.position,

            prefabIndex = this.prefabIndex,

            hasScenePosition = true
        };
    }

    public void ApplySave(CustomerSave data)
    {
        this.seatIndex = data.seatIndex;
        this.state = data.state;

        this.isTutorialCustomer = data.isTutorialCustomer;
        this.tutorialDagwaId = data.tutorialDagwaId;

        this.orderedDagwa = data.orderedDagwa;
        this.orderTimeLimit = data.orderTimeLimit;
        this.remainingTime = data.remainingTime;

        //위치, 경로 복원
        if (data.hasScenePosition && (data.state == CustomerState.Walking || data.state == CustomerState.Sit))
        {
            transform.position = data.position;
            currentIndex = data.currentWaypointIndex;
        }

        this.prefabIndex = data.prefabIndex;

        //타이머 / UI 복원
        if (orderUI != null)
        {
            //주문/대기 상태면 주문 UI + 타이머 UI 복원
            if (state == CustomerState.Ordering || state == CustomerState.Waiting)
            {
                orderUI.ShowOrder(orderedDagwa);

                if (!isTutorialCustomer)
                {
                    float ratio = orderTimeLimit > 0f ? (remainingTime / orderTimeLimit) : 0f;
                    orderUI.ShowTimerUI(true);
                    orderUI.UpdateTimer(ratio);
                    isTimerRunning = true;
                }
                else
                {
                    //튜토리얼
                    orderUI.ShowTimerUI(false);
                    isTimerRunning = false;
                }
            }
            else
            {
                orderUI.ShowTimerUI(false);
                isTimerRunning = false;
            }
        }
    }
}
