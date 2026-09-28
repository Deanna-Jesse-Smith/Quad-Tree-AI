using UnityEngine;
using UnityEngine.InputSystem;

public class DistrictManager : MonoBehaviour
{
    public static DistrictManager Instance { get; private set; }

    [Header("World Settings")]
    [SerializeField] private float worldSize = 90f; // matches your spawn range * 2
    [SerializeField] private int gridSize;   // 3x3 = 9 districts
    [SerializeField] private float decayRate = 0.5f; // per second per district
    [SerializeField] private float decayInterval = 1f;   // decay every 1 second

    private District[,] _districts;
    private float _decayTimer;

    // Inspector visibility — Unity can't show a 2D array directly
    [SerializeField]
    private float[] _crimeLevelsFlat;

    private bool showDistricts = true;
    [SerializeField] private Material districtMaterial;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _decayTimer -= Time.deltaTime;
        if (_decayTimer <= 0f)
        {
            _decayTimer = decayInterval;
            DecayCrime();
        }

        // Keep flat array in sync for Inspector visibility
        SyncInspectorArray();

        Keyboard keyboard = Keyboard.current;
        if (keyboard.dKey.wasPressedThisFrame)
        {
            Debug.Log("dkey");
            showDistricts = !showDistricts;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InitialiseDistricts();

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        districtMaterial = new Material(shader);
        districtMaterial.hideFlags = HideFlags.HideAndDontSave;
        districtMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        districtMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        districtMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        districtMaterial.SetInt("_ZWrite", 0);
    }

    void OnRenderObject()
    {
        if (!showDistricts || _districts == null) return;

        districtMaterial.SetPass(0);

        // Draw filled quads first
        GL.Begin(GL.QUADS);
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                District d = _districts[x, y];
                float crime = d.CrimeLevel / 100f; // 0-1
                float alpha = Mathf.Lerp(0.05f, 0.35f, crime); // subtle at low crime

                // Colour shifts green to yellow to red as crime rises
                Color fill = crime < 0.5f
                    ? Color.Lerp(new Color(0f, 1f, 0f, alpha), new Color(1f, 1f, 0f, alpha), crime * 2f)
                    : Color.Lerp(new Color(1f, 1f, 0f, alpha), new Color(1f, 0f, 0f, alpha), (crime - 0.5f) * 2f);

                GL.Color(fill);

                Rect b = d.Bounds;
                // Draw on XZ plane at Y=0.01 to sit just above ground
                GL.Vertex(new Vector3(b.xMin, 0.01f, b.yMin));
                GL.Vertex(new Vector3(b.xMax, 0.01f, b.yMin));
                GL.Vertex(new Vector3(b.xMax, 0.01f, b.yMax));
                GL.Vertex(new Vector3(b.xMin, 0.01f, b.yMax));
            }
        }
        GL.End();

        // Draw border lines on top
        GL.Begin(GL.LINES);
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                District d = _districts[x, y];
                float crime = d.CrimeLevel / 100f;
                float alpha = Mathf.Lerp(0.3f, 1f, crime);
                GL.Color(new Color(1f, 1f, 1f, alpha));

                Rect b = d.Bounds;
                GL.Vertex(new Vector3(b.xMin, 0.02f, b.yMin));
                GL.Vertex(new Vector3(b.xMax, 0.02f, b.yMin));

                GL.Vertex(new Vector3(b.xMax, 0.02f, b.yMin));
                GL.Vertex(new Vector3(b.xMax, 0.02f, b.yMax));

                GL.Vertex(new Vector3(b.xMax, 0.02f, b.yMax));
                GL.Vertex(new Vector3(b.xMin, 0.02f, b.yMax));

                GL.Vertex(new Vector3(b.xMin, 0.02f, b.yMax));
                GL.Vertex(new Vector3(b.xMin, 0.02f, b.yMin));
            }
        }
        GL.End();
    }

    private void InitialiseDistricts()
    {
        _districts = new District[gridSize, gridSize];
        float districtSize = worldSize / gridSize;
        float origin = -worldSize / 2f;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Rect bounds = new Rect(
                    origin + x * districtSize,
                    origin + y * districtSize,
                    districtSize,
                    districtSize
                );
                _districts[x, y] = new District($"District_{x}_{y}", bounds);
            }
        }

        _crimeLevelsFlat = new float[gridSize * gridSize];
        Debug.Log($"Initialised {gridSize * gridSize} districts");
    }

    private void DecayCrime()
    {
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                _districts[x, y].Decay(decayRate);
    }

    private void SyncInspectorArray()
    {
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                _crimeLevelsFlat[x * gridSize + y] = _districts[x, y].CrimeLevel;
    }

    // --- Public API ---

    public District GetDistrict(Vector2 position)
    {
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                if (_districts[x, y].Contains(position))
                    return _districts[x, y];

        // Position outside all districts — return nearest
        return GetNearestDistrict(position);
    }

    private District GetNearestDistrict(Vector2 position)
    {
        District nearest = _districts[0, 0];
        float minDist = float.MaxValue;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                float dist = Vector2.Distance(position, _districts[x, y].Bounds.center);
                if (dist < minDist) { minDist = dist; nearest = _districts[x, y]; }
            }
        }
        return nearest;
    }

    public bool isSafeToSpawn(Vector2 posi)
    {
        District district = GetDistrict(posi);
        if (district.CrimeLevel >= 80f)
            return false;
        if (district.CrimeLevel >= 50f)
            return Random.value > 0.5f;
        return true;
    }

    public Vector3 getSafeSpawnPosi(float range)
    {
        for (int loop = 0; loop < 10; loop++)
        {
            Vector3 posi = new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range));
            Vector2 posi2D = new Vector2(posi.x, posi.z);

            if (isSafeToSpawn(posi2D))
                return posi;
        }

        return new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
    }

    public void RaiseCrime(Vector2 position, float amount)
    {
        District d = GetDistrict(position);
        d.RaiseCrime(amount);
        Debug.Log($"Crime raised in {d.Name} to {d.CrimeLevel:F1}");
    }

    public void ReduceCrime(Vector2 position, float amount)
    {
        District d = GetDistrict(position);
        d.ReduceCrime(amount);
        Debug.Log($"Crime reduced in {d.Name} to {d.CrimeLevel:F1}");
    }

    public District GetHighestCrimeDistrict()
    {
        District highest = _districts[0, 0];
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                if (_districts[x, y].CrimeLevel > highest.CrimeLevel)
                    highest = _districts[x, y];
        return highest;
    }

    public District GetDistrict(int x, int y) => _districts[x, y];
    public int GridSize => gridSize;
}

public class District
{
    public string Name { get; private set; }
    public Rect Bounds { get; private set; }
    public float CrimeLevel { get; private set; }

    public District(string name, Rect bounds)
    {
        Name = name;
        Bounds = bounds;
        CrimeLevel = 0f;
    }

    public void RaiseCrime(float amount)
    {
        CrimeLevel = Mathf.Min(CrimeLevel + amount, 100f);
    }

    public void ReduceCrime(float amount)
    {
        CrimeLevel = Mathf.Max(CrimeLevel - amount, 0f);
    }

    public void Decay(float amount)
    {
        CrimeLevel = Mathf.Max(CrimeLevel - amount, 0f);
    }

    public bool Contains(Vector2 position)
    {
        return position.x >= Bounds.xMin && position.x <= Bounds.xMax
            && position.y >= Bounds.yMin && position.y <= Bounds.yMax;
    }
}
