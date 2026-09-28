using System.Collections;
using UnityEngine;
using static UnityEngine.InputSystem.HID.HID;

public class HeroTracker : MonoBehaviour
{
    public static HeroTracker instance { get; private set; }

    [Header("Population")]
    [SerializeField] public GameObject heroPrefab;
    [SerializeField] private int minHeroes = 5;
    [SerializeField] private float populationCheckInterval = 15f;
    [SerializeField] private float spawnRange = 40f;

    public int heroCount { get; private set; }
    private float checkTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = populationCheckInterval;
            CheckMinimumPopulation();
        }
    }

    void Awake()
    {
        if (instance != null && instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        instance = this;
    }

    public void onHeroSpawned()
    {
        heroCount++;
    }

    public void OnHeroDied()
    {
        heroCount = Mathf.Max(0, heroCount - 1);
        Debug.Log($"Hero died. Remaining: {heroCount}");
    }

    private void CheckMinimumPopulation()
    {
        if (heroCount >= minHeroes) return;
        if (heroPrefab == null)
        {
            Debug.LogWarning("No hero prefab assigned on HeroTracker");
            return;
        }

        int deficit = minHeroes - heroCount;
        Debug.Log($"Hero count below minimum — spawning {deficit} replacement heroes");
        StartCoroutine(SpawnReplacementHeroes(deficit));
    }

    private IEnumerator SpawnReplacementHeroes(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-spawnRange, spawnRange),
                0f,
                Random.Range(-spawnRange, spawnRange)
            );

            GameObject go = Instantiate(heroPrefab, pos, Quaternion.identity);
            go.SetActive(false);
            go.name = $"Hero_Replacement_{i}";

            if (go.GetComponent<Hero>() == null)
                go.AddComponent<Hero>();

            go.SetActive(true);
            onHeroSpawned();

            CrimeReport.instance?.LogEvent("Replacement hero arrived");
            yield return new WaitForSeconds(0.5f);
        }
    }
}
