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

    void Awake()
    {
        Debug.Log($"[CustomerSpawner] Awake name={name}, tutorialMode={tutorialMode}");
    }

    void Start()
    {
        var saveMgr = CustomerSaveManager.Instance;
        if (saveMgr != null)
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

        TrySpawnCustomer();  
        timer = 0f;          
    }

    void Update()
    {
        if (tutorialMode)
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

    public void SpawnTutorialCustomer(string tutorialDagwaId, float delay = 0f)
    {
        tutorialMode = true;  
        StartCoroutine(SpawnCustomerDelay(tutorialDagwaId, delay));
    }

    private IEnumerator SpawnCustomerDelay(string tutorialDawgaId, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        int seatIndex = seatManager.GetAvailableSeatIndex();

        seatManager.OccupySeat(seatIndex);

        GameObject customer = Instantiate(customerPrefab[3], spawnPoint.position, Quaternion.identity);

        Transform[] path = routesPerSeat[seatIndex].waypoints;
        Customer customerScript = customer.GetComponent<Customer>();

        customerScript.Initialize(path);
        customerScript.SetSeatInfo(seatIndex, seatManager);
        customerScript.SetTutorialCustomer("baekseolgi_finish");
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

        if (data.state == CustomerState.Walking)
        {
           //하....
        }
        else
        {
            Vector3 seatPos = path[path.Length - 1].position;
            customer.transform.position = seatPos;
        }

        customer.ApplySave(data);

        return customer;
    }
}
