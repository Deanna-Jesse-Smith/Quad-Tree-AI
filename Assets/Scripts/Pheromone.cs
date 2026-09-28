using UnityEngine;

public class Pheromone : MonoBehaviour
{
    //public Vector2 Position => transform.position;
    //public PheromoneType Type { get; private set; }
    //public int ColonyId { get; private set; }
    //public float Strength { get; private set; }

    //[SerializeField] private float evaporationRate = 0.05f; // per second
    //[SerializeField] private float minStrength = 0.01f;

    //private SpriteRenderer sr;
    //// Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
        
    //}

    //// Update is called once per frame
    //void Update()
    //{
    //    Strength -= evaporationRate * Time.deltaTime;

    //    // Visual: fade alpha with strength
    //    if (sr != null)
    //    {
    //        Color c = sr.color;
    //        c.a = Strength;
    //        sr.color = c;
    //    }

    //    if (Strength <= minStrength)
    //        Destroy(gameObject);
    //}

    //void Awake()
    //{
    //    //sr = GetComponent<SpriteRenderer>();
    //    //QTManager.instance.Register(this);
    //}

    //public void Initialise(PheromoneType type, int colonyId, float strength = 1f)
    //{
    //    Type = type;
    //    ColonyId = colonyId;
    //    Strength = strength;
    //}
    
    //void OnDestroy()
    //{
    //    QTManager.instance.Deregister(this);
    //}
}
