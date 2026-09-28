using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Civilian : Entity
{
    [Header("Detection")]
    [SerializeField] private float villainDetectionRadius = 8f;
    [SerializeField] private float victimiseRadius = 2f;
    [SerializeField] private float robbedDuration = 4f;
    [SerializeField] private float injuredDuration = 10f;
    [SerializeField] private float injuredSpeedFactor = 0.5f;
    private float injuredTimer;
    private float ogWanderSpeed;
    private float robbingTimer;

    [Header("Movement")]
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float fleeSpeed = 4f;
    [SerializeField] private float worldHalfSize = 45f;

    [Header("Timers")]
    [SerializeField] private float victimisedDuration = 3f;
    [SerializeField] private float detectionInterval = 0.5f;

    [Header("Conversion")]
    [SerializeField] private float heroExposureRadius = 10f;
    [SerializeField] private float crimeExposureRadius = 10f;
    [SerializeField] private float exposureCheckInterval = 3f;
    [SerializeField] private float vigilanteThreshold = 20f;
    [SerializeField] private float weakVillainThreshold = 20f;
    [SerializeField] private float vigilanteChance = 0.25f;
    [SerializeField] private float weakVillainChance = 0.25f;
    [SerializeField] private float heroAutographRadius = 4f;
    [SerializeField] private float autographChance = 0.3f;

    private float heroExposure = 0f;
    private float crimeExposure = 0f;
    private float exposureTimer = 0f;

    public CivilianState State { get; private set; } = CivilianState.Wandering;

    private WanderBehaviour wanderingBehaviour;
    private float victumTimer;
    private float detectionTimer;
    private Transform nearbyVillain;

    [Header("Civilian Crowding")]
    [SerializeField] private float civilianFlockRadius = 6f;
    [SerializeField] private float flockScanInterval = 2f;
    [SerializeField] private float crowdTimeout = 20f;
    [SerializeField] private float flockSeparationRadius = 1.2f;
    [SerializeField] private float flockCohesionWeight = 0.5f;
    [SerializeField] private float flockSeparationWeight = 0.4f;
    [SerializeField] private float flockMoveSpeed = 2.2f;

    public CivilianFlockGroup curCrowd { get; private set; }
    public Vector3 curDirection { get; private set; }
    private float crowdScanTimer;
    private float crowdDurationTimer;
    private Vector3 crowdDestination;
    private Transform crowdThreat;

    protected override void Start()
    {
        base.Start();
        wanderingBehaviour = gameObject.AddComponent<WanderBehaviour>();
        wanderingBehaviour.speed = wanderSpeed;
        CivilianTracker.Instance.OnCivilianSpawned();
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

        exposureTimer -= Time.deltaTime;
        if (exposureTimer <= 0f)
        {
            exposureTimer = exposureCheckInterval;
            updateExposure();
        }

        crowdScanTimer -= Time.deltaTime;
        if (crowdScanTimer <= 0f && State == CivilianState.Wandering)
        {
            crowdScanTimer = flockScanInterval;
            findFellowCrowdsmen();
        }

        if (State == CivilianState.Crowding || State == CivilianState.GroupFleeing)
        {
            crowdDurationTimer -= Time.deltaTime;
            if (crowdDurationTimer <= 0f)
                leaveCrowd();
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard.vKey.wasPressedThisFrame)
            ConvertToVigilante();
        if (keyboard.bKey.wasPressedThisFrame)
            ConvertToWeakVillain();

        HandleState();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void RunDetection()
    {
        if (State == CivilianState.Dead || State == CivilianState.Robbed || State == CivilianState.Injured || State == CivilianState.Victimised)
            return;

        Vector2 posi = new Vector2(transform.position.x, transform.position.z);
        List<Entity> groundZero = QTManager.instance.QueryRadius(posi, villainDetectionRadius);

        Transform closestVillain = null;
        bool isRevealed = false;
        float closestDist = float.MaxValue;

        foreach (Entity person in groundZero)
        {
            if (person == null)
                continue;
            if (person is not Villain vil || !vil.IsRevealed) 
                continue;
            float distance = Vector3.Distance(transform.position, person.transform.position);
            if (distance < closestDist)
            {
                closestDist = distance;
                closestVillain = person.transform;
                isRevealed = ((Villain)person).IsRevealed;
            }
        }

        if (closestVillain != null)
        {
            if (isRevealed)
            {
                nearbyVillain = closestVillain;

                if (curCrowd != null)
                    curCrowd.broadcastThreat(closestVillain);
                else
                    EnterFleeing();



                //if (closestDist < victimiseRadius)
                //    EnterVictimised();
            }
        }
        else if (State == CivilianState.Fleeing || State == CivilianState.GroupFleeing)
        {
            if (curCrowd != null)
                leaveCrowd();
            else
                EnterWandering();
        }
    }

    private void HandleState()
    {
        switch (State)
        {
            case CivilianState.Fleeing:
                if (nearbyVillain != null)
                    FleeFrom(nearbyVillain.position);
                break;

            case CivilianState.Victimised:
                victumTimer -= Time.deltaTime;
                if (victumTimer <= 0f)
                    EnterDead();
                break;
            case CivilianState.Robbed:
                robbingTimer -= Time.deltaTime;
                if (robbingTimer <= 0f)
                    EnterWandering();
                break;
            case CivilianState.Injured:
                injuredTimer -= Time.deltaTime;
                if (injuredTimer <= 0f)
                {
                    wanderingBehaviour.speed = ogWanderSpeed;
                    EnterWandering();
                }
                break;
            case CivilianState.Crowding:
                if (curCrowd == null || curCrowd.crowdCount <= 1)
                {
                    leaveCrowd();
                    break;
                }
                ApplyCivilianFlockSteering(moveTowardDestination: true);
                break;

            case CivilianState.GroupFleeing:
                if (curCrowd == null)
                {
                    EnterFleeing();
                    break;
                }
                if (crowdThreat == null)
                {
                    leaveCrowd();
                    break;
                }
                ApplyCivilianFlockSteering(moveTowardDestination: false);
                break;
            case CivilianState.Distracting:
                break;
        }
    }

    private void FleeFrom(Vector3 threatPosition)
    {
        Vector3 fleeDirection = (transform.position - threatPosition).normalized;
        Vector3 newPos = transform.position + fleeDirection * fleeSpeed * Time.deltaTime;

        newPos.x = Mathf.Clamp(newPos.x, -worldHalfSize, worldHalfSize);
        newPos.y = 0f;
        newPos.z = Mathf.Clamp(newPos.z, -worldHalfSize, worldHalfSize);

        transform.position = newPos;
    }

    public void doubtInHeroes()
    {
        if (State == CivilianState.Dead || State == CivilianState.Distracting) 
            return;
        State = CivilianState.Dead;
        
        CivilianTracker.Instance.onCivilianLeft(name);
        UpdateVisuals();
        die();
    }

    private void EnterWandering()
    {
        State = CivilianState.Wandering;
        wanderingBehaviour.enabled = true;
        wanderingBehaviour.exitScatter();
        UpdateVisuals();
    }

    private void EnterFleeing()
    {
        if (State == CivilianState.Fleeing) 
            return;
        State = CivilianState.Fleeing;
        wanderingBehaviour.enabled = false;
        wanderingBehaviour.enterScatter();
        UpdateVisuals();
    }

    public void EnterVictimised()
    {
        applyOutcomeOfCrime(CivilianOutcome.Killed);

        //State = CivilianState.Victimised;
        //wanderingBehaviour.enabled = false;
        //victumTimer = victimisedDuration;
        //UpdateVisuals();
    }

    public void applyOutcomeOfCrime(CivilianOutcome outcome)
    {
        switch (outcome)
        {
            case CivilianOutcome.Robbed:
                beingRobbed();
                break;
            case CivilianOutcome.Injured:
                beingAssulted();
                break;
            case CivilianOutcome.Killed:
                EnterDead();
                break;
        }
    }

    private void beingRobbed()
    {
        State = CivilianState.Robbed;
        robbingTimer = robbedDuration;
        wanderingBehaviour.enabled = false;
        MoraleManager.Instance.onCivilianOutcome(CivilianOutcome.Robbed);
        UpdateVisuals();
        Debug.Log($"{name} was robbed");
    }

    private void beingAssulted()
    {
        State = CivilianState.Injured;
        injuredTimer = injuredDuration;
        ogWanderSpeed = wanderingBehaviour.speed;
        wanderingBehaviour.speed = ogWanderSpeed * injuredSpeedFactor;
        wanderingBehaviour.enabled = true;
        MoraleManager.Instance.onCivilianOutcome(CivilianOutcome.Injured);
        UpdateVisuals();
        Debug.Log($"{name} was injured — speed halved for {injuredDuration}s");
    }

    private void EnterDead()
    {
        State = CivilianState.Dead;
        CivilianTracker.Instance.OnCivilianDied(false);
        UpdateVisuals();
        die();
    }

    private void updateExposure()
    {
        if (State == CivilianState.Dead) 
            return;

        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        List<Entity> groundZero = QTManager.instance.QueryRadius(pos, Mathf.Max(heroExposureRadius, crimeExposureRadius));

        bool herogroundZero = false;
        bool crimegroundZero = false;
        bool autographable = false;

        foreach (Entity person in groundZero)
        {
            float dist = Vector3.Distance(transform.position, person.transform.position);

            if (person is Hero hero && hero.State != HeroState.Fleeing)
            {
                if (dist <= heroExposureRadius)
                {
                    herogroundZero = true;
                    if (dist <= heroAutographRadius && hero.State == HeroState.Patrolling)
                        autographable = true;
                }
            }

            if (person is Villain vil && vil.IsRevealed && dist <= crimeExposureRadius)
                crimegroundZero = true;

            if (person is Building b && b.State == BuildingState.Destroyed && dist <= crimeExposureRadius)
                crimegroundZero = true;
        }

        if (herogroundZero) 
            heroExposure += exposureCheckInterval;
        if (crimegroundZero) 
            crimeExposure += exposureCheckInterval;

        if (autographable)
            TryAutographDistraction(groundZero);

        TryConversion();
    }

    private void findFellowCrowdsmen()
    {
        Vector2 posi = new Vector2(transform.position.x, transform.position.z);
        List<Entity> nearby = QTManager.instance.QueryRadius(posi, civilianFlockRadius);

        foreach (Entity person in nearby)
        {
            if (person == null) 
                continue;
            if (person is not Civilian civ) 
                continue;
            if (civ == this) 
                continue;
            if (civ.State == CivilianState.Dead) 
                continue;
            if (civ.State == CivilianState.Robbed) 
                continue;

            if (curCrowd == null && civ.curCrowd == null)
            {
                CivilianFlockGroup crowd = CivilianFlocker.instance.createCrowd(this);
                startCrowding(crowd);
                CivilianFlocker.instance.joinCrowd(crowd, civ);
                civ.startCrowding(crowd);

                broadcastNearestBuilding(crowd, posi);
            }
            else if (curCrowd == null && civ.curCrowd != null)
            {
                CivilianFlocker.instance.joinCrowd(civ.curCrowd, this);
                startCrowding(civ.curCrowd);
            }
            return;
        }
    }

    private void broadcastNearestBuilding(CivilianFlockGroup crowd, Vector2 posi)
    {
        List<Entity> buildingNearby = QTManager.instance.QueryRadius(posi, 30f);
        Building nearest = null;
        float minDist = float.MaxValue;

        foreach (Entity building in buildingNearby)
        {
            if (building == null) 
                continue;
            if (building is not Building b) 
                continue;
            if (b.State != BuildingState.Intact) 
                continue;
            float dist = Vector2.Distance(posi, new Vector2(b.transform.position.x, b.transform.position.z));
            if (dist < minDist) 
            { 
                minDist = dist; 
                nearest = b; 
            }
        }

        if (nearest != null)
            crowd.broadcastDestination(nearest.transform.position);
    }

    public void startCrowding(CivilianFlockGroup group)
    {
        if (State == CivilianState.Crowding) 
            return;
        State = CivilianState.Crowding;
        curCrowd = group;
        crowdDurationTimer = crowdTimeout;
        wanderingBehaviour.enabled = false;
        UpdateVisuals();
    }

    public void leaveCrowd()
    {
        if (curCrowd != null)
        {
            CivilianFlocker.instance?.leaveCrowd(curCrowd, this);
            curCrowd = null;
        }
        crowdThreat = null;
        crowdDestination = Vector3.zero;
        State = CivilianState.Wandering;
        wanderingBehaviour.enabled = true;
        UpdateVisuals();
    }

    public void onGroupThreatDetected(Transform threat)
    {
        crowdThreat = threat;
        crowdDurationTimer = crowdTimeout;
        State = CivilianState.GroupFleeing;
        wanderingBehaviour.enabled = false;
        UpdateVisuals();
    }

    public void onGroupDestinationSet(Vector3 destination)
    {
        crowdDestination = destination;
        crowdDurationTimer = crowdTimeout;
    }

    private void ApplyCivilianFlockSteering(bool moveTowardDestination)
    {
        Vector3 separation = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int count = 0;

        foreach (Civilian member in curCrowd.crowds)
        {
            if (member == null || member == this) continue;
            float dist = Vector3.Distance(transform.position, member.transform.position);
            count++;

            if (dist > flockSeparationRadius)
            {
                float falloff = 1f - (dist / civilianFlockRadius);
                cohesion += (member.transform.position - transform.position).normalized * falloff;
            }

            if (dist < flockSeparationRadius && dist > 0.01f)
            {
                Vector3 away = (transform.position - member.transform.position).normalized;
                separation += away * (flockSeparationRadius / dist);
            }
        }

        if (count > 0)
        {
            cohesion = (cohesion / count).normalized;
            separation = separation == Vector3.zero ? Vector3.zero : separation.normalized;
        }

        Vector3 primaryDir;
        if (moveTowardDestination && crowdDestination != Vector3.zero)
        {
            primaryDir = (crowdDestination - transform.position).normalized;
        }
        else if (!moveTowardDestination && crowdThreat != null)
        {
            primaryDir = (transform.position - crowdThreat.position).normalized;
        }
        else
        {
            leaveCrowd();
            return;
        }

        // Civilians use NO alignment — they don't match headings
        // This makes them look panicked rather than coordinated
        // Villain flocks use alignment — this is the key visual distinction
        Vector3 steering = primaryDir * 1f + cohesion * flockCohesionWeight + separation * flockSeparationWeight;

        steering = steering.normalized;

        Vector3 newPos = transform.position + steering * flockMoveSpeed * Time.deltaTime;
        newPos.x = Mathf.Clamp(newPos.x, -44f, 44f);
        newPos.y = 0f;
        newPos.z = Mathf.Clamp(newPos.z, -44f, 44f);
        transform.position = newPos;
        curDirection = steering;

        if (moveTowardDestination && crowdDestination != Vector3.zero && Vector3.Distance(transform.position, crowdDestination) < 3f)
        {
            leaveCrowd();
        }
    }

    private void TryConversion()
    {
        if (State == CivilianState.Dead || State == CivilianState.Victimised) 
            return;

        if (heroExposure >= vigilanteThreshold && Random.value < vigilanteChance)
        {
            ConvertToVigilante();
            return;
        }

        if (crimeExposure >= weakVillainThreshold && Random.value < weakVillainChance)
        {
            ConvertToWeakVillain();
        }
    }

    private void ConvertToVigilante()
    {
        Debug.Log($"{name} converting to Vigilante after {heroExposure:F0}s hero exposure");
        CrimeReport.instance?.LogEvent("Civilian converting to Vigilante");

        GameObject go = new GameObject("Vigilante");
        go.transform.position = transform.position;
        go.SetActive(false);

        var srcRenderer = GetComponentInChildren<Renderer>();
        if (srcRenderer != null)
        {
            var mesh = go.AddComponent<MeshFilter>();
            var rend = go.AddComponent<MeshRenderer>();
            mesh.mesh = srcRenderer.GetComponent<MeshFilter>()?.mesh;
            rend.material = new Material(srcRenderer.material);
        }

        go.AddComponent<Vigilante>();
        go.SetActive(true);

        CivilianTracker.Instance.OnCivilianDied(true);
        die();
    }

    private void ConvertToWeakVillain()
    {
        Debug.Log($"{name} converting to Weak Villain after {crimeExposure:F0}s crime exposure");
        CrimeReport.instance?.LogEvent("Civilian converting to Weak Villain");

        GameObject go = new GameObject("WeakVillain");
        go.transform.position = transform.position;
        go.SetActive(false);

        var srcRenderer = GetComponentInChildren<Renderer>();
        if (srcRenderer != null)
        {
            var mesh = go.AddComponent<MeshFilter>();
            var rend = go.AddComponent<MeshRenderer>();
            mesh.mesh = srcRenderer.GetComponent<MeshFilter>()?.mesh;
            rend.material = new Material(srcRenderer.material);
        }

        Villain weakVillain = go.AddComponent<Villain>();
        weakVillain.setPowerLevel(Random.Range(1f, 3f));
        weakVillain.isWeakVillain = true;
        go.SetActive(true);

        CivilianTracker.Instance.OnCivilianDied(true);
        die();
    }

    private void UpdateVisuals()
    {
        Color target = State switch
        {
            CivilianState.Wandering => Color.white,
            CivilianState.Fleeing => Color.cyan,
            CivilianState.Robbed => new Color(1f, 0.5f, 0f),
            CivilianState.Injured => new Color(0.6f, 0.2f, 0f),
            CivilianState.Victimised => Color.red,
            CivilianState.Dead => Color.grey,
            CivilianState.Crowding => new Color(0.8f, 0.9f, 1f),  // pale blue — calm group movement
            CivilianState.GroupFleeing => new Color(1f, 0.8f, 0f),    // bright yellow — panicked group flee
            _ => Color.white
        };

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
    #region Autographs
    private void TryAutographDistraction(List<Entity> groundZero)
    {
        if (Random.value > autographChance) return;

        foreach (Entity person in groundZero)
        {
            if (person is Hero h)
            {
                float dist = Vector3.Distance(transform.position, h.transform.position);
                if (dist <= heroAutographRadius && h.TryDistract(this))
                {
                    State = CivilianState.Distracting;
                    wanderingBehaviour.enabled = false;
                    Debug.Log($"{name} stopped {h.name} for an autograph");
                    return;
                }
            }
        }
    }

    public void stopDistracting()
    {
        EnterWandering();
    }
    #endregion
}

public enum CivilianState
{
    Wandering,
    Fleeing,
    Robbed,
    Injured,
    Victimised,
    Dead,
    Distracting,
    Crowding,
    GroupFleeing
}

public enum CivilianOutcome
{
    Robbed,
    Injured,
    Killed
}