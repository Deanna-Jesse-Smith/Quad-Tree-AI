using UnityEngine;

public class Spawning : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private GameObject villainPrefab;
    [SerializeField] private GameObject civilianPrefab;
    [SerializeField] private GameObject buildingPrefab;

    [Header("Spawn Counts")]
    [SerializeField] private int heroCount = 5;
    [SerializeField] private int villainCount = 5;
    [SerializeField] private int civilianCount = 10;
    [SerializeField] private int buildingCount = 6;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRange = 30f;

    void Start()
    {
        SpawnGroup<Hero>(heroPrefab, heroCount);
        SpawnGroup<Villain>(villainPrefab, villainCount);
        SpawnGroup<Civilian>(civilianPrefab, civilianCount);
        SpawnBuildings();
    }

    private void SpawnGroup<T>(GameObject prefab, int count) where T : Entity
    {
        if (prefab == null)
        {
            Debug.LogWarning($"No prefab assigned for {typeof(T).Name} — skipping.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-spawnRange, spawnRange),
                0f,
                Random.Range(-spawnRange, spawnRange)
                
            );

            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            go.name = $"{typeof(T).Name}_{i}";

            if (go.GetComponent<T>() == null)
                go.AddComponent<T>();
        }
    }

    private void SpawnBuildings()
    {
        BuildingType[] types = (BuildingType[])System.Enum.GetValues(typeof(BuildingType));

        for (int i = 0; i < buildingCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-spawnRange, spawnRange),
                0f,
                Random.Range(-spawnRange, spawnRange)
            );

            GameObject go = Instantiate(buildingPrefab, pos, Quaternion.identity);
            go.name = $"Building_{i}";
            go.SetActive(false);

            Building b = go.GetComponent<Building>() ?? go.AddComponent<Building>();
            b.buildingType = types[i % types.Length];

            go.SetActive(true);
        }
    }
}
