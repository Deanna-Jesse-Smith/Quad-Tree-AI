using UnityEngine;

public class CivilianTracker : MonoBehaviour
{
    public static CivilianTracker Instance { get; private set; }

    public int CivilianCount { get; private set; }

    [SerializeField] private float moralePenaltyPerDeath = 3f;

    [Header("Minimum Population")]
    [SerializeField] private GameObject civilianPrefab;
    [SerializeField] private int minPop = 5;
    [SerializeField] private float popCheckInterval = 10f;
    [SerializeField] private float emergencySpawnRange = 40f;
    private float popCheckTimer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        popCheckTimer -= Time.deltaTime;

        if (popCheckTimer <= 0f)
        {
            popCheckTimer = popCheckInterval;
            checkMinPop();
        }
    }

    private void checkMinPop()
    {
        if (CivilianCount < minPop)
        {
            if (civilianPrefab == null)
                return;

            int spawnCount = minPop - CivilianCount;
            Debug.Log($"Population below minimum — spawning {spawnCount} emergency civilians");

            for (int loop = 0; loop < spawnCount; loop++)
            {
                //Vector3 posi = new Vector3(Random.Range(-emergencySpawnRange, emergencySpawnRange), 0f, Random.Range(-emergencySpawnRange, emergencySpawnRange));
                Vector3 posi = DistrictManager.Instance.getSafeSpawnPosi(emergencySpawnRange);

                GameObject go = Instantiate(civilianPrefab, posi, Quaternion.identity);
                go.SetActive(false);
                go.name = $"Civilian_Emergency_{loop}";

                if (go.GetComponent<Civilian>() == null)
                    go.AddComponent<Civilian>();

                go.SetActive(true);
                OnCivilianSpawned();
            }
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
    }

    public void OnCivilianSpawned() => CivilianCount++;

    public void OnCivilianDied(bool byConversion)
    {
        CivilianCount = Mathf.Max(0, CivilianCount - 1);
        
        if (!byConversion)
            MoraleManager.Instance.onCivilianOutcome(CivilianOutcome.Killed);
        Debug.Log($"Civilian died. Remaining: {CivilianCount}");
    }

    public void onCivilianLeft(string name)
    {
        CivilianCount = Mathf.Max(0, CivilianCount - 1);
        Debug.Log($"{name} left. Remaining: {CivilianCount}");
    }

}
