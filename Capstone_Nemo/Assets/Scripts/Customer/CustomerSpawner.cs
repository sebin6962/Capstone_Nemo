using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject[] customerPrefab;
    public Transform spawnPoint; 
    public SeatManager seatManager;
    public List<Route> routesPerSeat;

    public float spawnInterval;
    private float timer;
    public float firstSpawnDelay;

    [SerializeField] private float questCustomerChance = 0.2f;
    [SerializeField] private GameObject questCustomerPrefab;

    [SerializeField] private bool tutorialMode = false;

    [SerializeField] private bool allowNewCustomers = true;

    [SerializeField] private GameObject tutorialSeatedCustomerPrefab;

    void Awake()
    {
        Debug.Log($"[CustomerSpawner] Awake name={name}, tutorialMode={tutorialMode}");
    }

    void Start()
    {
        var saveMgr = CustomerSaveManager.Instance;
        
        if(!tutorialMode && saveMgr != null)
        {
            saveMgr.ConfigureFromSpawner(this);

            if (saveMgr.save.Count > 0)
            {
                saveMgr.RestoreToScene(this);
                return;
            }
        }

        if (!tutorialMode)
        {
            StartCoroutine(FirstCustomerDelay(firstSpawnDelay));
        }
    }

    private IEnumerator FirstCustomerDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (allowNewCustomers)
        {
            TrySpawnCustomer();
        }

        timer = 0f;
    }

    void Update()
    {
        if (tutorialMode || !allowNewCustomers)
            return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            TrySpawnCustomer();
        }
    }

    void TrySpawnCustomer()
    {
        int seatIndex = seatManager.GetAvailableSeatIndex();
        if (seatIndex >= 0 && seatIndex < routesPerSeat.Count)
        {
            int prefabIndex = Random.Range(0, customerPrefab.Length);

            GameObject prefab = customerPrefab[prefabIndex];
            GameObject customer = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            seatManager.OccupySeat(seatIndex);

            Transform[] path = routesPerSeat[seatIndex].waypoints;
            Customer customerScript = customer.GetComponent<Customer>();
            customerScript.Initialize(path);
            customerScript.SetSeatInfo(seatIndex, seatManager);

            customerScript.SetPrefabIndex(prefabIndex);
        }
    }

    public void SetAllowNewCustomers(bool allow)
    {
        allowNewCustomers = allow;
    }

    public void SpawnTutorialCustomer(int prefabIndex, string tutorialDagwaId, float delay = 0f)
    {
        tutorialMode = true;
        StartCoroutine(SpawnCustomerDelay(prefabIndex, tutorialDagwaId, delay));
    }

    public void SpawnSeatedTutorialCustomer(string tutorialDagwaId, float delay = 0f)
    {
        tutorialMode = true;
        StartCoroutine(SpawnSeatedTutorialCustomerDelay(tutorialDagwaId, delay));
    }

    private IEnumerator SpawnSeatedTutorialCustomerDelay(string tutorialDagwaId, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (seatManager == null)
        {
            Debug.LogError("[Tutorial] seatManager가 null입니다.");
            yield break;
        }

        int seatIndex = seatManager.GetAvailableSeatIndex();

        if (seatIndex < 0 || seatIndex >= routesPerSeat.Count)
        {
            Debug.LogError($"[Tutorial] 잘못된 seatIndex={seatIndex}");
            yield break;
        }

        if (tutorialSeatedCustomerPrefab == null)
        {
            Debug.LogError("[Tutorial] tutorialSeatedCustomerPrefab이 연결되지 않았습니다.");
            yield break;
        }

        var route = routesPerSeat[seatIndex];

        if (route == null || route.waypoints == null || route.waypoints.Length == 0)
        {
            Debug.LogError($"[Tutorial] routesPerSeat[{seatIndex}]의 waypoints가 비어 있습니다.");
            yield break;
        }

        seatManager.OccupySeat(seatIndex);

        Vector3 seatPos = route.waypoints[route.waypoints.Length - 1].position;

        GameObject customer = Instantiate(
            tutorialSeatedCustomerPrefab,
            seatPos,
            Quaternion.identity
        );

        Customer customerScript = customer.GetComponent<Customer>();

        if (customerScript == null)
        {
            Debug.LogError("[Tutorial] 앉은 튜토리얼 손님 프리팹에 Customer 컴포넌트가 없습니다.");
            yield break;
        }

        customerScript.SetSeatInfo(seatIndex, seatManager);
        customerScript.SetTutorialCustomer(tutorialDagwaId);

        // 필요하면 Customer 쪽에 아래 함수 추가
        customerScript.ForceSeatedTutorialState();
    }

    private IEnumerator SpawnCustomerDelay(int prefabIndex, string tutorialDagwaId, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (seatManager == null)
        {
            Debug.LogError("[Tutorial] seatManager가 null 입니다.");
            yield break;
        }

        int seatIndex = seatManager.GetAvailableSeatIndex();

        Debug.Log($"[Tutorial] GetAvailableSeatIndex = {seatIndex}, routesPerSeat.Count = {routesPerSeat.Count}");
        if (seatIndex < 0 || seatIndex >= routesPerSeat.Count)
        {
            Debug.LogError($"[Tutorial] 잘못된 seatIndex={seatIndex}. routesPerSeat.Count={routesPerSeat.Count}");
            yield break;
        }

        if (prefabIndex < 0 || prefabIndex >= customerPrefab.Length)
        {
            Debug.LogError($"[Tutorial] 잘못된 prefabIndex={prefabIndex}. customerPrefab.Length={customerPrefab.Length}");
            yield break;
        }

        seatManager.OccupySeat(seatIndex);

        var route = routesPerSeat[seatIndex];
        if (route == null || route.waypoints == null || route.waypoints.Length == 0)
        {
            Debug.LogError($"[Tutorial] routesPerSeat[{seatIndex}] 혹은 waypoints가 비어 있습니다.");
            yield break;
        }

        GameObject customer = Instantiate(customerPrefab[prefabIndex], spawnPoint.position, Quaternion.identity);

        Transform[] path = routesPerSeat[seatIndex].waypoints;
        Customer customerScript = customer.GetComponent<Customer>();

        customerScript.Initialize(path);
        customerScript.SetSeatInfo(seatIndex, seatManager);
        customerScript.SetTutorialCustomer(tutorialDagwaId);
        customerScript.SetPrefabIndex(prefabIndex);
    }

    public void EndTutorial()
    {
        tutorialMode = false;
        timer = 0f;
    }

    public Customer SpawnFromSave(CustomerSave data)
    {
        int seatIndex = data.seatIndex;

        if (seatIndex < 0 || seatIndex >= routesPerSeat.Count)
        {
            Debug.LogWarning($"[CustomerSpawner] 잘못된 seatIndex: {seatIndex}");
            return null;
        }

        int prefabIndex = Mathf.Clamp(data.prefabIndex, 0, customerPrefab.Length - 1);
        GameObject prefab = customerPrefab[prefabIndex];
        GameObject obj = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        seatManager.OccupySeat(seatIndex);

        Transform[] path = routesPerSeat[seatIndex].waypoints;
        Customer customer = obj.GetComponent<Customer>();
        customer.Initialize(path);
        customer.SetSeatInfo(seatIndex, seatManager);
        customer.SetPrefabIndex(prefabIndex);

        Vector3 seatPos = path[path.Length - 1].position;

        if (data.state == CustomerState.Walking)
        {
           
        }
        else
        {
            customer.transform.position = seatPos;
        }

        customer.ApplySave(data);

        if (data.state == CustomerState.Ordering || data.state == CustomerState.Waiting)
        {
            customer.AssignPlate(); 
        }
        else if (data.state == CustomerState.Served ||
                 data.state == CustomerState.Displeased ||
                 data.state == CustomerState.Leaving)
        {
            customer.ForceLeaveFromSave();
        }


        return customer;
    }
}
