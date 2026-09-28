using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : Entity
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 12f; // og 12f
    [SerializeField] private float engageRadius = 3f;
    [SerializeField] private float buildingAidRadius = 25f;
    [SerializeField] private float detectionInterval = 0.2f;

    [Header("Movement")]
    [SerializeField] private float respondSpeed = 5f;
    [SerializeField] private float fleeSpeed = 4f;
    [SerializeField] private float aidingSpeed = 2f;
    private float respondingTimer = 0f;
    private float maxRespondingTime = 5f;
    private Vector3 lastPosition;
    private float stuckCheckInterval = 1f;
    private float stuckTimer = 0f;
    private float minProgressDistance = 0.5f;

    [Header("Stats")]
    [SerializeField] public float powerLvl = 5f;

    [Header("Building Aid")]
    [SerializeField] private float repairBoostPerSecond = 8f;

    [Header("Team Up")]
    [SerializeField] private float teamUpRadius = 20f;
    private Vector3 teamUpTarget;
    private float supervillainWaitTimer = 0f;

    public HeroState State { get; private set; } = HeroState.Patrolling;

    private HeroPatrol patrol;
    private WanderBehaviour wander;
    private float detectionTimer;

    private Transform villainTarget;
    private Building buildingTarget;
    private Vector3 crimeScenePosi;

    private bool _combatDecisionLocked = false;
    private float _combatDecisionTimer = 0f;
    [SerializeField] private float combatDecisionLockDuration = 3f;
    private bool _inActivePursuit = false;

    protected override void Start()
    {
        base.Start();
        patrol = gameObject.AddComponent<HeroPatrol>();
        powerLvl = Random.Range(3f, 10f);
        HeroTracker.instance?.onHeroSpawned();
        UpdateVisuals();
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

    public void backupStart()
    {
        base.Start();
        patrol = gameObject.AddComponent<HeroPatrol>();
        powerLvl = Random.Range(3f, 10f);
        HeroTracker.instance?.onHeroSpawned();
        UpdateVisuals();
    }

    private void RunDetection()
    {
        if (State == HeroState.Fleeing || State == HeroState.Distracted || State == HeroState.TeamUp)
            return;
        //if (State != HeroState.Patrolling || State != HeroState.AidingBuilding)
        //    return;

        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        List<Entity> groundZero = QTManager.instance.QueryRadius(pos, detectionRadius);
        Villain closestVillain = null;
        float closestDist = float.MaxValue;
        int closeCivilians = 0;
        Vector3 ongoingFight = Vector3.zero;
        bool fightDetected = false;
        bool canEvaluateCombat = State != HeroState.RespondingToCrime && State != HeroState.Engaging;

        foreach (Entity person in groundZero)
        {
            if (person == null) 
                continue;
            if (canEvaluateCombat && person is Villain vil && vil.IsRevealed)
            {
                float dist = Vector3.Distance(transform.position, person.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestVillain = vil;
                }
            }

            if (person is Civilian civ && civ.State != CivilianState.Dead)
                closeCivilians++;

            //if (person is Hero other && other != this)
            //{
            //    Debug.Log($"{name} sees {other.name} in state {other.State} " +
            //      $"at distance {Vector3.Distance(transform.position, person.transform.position):F1} " +
            //      $"pileInRadius:{teamUpRadius}");
            //}

            if (person is Hero otherHero && otherHero != this && (otherHero.State == HeroState.Engaging || State == HeroState.TeamUp))
            {
                float distance = Vector3.Distance(transform.position, person.transform.position);
                if (distance <= teamUpRadius && !fightDetected)
                {
                    fightDetected = true;
                    ongoingFight = person.transform.position;
                    //Debug.Log("Team up trigger");
                }
            }
        }

        if (closestVillain != null)
        {
            _inActivePursuit = true;
            EvaluateCombatDecision(closestVillain, closestDist, closeCivilians);
            return;
        }
        else if (fightDetected)
        {
            commenseTeamUp(ongoingFight);
            return;
        }

        _inActivePursuit = false;

        List<Entity> buildingRadius = QTManager.instance.QueryRadius(pos, buildingAidRadius);
        Building damagedBuilding = null;

        //Debug.Log($"{name} building scan found {buildingRadius.Count} entities in radius {buildingAidRadius}");

        foreach (Entity things in buildingRadius)
        {
            if (things == null)
                continue;

            if (things is Building building)
            {
                //Debug.Log($"  Found building {building.name} state:{building.State} isBeingAided:{building.isBeingAided}");
                if ((building.State == BuildingState.Rebuilding || building.State == BuildingState.Destroyed) && !building.isBeingAided && damagedBuilding == null)
                    damagedBuilding = building;
            }
        }
        //Debug.Log($"{name} damagedBuilding result: {(damagedBuilding == null ? "null" : damagedBuilding.name)}");

        // Priority: villain > team up > damaged building > patrol

        if (damagedBuilding != null)
            EnterAidingBuilding(damagedBuilding);
        else if (State == HeroState.AidingBuilding)
            EnterPatrolling();
    }

    private void commenseTeamUp(Vector3 fightPosi)
    {
        if (State == HeroState.TeamUp)
            return;

        State = HeroState.TeamUp;
        patrol.IsEnabled = false;
        teamUpTarget = fightPosi;

        Debug.Log($"{name} piling in to fight at {fightPosi}");
        UpdateVisuals();
    }

    private void EvaluateCombatDecision(Villain villain, float distance, int groundZeroCivilians)
    {
        if (_combatDecisionLocked)
        {
            _combatDecisionTimer -= Time.deltaTime;
            if (_combatDecisionTimer <= 0f)
                _combatDecisionLocked = false;
            return;
        }

        float differential = CombatResolution.powerDifferential(powerLvl, villain.powerLvl);

        bool heroIsWeak = differential > 0.4f;
        bool civiliansgroundZero = groundZeroCivilians > 0;

        if (villain.isSupervillain)
        {
            commenseSupervillainResponse(villain, distance);
            return;
        }

        if (heroIsWeak)
        {
            if (civiliansgroundZero)
            {
                float engageChance = Mathf.Lerp(0.8f, 0.3f, differential);
                if (Random.value < engageChance)
                {
                    Debug.Log($"{name} engaging despite disadvantage — civilians at risk " + $"(differential:{differential:F2}, chance:{engageChance:F2})");
                    ProceedWithEngagement(villain, distance);
                    LockCombatDecision();
                }
                else
                {
                    Debug.Log($"{name} too outmatched even with civilians groundZero — fleeing");
                    LockCombatDecision();
                    EnterFleeing(villain.transform);
                }
            }
            else
            {
                Debug.Log($"{name} avoiding stronger villain " +
                          $"(differential:{differential:F2})");
                LockCombatDecision();
                EnterFleeing(villain.transform);
            }
        }
        else
        {
            LockCombatDecision();
            ProceedWithEngagement(villain, distance);
        }
    }

    private void LockCombatDecision()
    {
        _combatDecisionLocked = true;
        _combatDecisionTimer = combatDecisionLockDuration;
    }

    private void ProceedWithEngagement(Villain villain, float distance)
    {
        villainTarget = villain.transform;

        if (distance <= engageRadius)
        {
            EnterEngaging();
            CombatManager.instance.InitiateCombat(this, villain);
        }
        else
        {
            EnterRespondingToCrime(villain.transform.position);
        }
    }

    private void HandleState()
    {
        switch (State)
        {
            case HeroState.RespondingToCrime:
                MoveTowards(crimeScenePosi, respondSpeed);

                respondingTimer += Time.deltaTime;
                stuckTimer += Time.deltaTime;

                if (stuckTimer >= stuckCheckInterval)
                {
                    stuckTimer = 0f;
                    float progress = Vector3.Distance(transform.position, lastPosition);
                    if (progress < minProgressDistance)
                    {
                        Debug.Log($"{name} stuck in RespondingToCrime — giving up");
                        EnterPatrolling();
                        break;
                    }
                    lastPosition = transform.position;
                }

                if (respondingTimer >= maxRespondingTime)
                {
                    Debug.Log($"{name} timed out in RespondingToCrime");
                    EnterPatrolling();
                    break;
                }

                float arrivalDist = Vector3.Distance(transform.position, crimeScenePosi);
                if (arrivalDist < 2f)
                {
                    detectionTimer = 0f;
                    EnterPatrolling();
                }
                break;

            case HeroState.Engaging:
                if (villainTarget != null)
                {
                    MoveTowards(villainTarget.position, respondSpeed);
                    float dist = Vector3.Distance(transform.position, villainTarget.position);
                    if (dist <= engageRadius)
                    {
                        if (villainTarget.TryGetComponent<Villain>(out Villain v))
                            CombatManager.instance.InitiateCombat(this, v);
                    }
                    else if (villainTarget.GetComponent<Villain>() == null)
                        EnterPatrolling();
                } else 
                    EnterPatrolling();
                break;

            case HeroState.Fleeing:
                if (villainTarget != null)
                {
                    float dist = Vector3.Distance(transform.position, villainTarget.position);
                    if (dist > 20f || villainTarget.GetComponent<Villain>() == null)
                    {
                        villainTarget = null;
                        EnterPatrolling();
                    }
                    else
                        FleeFrom(villainTarget.position);
                }
                else
                    EnterPatrolling();
                break;

            case HeroState.AidingBuilding:
                if (buildingTarget != null)
                {
                    MoveTowards(buildingTarget.transform.position, aidingSpeed);
                    ApplyRepairBoost();
                }
                else
                    EnterPatrolling();
                break;
            case HeroState.TeamUp:
                MoveTowards(teamUpTarget, respondSpeed);
                float teamUpDistance = Vector3.Distance(transform.position, teamUpTarget);

                if (teamUpDistance < engageRadius * 2f)
                {
                    Vector2 posi = new Vector2(transform.position.x, transform.position.z);
                    List<Entity> groundZero = QTManager.instance.QueryRadius(posi, engageRadius * 3f);

                    foreach (Entity person in groundZero)
                    {
                        if (person == null) 
                            continue;
                        if (person is Villain vil && vil.IsRevealed)
                        {
                            villainTarget = vil.transform;
                            EnterEngaging();
                            return;
                        }
                    }
                    EnterPatrolling();
                }
                break;
            case HeroState.Distracted:
                distractionTimer -= Time.deltaTime;
                if (distractingCivilian == null)
                {
                    EnterPatrolling();
                }else if (distractionTimer <= 0f)
                {
                    distractingCivilian.stopDistracting();
                    distractingCivilian = null;
                    EnterPatrolling();
                }
                break;
        }
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 flat = new Vector3(target.x, 0f, target.z);
        Vector3 newPosi = Vector3.MoveTowards(transform.position, flat, speed * Time.deltaTime);
        newPosi.x = Mathf.Clamp(newPosi.x, -45f, 45f);
        newPosi.z = Mathf.Clamp(newPosi.z, -45f, 45f);
        transform.position = newPosi;
    }

    private void FleeFrom(Vector3 threat)
    {
        Vector3 dir = (transform.position - threat).normalized;
        Vector3 newPos = transform.position + dir * fleeSpeed * Time.deltaTime;
        newPos.x = Mathf.Clamp(newPos.x, -45f, 45f);
        newPos.y = 0f;
        newPos.z = Mathf.Clamp(newPos.z, -45f, 45f);
        transform.position = newPos;

        //StartCoroutine(FleeDuration());
    }

    IEnumerator FleeDuration()
    {
        yield return new WaitForSeconds(2);
        State = HeroState.Patrolling;
    }

    private void ApplyRepairBoost()
    {
        if (buildingTarget == null || buildingTarget.State == BuildingState.Intact)
        {
            EnterPatrolling();
            return;
        }

        float dist = Vector3.Distance(transform.position, buildingTarget.transform.position);
        if (dist <= buildingAidRadius)
        {
            buildingTarget.getBoost(repairBoostPerSecond * Time.deltaTime);
            DistrictManager.Instance.ReduceCrime(new Vector2(transform.position.x, transform.position.z), 2f * Time.deltaTime);
        }
    }

    private void releaseBuilding()
    {
        if (buildingTarget != null)
        {
            buildingTarget.setAided(false);
            buildingTarget = null;
        }
    }

    public void supervillainAlert(Vector3 posi)
    {
        if (State == HeroState.Fleeing || State == HeroState.Engaging)
            return;

        Debug.Log($"{name} alterted to supervillain");
        EnterRespondingToCrime(posi);
        detectionTimer = 0f;
    }

    private void commenseSupervillainResponse(Villain supervillain, float distance)
    {
        Vector2 posi = new Vector2(transform.position.x, transform.position.z);
        List<Entity> groundZero = QTManager.instance.QueryRadius(posi, teamUpRadius);

        int alliedHeroes = 0;
        foreach (Entity person in groundZero)
        {
            if (person == null)
                continue;
            if (person is Hero otherHero && otherHero != this
                && otherHero.State != HeroState.Fleeing)
                alliedHeroes++;
        }

        if (alliedHeroes >= 2)
        {
            Debug.Log($"{name} engaging mega villain with {alliedHeroes} allies");
            ProceedWithEngagement(supervillain, distance);
        }
        else
        {
            supervillainWaitTimer += detectionInterval;

            if (supervillainWaitTimer >= 8f)
            {
                Debug.Log($"{name} engaging supervillain alone — waited too long");
                supervillainWaitTimer = 0f;
                ProceedWithEngagement(supervillain, distance);
            }
            else
            {
                Debug.Log($"{name} waiting for allies — have {alliedHeroes}, need 2 " +
                          $"({supervillainWaitTimer:F0}s elapsed)");
                EnterRespondingToCrime(supervillain.transform.position);
            }
        }
    }

    private void EnterPatrolling()
    {
        releaseBuilding();
        State = HeroState.Patrolling;
        supervillainWaitTimer = 0f;
        _inActivePursuit = false;
        patrol.IsEnabled = true;
        villainTarget = null;
        _combatDecisionLocked = false;
        if (buildingTarget != null)
        {
            buildingTarget.setAided(false);
            buildingTarget = null;
        }

        UpdateVisuals();
    }

    private void EnterRespondingToCrime(Vector3 position)
    {
        if (State == HeroState.RespondingToCrime) 
            return;
        releaseBuilding();
        State = HeroState.RespondingToCrime;
        patrol.IsEnabled = false;
        crimeScenePosi = position;
        respondingTimer = 0f;
        stuckTimer = 0f;
        lastPosition = transform.position;
        Debug.Log($"{name} responding to villain at {crimeScenePosi}");
        UpdateVisuals();
    }

    public void EnterEngaging()
    {
        State = HeroState.Engaging;
        patrol.IsEnabled = false;
        UpdateVisuals();
    }

    public void EnterFleeing(Transform threat)
    {
        State = HeroState.Fleeing;
        releaseBuilding();
        patrol.IsEnabled = false;
        villainTarget = threat;
        UpdateVisuals();
    }

    private void EnterAidingBuilding(Building building)
    {
        if (State == HeroState.AidingBuilding) 
            return;
        State = HeroState.AidingBuilding;
        patrol.IsEnabled = false;
        buildingTarget = building;
        buildingTarget.setAided(true);
        Debug.Log($"{name} aiding {buildingTarget.name}");
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (State == HeroState.AidingBuilding)
            Debug.Log($"{name} is aiding building");
        var renderers = GetComponentsInChildren<Renderer>();
        Color target = State switch
        {
            HeroState.Patrolling => Color.green,
            HeroState.RespondingToCrime => Color.yellow,
            HeroState.Engaging => Color.red,
            HeroState.Fleeing => Color.magenta,
            HeroState.AidingBuilding => Color.cyan,
            HeroState.Distracted => new Color(1f, 0.75f, 0.8f), // light pink
            HeroState.TeamUp => new Color(1f, 0.5f, 0f), // orange
            _ => Color.green
        };

        Debug.Log($"{name} visuals updated to {target} for state {State}");

        foreach (var r in renderers)
        {
            var mat = r.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", target);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", target);
        }
    }

    #region Autograph
    [SerializeField] private float distractedDuration = 3f;
    private float distractionTimer;
    public Civilian distractingCivilian;

    public bool TryDistract(Civilian civ)
    {
        if (State != HeroState.Patrolling && distractingCivilian == null) 
            return false;
        EnterDistracted();
        distractingCivilian = civ;
        return true;
    }

    private void EnterDistracted()
    {
        State = HeroState.Distracted;
        patrol.IsEnabled = false;
        distractionTimer = distractedDuration;
        UpdateVisuals();
        Debug.Log($"{name} distracted by autograph request");
    }
    #endregion



}
