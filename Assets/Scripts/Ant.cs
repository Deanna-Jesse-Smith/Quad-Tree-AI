using System.Collections.Generic;
using UnityEngine;

public class Ant : MonoBehaviour
{
    //public Vector2 Position => transform.position;

    //[SerializeField] private float speed = 3f;
    //[SerializeField] private float wanderStrength = 0.5f;
    //[SerializeField] private float obstacleAvoidRadius = 1.5f;

    //public QTManager manager;
    //private Rigidbody2D rb;
    //private Vector2 wanderDir;
    //private readonly List<Collider2D> overlapResults = new();
    //private ContactFilter2D obstacleFilter;
    public int colonyID;

    //// Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{

    //}

    //// Update is called once per frame
    //void Update()
    //{

    //}

    //void Awake()
    //{
    //    rb = GetComponent<Rigidbody2D>();
    //    wanderDir = Random.insideUnitCircle.normalized;

    //    manager.Register(this);
    //    obstacleFilter = new ContactFilter2D();
    //    obstacleFilter.SetLayerMask(LayerMask.GetMask("Obstacles"));
    //    obstacleFilter.useTriggers = false;
    //}

    //void OnDestroy()
    //{
    //    manager?.Deregister(this);
    //}

    //void FixedUpdate()
    //{
    //    Vector2 steering = wander() + avoidObstacles() * 3f;
    //    rb.linearVelocity = steering.normalized * speed;

    //    if (rb.linearVelocity.sqrMagnitude > 0.01f)
    //        transform.up = rb.linearVelocity.normalized;
    //}


    //// --- Steering behaviours ---

    //// Wander: nudges a persistent direction randomly each frame
    //private Vector2 wander()
    //{
    //    wanderDir += new Vector2(
    //        Random.Range(-1f, 1f),
    //        Random.Range(-1f, 1f)) * wanderStrength;
    //    return wanderDir.normalized;
    //}

    //// Seek: steer directly toward a world position
    //public Vector2 seek(Vector2 target)
    //{
    //    return ((Vector2)transform.position - target).normalized * -1f;
    //}

    //// Arrive: like Seek but decelerates within slowRadius
    //public Vector2 arrive(Vector2 target, float slowRadius = 1.5f)
    //{
    //    Vector2 toTarget = target - (Vector2)transform.position;
    //    float dist = toTarget.magnitude;
    //    float speedScale = dist < slowRadius ? dist / slowRadius : 1f;
    //    return toTarget.normalized * speedScale;
    //}

    //// Flee: steer directly away from a world position
    //public Vector2 flee(Vector2 threat)
    //{
    //    return ((Vector2)transform.position - threat).normalized;
    //}

    //// --- Obstacle avoidance ---
    //private Vector2 avoidObstacles()
    //{
    //    Vector2 avoidance = Vector2.zero;
    //    overlapResults.Clear();
    //    Physics2D.OverlapCircle(transform.position, obstacleAvoidRadius, obstacleFilter, overlapResults);
    //    foreach (var col in overlapResults)
    //    {
    //        Vector2 away = (Vector2)transform.position - (Vector2)col.bounds.center;
    //        avoidance += away.normalized / Mathf.Max(away.magnitude, 0.01f);
    //    }
    //    return avoidance;
    //}
}
