using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.HID.HID;

public class QTManager : MonoBehaviour
{
    [Header("World Bounds")]
    [SerializeField] private float worldWidth = 100f;
    [SerializeField] private float worldHeight = 100f;
    public static QTManager instance { get; private set; }
    private readonly List<IQuadTreeEntity> entities = new();
    private QuadTree tree;
    public QuadTree Tree => tree;
    private bool drawTree;

    private float cleanUpInterval = 5f;
    private float cleanUpTimer = 0f;

    private Material border;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        cleanUpTimer -= Time.deltaTime;
        if (cleanUpTimer <= 0f)
        {
            cleanUpTimer = cleanUpInterval;
            tree.cleanUpTree();
        }


        Keyboard keyboard = Keyboard.current;
        if (keyboard.mKey.wasPressedThisFrame)
        {
            MoraleManager.Instance.SpawnSupervillain();
        }

        if (keyboard.cKey.wasPressedThisFrame)
        {
            ForceVillainCollaboration();
        }

        if (keyboard.kKey.wasPressedThisFrame)
            forceHeroicDefeat();

        if (keyboard.xKey.wasPressedThisFrame)
            ForceKillSupervillain();

        if (keyboard.rKey.wasPressedThisFrame)
            forceReveal();

        if (keyboard.gKey.wasPressedThisFrame)
            ForceVillainFlock();

        if (keyboard.qKey.wasPressedThisFrame)
            drawTree = !drawTree;

        if (keyboard.digit1Key.wasPressedThisFrame)
            MoraleManager.Instance.setMorale(10f);

        if (keyboard.digit2Key.wasPressedThisFrame)
            MoraleManager.Instance.setMorale(90f);

        if (drawTree)
            tree.DrawDebugLines();

        DebugTreeState(keyboard);
    }

    void Awake()
    {
        if (instance != null && instance != this) 
        { 
            Destroy(gameObject); 
            //return; 
        }
        instance = this;

        Rect worldBounds = new Rect(-worldWidth / 2f, -worldHeight / 2f, worldWidth, worldHeight);
        tree = new QuadTree(worldBounds, 0);

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        border = new Material(shader);
        border.hideFlags = HideFlags.HideAndDontSave;
        border.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        border.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        border.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        border.SetInt("_ZWrite", 0);
    }

    void DebugTreeState(Keyboard keyboard)
    {
        if (keyboard.dKey.wasPressedThisFrame)
        {
            Debug.Log($"Tree bounds: {tree.bounds}");
            Debug.Log($"Registered entities: {entities.Count}");
            foreach (Entity e in entities)
            {
                Vector2 pos = new Vector2(e.transform.position.x, e.transform.position.z);
                bool inside = tree.bounds.Contains(pos);
                Debug.Log($"{e.name} at {pos} | inside bounds: {inside}");
            }
        }
    }

    public void Register(Entity entity, Vector2 position)
    {
        if (entity == null) 
            return;
        bool inserted = tree.insert(entity, position);
        if (!inserted)
            Debug.LogWarning($"{entity.name} at {position} is outside world bounds {tree.bounds}");
        entities.Add(entity);
        //Debug.Log($"Total registered: {entities.Count}");
    }

    public void Deregister(Entity entity, Vector2 position)
    {
        tree.remove(entity, position);
        entities.Remove(entity);
    }

    public List<Entity> QueryRadius(Vector2 center, float radius)
    {
        //Debug.Log("Querying radius: " + center + ", " + radius);
        var results = new List<Entity>();
        tree?.queryRadius(center, radius, results);
        return results;
    }

    void OnDrawGizmos()
    {
        if (!drawTree || tree == null) 
            return;
        tree.DrawGizmos();
    }

    private void DrawNode(QuadTree node)
    {
        if (node == null) 
            return;
        Gizmos.color = Color.green;
        
        Vector3 center = new Vector3(node.bounds.center.x, 0, node.bounds.center.y);
        Vector3 size = new Vector3(node.bounds.width, 0, node.bounds.height);
        Gizmos.DrawWireCube(center, size);

        if (node.isSubdivided)
        {
            DrawNode(node.NE);
            DrawNode(node.NW);
            DrawNode(node.SE);
            DrawNode(node.SW);
        }
    }

    void DrawNode(QuadTree node, int depth)
    {
        float t = Mathf.Clamp01(depth * 0.15f);
        Gizmos.color = Color.Lerp(Color.green, Color.red, t);

        Rect bounds = node.bounds;

        Vector3 bottomLeft = new Vector3(bounds.xMin, bounds.yMin, 0f);
        Vector3 bottomRight = new Vector3(bounds.xMax, bounds.yMin, 0f);
        Vector3 topRight = new Vector3(bounds.xMax, bounds.yMax, 0f);
        Vector3 topLeft = new Vector3(bounds.xMin, bounds.yMax, 0f);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        if (node.isSubdivided)
        {
            foreach (var child in node.children)
            {
                DrawNode(child, depth + 1);
            }
        }
    }

    void DrawNodeDebug(QuadTree node)
    {
        Rect b = node.bounds;

        Vector3 bl = new Vector3(b.xMin, b.yMin, 0f);
        Vector3 br = new Vector3(b.xMax, b.yMin, 0f);
        Vector3 tr = new Vector3(b.xMax, b.yMax, 0f);
        Vector3 tl = new Vector3(b.xMin, b.yMax, 0f);

        Debug.DrawLine(bl, br, Color.white);
        Debug.DrawLine(br, tr, Color.white);
        Debug.DrawLine(tr, tl, Color.white);
        Debug.DrawLine(tl, bl, Color.white);

        if (node.isSubdivided)
        {
            foreach (var child in node.children)
                DrawNodeDebug(child);
        }
    }

    private void forceHeroicDefeat()
    {
        Hero[] heroes = FindObjectsByType<Hero>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        Hero strongest = null;
        float maxPower = 0f;
        foreach (Hero h in heroes)
        {
            if (h.powerLvl > maxPower)
            {
                maxPower = h.powerLvl;
                strongest = h;
            }
        }

        if (strongest == null)
        {
            Debug.LogWarning("No heroes available to force defeat");
            return;
        }

        Debug.Log($"Force defeating {strongest.name} (power:{strongest.powerLvl:F1})");

        Vector2 pos = new Vector2(strongest.transform.position.x, strongest.transform.position.z);
        List<Entity> nearby = QTManager.instance.QueryRadius(pos, 50f);

        Villain attacker = null;
        foreach (Entity e in nearby)
        {
            if (e is Villain v) { attacker = v; break; }
        }

        if (attacker != null)
            CombatManager.instance.forceHeroicDefeat(strongest, attacker);
        else
            Debug.LogWarning("No villain found to attribute defeat to — spawn a villain first");
    }

    private void ForceKillSupervillain()
    {
        Villain[] villains = FindObjectsByType<Villain>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None
        );
        foreach (Villain v in villains)
        {
            if (v.isSupervillain)
            {
                MoraleManager.Instance.onSupervillainDefeated();
                v.die();
                Debug.Log("Force killed supervillain");
                return;
            }
        }
        Debug.LogWarning("No supervillain in scene — press M first");
    }

    private void ForceVillainFlock()
    {
        Villain[] villains = FindObjectsByType<Villain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < villains.Length; i++)
        {
            if (!villains[i].IsRevealed) villains[i].forceReveal();
            if (villains[i].isSupervillain) continue;
            if (villains[i].CurrentGroup != null) continue;

            for (int j = i + 1; j < villains.Length; j++)
            {
                if (!villains[j].IsRevealed) villains[j].forceReveal();
                if (villains[j].isSupervillain) continue;
                if (villains[j].CurrentGroup != null) continue;

                FlockGroup group = VillainFlocker.Instance.CreateGroup(villains[i]);
                villains[i].EnterFlocking(group);
                VillainFlocker.Instance.JoinGroup(group, villains[j]);
                villains[j].EnterFlocking(group);

                Debug.Log($"Force flock: {villains[i].name} + {villains[j].name}");
                CrimeReport.instance?.LogEvent("Villain gang formed!");
                return;
            }
        }

        Debug.LogWarning("Could not find two suitable villains to force flock");
    }


    private void ForceVillainCollaboration()
    {
        var villains = FindObjectsByType<Villain>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None
        );
        if (villains.Length < 2) return;
        villains[0].forcePartnersInCrime(villains[1]);
    }

    private void forceReveal()
    {
        Villain[] villains = FindObjectsByType<Villain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Villain vil in villains)
        {
            if (!vil.IsRevealed && !vil.isSupervillain)
            {
                vil.forceReveal();
                Debug.Log($"Force revealed {vil.name}");
                return;
            }
        }
        Debug.LogWarning("No hidden villains to force reveal");
    }

    void OnRenderObject()
    {
        if (drawTree || tree == null) return;

        border.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(Color.green);
        DrawNodeGL(tree);
        GL.End();
    }

    private void DrawNodeGL(QuadTree node)
    {
        if (node == null) return;

        Rect b = node.bounds;
        Vector3 bl = new Vector3(b.xMin, 0f, b.yMin);
        Vector3 br = new Vector3(b.xMax, 0f, b.yMin);
        Vector3 tl = new Vector3(b.xMin, 0f, b.yMax);
        Vector3 tr = new Vector3(b.xMax, 0f, b.yMax);

        GL.Vertex(bl); GL.Vertex(br);
        GL.Vertex(br); GL.Vertex(tr);
        GL.Vertex(tr); GL.Vertex(tl);
        GL.Vertex(tl); GL.Vertex(bl);

        if (node.isSubdivided)
        {
            DrawNodeGL(node.NE);
            DrawNodeGL(node.NW);
            DrawNodeGL(node.SE);
            DrawNodeGL(node.SW);
        }
    }
}
