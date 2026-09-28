using System.Collections.Generic;
using UnityEngine;

public class Vigilante : Entity
{
    [Header("Stats")]
    public float powerLevel = 2f; // always weak

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float engageRadius = 3f;
    [SerializeField] private float detectionInterval = 0.5f;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;

    private WanderBehaviour wanderBehaviour;
    private float detectionTimer;
    private Transform villainTarget;
    public bool IsEngaging { get; private set; } = false;

    protected override void Start()
    {
        base.Start();
        wanderBehaviour = gameObject.AddComponent<WanderBehaviour>();
        wanderBehaviour.speed = patrolSpeed;
        UpdateVisuals();
        Debug.Log($"Vigilante spawned at {transform.position}");
    }

    protected override void Update()
    {
        base.Update();

        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            detectionTimer = detectionInterval;
            RunDetection();
        }

        HandleState();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void RunDetection()
    {
        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        List<Entity> groundZero = QTManager.instance.QueryRadius(pos, detectionRadius);

        foreach (Entity e in groundZero)
        {
            if (e is Villain v && v.IsRevealed)
            {
                villainTarget = v.transform;
                IsEngaging = true;
                wanderBehaviour.enabled = false;
                UpdateVisuals();
                return;
            }
        }

        // No villain found
        if (IsEngaging)
        {
            IsEngaging = false;
            villainTarget = null;
            wanderBehaviour.enabled = true;
            UpdateVisuals();
        }
    }

    private void HandleState()
    {
        if (!IsEngaging || villainTarget == null) 
            return;

        Vector3 flat = new Vector3(villainTarget.position.x, 0f, villainTarget.position.z);
        Vector3 newPos = Vector3.MoveTowards(transform.position, flat, chaseSpeed * Time.deltaTime);
        newPos.x = Mathf.Clamp(newPos.x, -45f, 45f);
        newPos.z = Mathf.Clamp(newPos.z, -45f, 45f);
        transform.position = newPos;

        float dist = Vector3.Distance(transform.position, villainTarget.position);
        if (dist <= engageRadius)
        {
            if (villainTarget.TryGetComponent<Villain>(out Villain v))
                CombatManager.instance.InitiateCombat(this, v);
            else
            {
                // Villain destroyed mid-chase
                IsEngaging = false;
                villainTarget = null;
                wanderBehaviour.enabled = true;
            }
        }
    }

    private void UpdateVisuals()
    {
        Color target = IsEngaging ? Color.yellow : new Color(1f, 0.5f, 0f); // orange when patrolling
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            var mat = r.material;
            if (mat.HasProperty("_BaseColor")) 
                mat.SetColor("_BaseColor", target);
            else if (mat.HasProperty("_Color")) 
                mat.SetColor("_Color", target);
        }
    }
}