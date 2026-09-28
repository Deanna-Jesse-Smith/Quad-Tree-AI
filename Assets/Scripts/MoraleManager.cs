using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.InputSystem.HID.HID;

public class MoraleManager : MonoBehaviour
{
    public static MoraleManager Instance { get; private set; }

    [Header("General Morale")]
    [SerializeField] private float startingMorale = 100f;
    [SerializeField] private float recRate = 4f;
    [SerializeField] private float minMorale = 0f;
    [SerializeField] private float maxMorale = 100f;

    public float morale { get; private set; }

    [Header("Supervillain")]
    [SerializeField] private float supervillainMoraleThreshold = 40f;
    [SerializeField] private GameObject supervillainPrefab;
    [SerializeField] private float supervillainCooldown = 60f;

    private bool isSupervillainActive = false;
    private float supervillainCooldownTimer = 0f;

    [Header("Supervillain Backup")]
    [SerializeField] private float backupHeroDelay = 10f;
    [SerializeField] private int backupHeroCount = 2;
    [SerializeField] private float backupSpawnRange = 15f;
    private Coroutine backup;

    [Header("Population Recovery")]
    [SerializeField] private GameObject civilianPrefab;
    [SerializeField] private int recSpawnMin = 5;
    [SerializeField] private int recSpawnMax = 8;
    [SerializeField] private float recSpawnDelay = 2f;
    [SerializeField] private float recSpawnRange = 40f;

    [SerializeField] private GameObject heroPrefab;

    public float NormalisedMorale => morale / maxMorale;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        morale = Mathf.Min(morale + recRate * Time.deltaTime, maxMorale);
        //StartCoroutine(MoraleWatch());

        Hero[] heroes = FindObjectsByType<Hero>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Hero hero in heroes)
            if (hero.State == HeroState.Engaging)
                morale = Mathf.Min(morale + 0.5f * Time.deltaTime, maxMorale);

        supervillainCooldownTimer -= Time.deltaTime;

        if (!isSupervillainActive && morale <= supervillainMoraleThreshold && supervillainCooldownTimer <= 0f)
        {
            SpawnSupervillain();
        }
    }

    IEnumerator MoraleWatch()
    {
        Debug.Log($"Morale: {morale}");

        yield return new WaitForSeconds(3f);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        morale = startingMorale;
    }

    private static readonly Dictionary<BuildingType, float> MoraleCosts
    = new Dictionary<BuildingType, float>
{
        { BuildingType.Bank,             8f },
        { BuildingType.JewelleryStore,   6f },
        { BuildingType.PowerPlant,      12f },
        { BuildingType.ResidentialBlock, 3f }
};

    public void OnBuildingDestroyed(BuildingType type)
    {
        float cost = MoraleCosts.TryGetValue(type, out float val) ? val : 5f;
        morale = Mathf.Max(morale - cost, minMorale);
        Debug.Log($"Morale dropped to {morale} after {type} destroyed");
    }

    public void OnVillainDefeated()
    {
        morale = Mathf.Min(morale + 8f, maxMorale);
        Debug.Log($"Morale rose to {morale} after villain defeated");
    }

    public void OnHeroDefeated(float heroPower)
    {
        morale = Mathf.Max(morale - heroPower * 2f, minMorale);
        Debug.Log($"Morale dropped to {morale} after hero defeated");
    }

    public void SpawnSupervillain()
    {
        if (supervillainPrefab == null)
        {
            Debug.LogWarning("No mega villain prefab assigned");
            return;
        }

        Vector3 spawnPos = new Vector3(
            Random.Range(-40f, 40f), 0f, Random.Range(-40f, 40f)
        );

        GameObject go = Instantiate(supervillainPrefab, spawnPos, Quaternion.identity);
        go.SetActive(false);

        Villain mega = go.GetComponent<Villain>() ?? go.AddComponent<Villain>();
        mega.setPowerLevel(15f);
        mega.isSupervillain = true;

        go.name = "Supervillain";
        go.SetActive(true);

        isSupervillainActive = true;
        supervillainCooldownTimer = supervillainCooldown;

        Hero[] heroes = FindObjectsByType<Hero>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Hero hero in heroes)
            hero.supervillainAlert(spawnPos);

        if (backup != null)
            StopCoroutine(backup);
        backup = StartCoroutine(spawnBackupHeroes(spawnPos));

        Debug.Log($"Super villain spawned at {spawnPos} — morale was {morale:F0}");
        CrimeReport.instance?.LogEvent($"Supervillain!");
    }

    private IEnumerator spawnBackupHeroes(Vector3 supervillainPosi)
    {
        yield return new WaitForSeconds(backupHeroDelay);

        if (!isSupervillainActive)
        {
            yield break;                                                                                                                    
        }

        if (HeroTracker.instance?.heroPrefab == null)
        {
            Debug.LogWarning("No hero prefab available for backup spawn");
            yield break;
        }

        Debug.Log($"Supervillain undefeated after {backupHeroDelay}s — " +
                  $"spawning {backupHeroCount} backup heroes");
        CrimeReport.instance?.LogEvent("Backup heroes incoming!");

        for (int i = 0; i < backupHeroCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * backupSpawnRange;
            Vector3 pos = new Vector3(Mathf.Clamp(supervillainPosi.x + offset.x, -40f, 40f), 0f, Mathf.Clamp(supervillainPosi.z + offset.y, -40f, 40f));

            GameObject go = Instantiate(HeroTracker.instance.heroPrefab, pos, Quaternion.identity);
            go.SetActive(false);
            go.name = $"Hero_Backup_{i}";

            Hero hero = go.GetComponent<Hero>() ?? go.AddComponent<Hero>();
            go.SetActive(true);
            hero.backupStart();

            //HeroTracker.instance.onHeroSpawned();
            morale = Mathf.Min(morale + 1f, maxMorale);

            hero.supervillainAlert(supervillainPosi);

            CrimeReport.instance?.LogEvent($"Backup hero {go.name} arrived");
            yield return new WaitForSeconds(1f);
        }

        Hero[] heroes = FindObjectsByType<Hero>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Hero hero in heroes)
            hero.supervillainAlert(supervillainPosi);
    }

    public void onSupervillainDefeated()
    {
        isSupervillainActive = false;

        if (backup != null)
        {
            StopCoroutine(backup);
            backup = null;
        }

        morale = Mathf.Min(morale + 20f, maxMorale);
        Debug.Log($"Mega villain defeated. Morale rose to {morale:F0}");
        CrimeReport.instance.LogEvent("Supervillain Defeat!");

        StartCoroutine(MoraleRecoverySpawn());
    }

    private IEnumerator MoraleRecoverySpawn()
    {
        yield return new WaitForSeconds(recSpawnDelay);

        if (civilianPrefab == null)
        {
            Debug.LogWarning("No civilian prefab assigned for morale recovery");
            yield break;
        }

        int spawnCount = Random.Range(recSpawnMin, recSpawnMax + 1);

        for (int loop = 0; loop < spawnCount; loop++)
        {
            //Vector3 posi = new Vector3(Random.Range(-recSpawnRange, recSpawnRange), 0f, Random.Range(-recSpawnRange, recSpawnRange));
            Vector3 posi = DistrictManager.Instance.getSafeSpawnPosi(recSpawnRange);

            GameObject go = Instantiate(civilianPrefab, posi, Quaternion.identity);
            go.SetActive(false);
            go.name = $"Civilian_Returned_{loop}";

            if (go.GetComponent<Civilian>() == null)
                go.AddComponent<Civilian>();

            go.SetActive(true);
            CivilianTracker.Instance.OnCivilianSpawned();

            yield return new WaitForSeconds(0.2f);
            Debug.Log($"Spawned {spawnCount} returning civilians after supervillain defeat");
        }
    }

    public void onCivilianOutcome(CivilianOutcome outcome)
    {
        float penalty = outcome switch
        {
            CivilianOutcome.Robbed => 2f,
            CivilianOutcome.Injured => 7f,
            CivilianOutcome.Killed => 15f,
            _ => 0f
        };

        morale = Mathf.Max(morale - penalty, minMorale);
        Debug.Log($"Morale dropped to {morale:F0} after civilian {outcome}");
    }

    public void setMorale(float newMorale)
    {
        morale = Mathf.Clamp(newMorale, minMorale, maxMorale);
    }
}
