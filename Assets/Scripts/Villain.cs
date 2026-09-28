using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.HID.HID;

public class Villain : Entity
{
    [Header("Detection")]
    [SerializeField] private float buildingDetectionRadius = 10f;
    [SerializeField] private float heroDetectionRadius = 10f;
    [SerializeField] private float civilianTargetRadius = 5f;
    [SerializeField] private float detectionInterval = 0.5f;

    [Header("Discovery")]
    [SerializeField] private float discoveryChance = 0.05f;

    [Header("Movement")]
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float crimeSpeed = 3f;

    [Header("Stats")]
    [SerializeField] public float powerLvl = 5f;
    private bool overridePowerLvl = false;

    [Header("Crime")]
    [SerializeField] private float crimeCommitDuration = 1.5f;
    [SerializeField] private float civilianTargetChance = 0.3f;
    [SerializeField] private float buildingDamageAmount = 40f;

    [Header("Incentivisation")]
    [SerializeField] private float incentiveEvaluationInterval = 1f;

    [Header("Collaboration")]
    [SerializeField] private float collaborationRadius = 8f;
    [SerializeField] private float collaborationInterval = 3f;
    [SerializeField] private float crimeDamageAmplifier = 1.8f;

    private Villain partnerInCrime;
    private bool isBoss = false;

    [Header("Flocking")]
    [SerializeField] private float flockJoinRadius = 7f;
    [SerializeField] private float flockJoinInterval = 2f;
    [SerializeField] private float flockTimeout = 30f;

    private float crimeAmplifier = 1f;

    public Vector3 CurrentHeading { get; private set; } = Vector3.forward;
    public FlockGroup CurrentGroup { get; private set; }
    private float _flockJoinTimer;
    private float _flockTimeoutTimer;

    public VillainState State { get; private set; } = VillainState.Hidden;
    public bool isSupervillain { get; set; } = false;
    public bool IsRevealed { get; private set; } = false;

    private WanderBehaviour wanderBehaviour;
    private float _detectionTimer;
    private float _incentiveTimer;
    private float _crimeTimer;

    private Building _crimeTarget;
    private Transform _heroThreat;

    public bool isWeakVillain { get; set; } = false;

    protected override void Start()
    {
        base.Start();
        wanderBehaviour = gameObject.AddComponent<WanderBehaviour>();
        wanderBehaviour.speed = wanderSpeed;

        if (!overridePowerLvl)
            powerLvl = Random.Range(2f, 12f);

        if (isSupervillain)
        {
            IsRevealed = true;
            transform.localScale = Vector3.one * 1.8f;
            EnterWandering();
        } else
            EnterHidden();
    }

    protected override void Update()
    {
        base.Update();

        _detectionTimer -= Time.deltaTime;
        _incentiveTimer -= Time.deltaTime;
        _flockJoinTimer -= Time.deltaTime;

        if (_detectionTimer <= 0f)
        {
            _detectionTimer = detectionInterval;
            RunDetection();
        }

        if (_incentiveTimer <= 0f)
        {
            _incentiveTimer = incentiveEvaluationInterval;
            EvaluateIncentive();
        }

        if (_flockJoinTimer <= 0f && IsRevealed && (State == VillainState.Wandering || State == VillainState.Revealed))
        {
            _flockJoinTimer = flockJoinInterval;
            ScanForFlockmates();
        }

        if (State == VillainState.Flocking)
        {
            _flockTimeoutTimer -= Time.deltaTime;
            if (_flockTimeoutTimer <= 0f)
            {
                Debug.Log($"{name} flock timeout — returning to wandering");
                ExitFlock();
            }
        }

        HandleState();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    // --- Detection ---

    private void RunDetection()
    {
        if (State == VillainState.CommittingCrime) 
            return;

        Vector2 posi = new Vector2(transform.position.x, transform.position.z);
        List<Entity> groundZero = QTManager.instance.QueryRadius(posi, heroDetectionRadius);

        if (State == VillainState.Wandering || State == VillainState.Hidden)
        {
            Vector2 villainPosi = new Vector2(transform.position.x, transform.position.z);
            TryTargetCivilian(posi);
        }

        Hero closestHero = null;
        float closestDistance = float.MaxValue;

        foreach (Entity person in groundZero)
        {
            if (person is not Hero hero)
                continue;

            float distance = Vector3.Distance(transform.position, person.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHero = hero;
            }

            if (!IsRevealed && (State == VillainState.Wandering || State == VillainState.Hidden) && distance <= heroDetectionRadius)
            {
                Debug.Log($"Discovery roll on {name} — hero {hero.name} within range");
                if (Random.value < discoveryChance)
                {
                    Debug.Log($"{name} discovered by hero {hero.name}");
                    CrimeReport.instance?.LogEvent($"{name} discovered!");
                    entersRevealed(false);
                    return;
                }
            }
        }

        if (closestHero != null)
        {
            EvaluateHeroThreat(closestHero);
            return;
        }

        _heroThreat = null;
    }

    private void TryTargetCivilian(Vector2 pos)
    {
        if (Random.value > civilianTargetChance)
            return;

        List<Entity> groundZero = QTManager.instance.QueryRadius(pos, civilianTargetRadius);
        foreach (Entity person in groundZero)
        {
            if (person == null)
                continue;
            if (person is not Civilian civ)
                continue;
            if (civ.State == CivilianState.Dead || civ.State == CivilianState.Robbed || civ.State == CivilianState.Injured)
                continue;

            if (powerLvl < 4f)
                civ.applyOutcomeOfCrime(CivilianOutcome.Robbed);
            else if (powerLvl <= 8f)
                civ.applyOutcomeOfCrime(CivilianOutcome.Injured);
            else
                civ.applyOutcomeOfCrime(CivilianOutcome.Killed);

            //if (person is Civilian civ && civ.State != CivilianState.Dead)
            //{
            //    _civilianTarget = civ;
            //    civ.EnterVictimised();
            //    Debug.Log($"{name} targeted civilian {civ.name}");
            //    return;
            //}

            Debug.Log($"{name} (power:{powerLvl:F1}) {determineOutcome()} civilian {civ.name}");
            //CrimeReport.Instance?.LogEvent($"{name} {outcome} a civilian");
            return;
        }
    }

    private CivilianOutcome determineOutcome()
    {
        if (powerLvl < 4f)
            return CivilianOutcome.Robbed;
        if (powerLvl <= 8f)
            return CivilianOutcome.Injured;
        return CivilianOutcome.Killed;
    }

    private void EvaluateIncentive()
    {
        if (State == VillainState.CommittingCrime || State == VillainState.Fleeing)
            return;

        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        List<Entity> groundZero = QTManager.instance.QueryRadius(pos, buildingDetectionRadius);

        Building bestTarget = null;
        float bestScore = 0f;

        foreach (Entity person in groundZero)
        {
            if (person is not Building b || b.State != BuildingState.Intact) 
                continue;

            float score = EvaluateCrimeScore(b, pos, groundZero);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = b;
            }
        }

        float threshold = isSupervillain ? 0.1f : Mathf.Clamp(0.5f * (0.8f + MoraleManager.Instance.NormalisedMorale * 0.4f), 0.1f, 1.5f); // og 0.5f
        if (bestTarget == null || bestScore < threshold)
            return;

        if (State == VillainState.Flocking)
            broadcastFlockBreakup(bestTarget);
        else
        {
            //Villain conspirator = isSupervillain ? null : findPartnerInCrime(groundZero, bestTarget);

            Villain conspirator = findPartnerInCrime(groundZero, bestTarget);

            Debug.Log("Collaborating: " + (conspirator != null));
            if (conspirator != null)
            {
                enterJointCrime(conspirator, bestTarget, true);
            }
            else
                EnterIncentivised(bestTarget);
        }
    }

    private void broadcastFlockBreakup(Building crimeTarget)
    {
        if (CurrentGroup == null)
        {
            EnterIncentivised(crimeTarget);
            return;
        }

        int groupSize = CurrentGroup?.MemberCount ?? 1;

        crimeAmplifier = 1f + (groupSize - 1) * 0.5f;

        Debug.Log($"{name} triggering flock breakup — crime at {crimeTarget.name}");

        List<Villain> members = new List<Villain>(CurrentGroup.Members);

        foreach (Villain member in members)
        {
            if (member == null || member == this) continue;
            if (member.gameObject == null || !member.gameObject.activeInHierarchy) continue;

            if (member.powerLvl >= powerLvl * 0.75f)
            {
                Debug.Log($"{member.name} following instigator to crime");
                member.FollowToCrime(crimeTarget);
            }
            else
            {
                Debug.Log($"{member.name} scattering from flock");
                member.ExitFlock();
            }
        }

        CurrentGroup = null;
        EnterIncentivised(crimeTarget);
    }

    public void FollowToCrime(Building target)
    {
        ExitFlock();
        // Followers enter incentivised toward the same target
        // They'll arrive independently and commit amplified crime if they reach it
        EnterIncentivised(target);
        Debug.Log($"{name} following flock instigator to {target.name}");
    }

    private Villain findPartnerInCrime(List<Entity> groundZero, Building targetBuilding)
    {
        foreach (Entity person in groundZero)
        {
            if (person is not Villain other) 
                continue;
            if (other == this) 
                continue;
            if (other.State == VillainState.Collaborating) 
                continue;
            if (other.State == VillainState.CommittingCrime) 
                continue;
            Debug.Log("Collaborating state checked");

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist > collaborationRadius) 
                continue;
            Debug.Log($"Collaborating distance: {dist} vs radius: {collaborationRadius}");

            bool compatible = other._crimeTarget == null;
            Debug.Log($"Collaborating compatible: {compatible}");

            if (compatible)
                return other;
        }
        return null;
    }

    public void forcePartnersInCrime(Villain partner)
    {
        var buildings = FindObjectsByType<Building>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (buildings[0] != null)
            enterJointCrime(partner, buildings[0], true);
    }

    private void enterJointCrime(Villain partner, Building target, bool isLeader)
    {
        if (State == VillainState.Collaborating) 
            return;

        if (partner == null || gameObject == null)
        {
            Debug.Log($"Collaborating incentivised by partner: {partner == null} or gameObject: {gameObject == null}");
            EnterIncentivised(target);
            return;
        }

        if (partner.State == VillainState.Collaborating || partner.State == VillainState.CommittingCrime)
        {
            Debug.Log("Collaborating state not okay apparently");
            EnterIncentivised(target);
            return;
        }

        State = VillainState.Collaborating;
        partnerInCrime = partner;
        _crimeTarget = target;
        isBoss = isLeader;
        wanderBehaviour.enabled = false;

        if (isLeader)
            partner.enterJointCrime(this, target, false);

        Debug.Log($"{name} collaborating with {partner.name} on {target.buildingType}");
        UpdateVisuals();
    }

    private void enterJointCrime()
    {
        State = VillainState.CommittingCrime;
        _crimeTimer = crimeCommitDuration;

        if (partnerInCrime != null && isBoss)
            partnerInCrime.State = VillainState.CommittingCrime;

        UpdateVisuals();
    }

    private float EvaluateCrimeScore(Building building, Vector2 villainPos, List<Entity> groundZero)
    {
        float score = 0f;

        score += building.buildingType switch
        {
            BuildingType.Bank => 1.0f,
            BuildingType.JewelleryStore => 0.8f,
            BuildingType.PowerPlant => 0.9f,
            BuildingType.ResidentialBlock => 0.5f,
            _ => 0.5f
        };

        int civilianCount = 0;
        foreach (Entity person in groundZero)
            if (person is Civilian) 
                civilianCount++;
        score += civilianCount * 0.1f;

        foreach (Entity person in groundZero)
        {
            if (person is Hero)
            {
                score -= 0.8f; // strong deterrent
                break;
            }
        }

        float dist = Vector2.Distance(villainPos, new Vector2(building.transform.position.x, building.transform.position.z));
        score -= dist * 0.02f;

        Vector2 buildingPos = new Vector2(building.transform.position.x, building.transform.position.z);
        District d = DistrictManager.Instance?.GetDistrict(buildingPos);
        if (d != null)
            score *= 1f + (d.CrimeLevel / 100f);

        return score;
    }

    private void EvaluateHeroThreat(Hero hero)
    {
        float differential = CombatResolution.powerDifferential(hero.powerLvl, powerLvl);

        bool villainIsMuchStronger = differential > 0.4f;
        bool villainIsHighPower = powerLvl > 9f;

        if (villainIsHighPower)
        {
            Debug.Log($"{name} avoiding hero — reputation already high");
            _heroThreat = hero.transform;
            if (IsRevealed) 
                EnterFleeing();
        }
        else if (villainIsMuchStronger && IsRevealed)
        {
            _heroThreat = hero.transform;
            EnterSeekingEngagement(hero);
        }
        else if (IsRevealed)
        {
            _heroThreat = hero.transform;
            EnterFleeing();
        }
    }

    private void EnterSeekingEngagement(Hero hero)
    {
        State = VillainState.Wandering;
        wanderBehaviour.enabled = false;
        _heroThreat = hero.transform;
        Debug.Log($"{name} seeking weak hero {hero.name} " +
                  $"(villain power:{powerLvl:F1}, hero power:{hero.powerLvl:F1})");
        UpdateVisuals();
    }

    private void HandleState()
    {
        switch (State)
        {
            case VillainState.Incentivised:
                if (_crimeTarget != null)
                    MoveTowards(_crimeTarget.transform.position, crimeSpeed);

                float distance = _crimeTarget != null ? Vector3.Distance(transform.position, _crimeTarget.transform.position) : float.MaxValue;

                if (distance < 2f)
                    EnterCommittingCrime();
                else if (isSupervillain && distance < 4f)
                    EnterCommittingCrime();
                    break;

            case VillainState.CommittingCrime:
                _crimeTimer -= Time.deltaTime;
                if (_crimeTimer <= 0f)
                    getAwayWithIt();
                break;

            case VillainState.Fleeing:
                if (_heroThreat != null && !isSupervillain)
                    FleeFrom(_heroThreat.position);
                else
                    EnterWandering();
                break;
            case VillainState.Wandering:
                if (_heroThreat != null && !IsRevealed)
                {
                    MoveTowards(_heroThreat.position, crimeSpeed);
                    float distance2 = Vector3.Distance(transform.position, _heroThreat.position);
                    if (distance2 < 2f && _heroThreat.TryGetComponent(out Hero target))
                        CombatManager.instance.InitiateCombat(target, this);
                }
                break;
            case VillainState.Collaborating:
                //if (partnerInCrime == null || _crimeTarget == null)
                //{
                //    if (_crimeTarget != null)
                //        EnterIncentivised(_crimeTarget);
                //    else
                //        EnterWandering();
                //    break;
                //}

                if (partnerInCrime == null || partnerInCrime.gameObject == null || !partnerInCrime.gameObject.activeInHierarchy)
                {
                    partnerInCrime = null;
                    if (_crimeTarget != null && _crimeTarget.State != BuildingState.Intact)
                        EnterIncentivised(_crimeTarget);
                    else
                        EnterWandering();
                    break;
                }

                if (_crimeTarget == null || _crimeTarget.State == BuildingState.Intact)
                {
                    partnerInCrime = null;
                    EnterWandering();
                    break;
                }

                if (isBoss)
                {
                    MoveTowards(_crimeTarget.transform.position, crimeSpeed);
                    float distance2 = Vector3.Distance(transform.position, _crimeTarget.transform.position);
                    if (distance2 < 2f)
                        enterJointCrime();
                }
                else
                {
                    MoveTowards(partnerInCrime.transform.position, crimeSpeed);
                }
                break;
            case VillainState.Flocking:
                if (CurrentGroup == null || CurrentGroup.MemberCount <= 1)
                {
                    ExitFlock();
                    break;
                }
                ApplyReynoldsSteering();
                break;
        }
    }

    private void getAwayWithIt()
    {
        if (_crimeTarget != null)
        {
            float damage = buildingDamageAmount * (crimeDamageAmplifier != null ? buildingDamageAmount : 1f) * crimeAmplifier;

            _crimeTarget.TakeDamage(damage);
            crimeAmplifier = 1f;

            Vector2 crimePos = new Vector2(_crimeTarget.transform.position.x, _crimeTarget.transform.position.z);
            DistrictManager.Instance?.RaiseCrime(crimePos, 5f);

            Debug.Log($"{name} committed {_crimeTarget.IncentivisedCrime} " +  $"at {_crimeTarget.name} (damage:{damage:F0})");
            CrimeReport.instance?.LogEvent($"{name} committed a crime at {_crimeTarget.name}");
        }

        partnerInCrime = null;
        entersRevealed(true);
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
        Vector3 newPosi = transform.position + dir * fleeSpeed * Time.deltaTime;
        newPosi.x = Mathf.Clamp(newPosi.x, -45f, 45f);
        newPosi.y = 0f;
        newPosi.z = Mathf.Clamp(newPosi.z, -45f, 45f);
        transform.position = newPosi;
    }

    private void EnterHidden()
    {
        State = VillainState.Hidden;
        wanderBehaviour.enabled = true;
        UpdateVisuals();
    }

    private void EnterWandering()
    {
        State = VillainState.Wandering;
        wanderBehaviour.enabled = true;
        UpdateVisuals();
    }

    private void EnterIncentivised(Building target)
    {
        if (State == VillainState.Incentivised) 
            return;
        State = VillainState.Incentivised;
        _crimeTarget = target;
        wanderBehaviour.enabled = false;
        UpdateVisuals();
        Debug.Log($"{name} incentivised by {target.buildingType} at {target.name}");
    }

    private void EnterCommittingCrime()
    {
        State = VillainState.CommittingCrime;
        wanderBehaviour.enabled = false;
        _crimeTimer = crimeCommitDuration;
        UpdateVisuals();
        Debug.Log($"{name} committing crime at {_crimeTarget?.name}");
    }

    private void EnterFleeing()
    {
        if (State == VillainState.Fleeing) return;
        State = VillainState.Fleeing;
        wanderBehaviour.enabled = false;
        UpdateVisuals();
    }

    private void entersRevealed(bool byCrime)
    {
        State = VillainState.Revealed;
        IsRevealed = true;
        wanderBehaviour.enabled = true;
        _incentiveTimer = isSupervillain ? 0f : incentiveEvaluationInterval;
        _crimeTarget = null;
        UpdateVisuals();
        Debug.Log($"{name} revealed after committing crime");
        CrimeReport.instance?.LogEvent($"{name} revealed");
    }

    public void forceReveal()
    {
        entersRevealed(false);
    }

    public void setPowerLevel(float lvl)
    {
        powerLvl = lvl;
        overridePowerLvl = true;
    }

    private void ScanForFlockmates()
    {
        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        List<Entity> nearby = QTManager.instance.QueryRadius(pos, flockJoinRadius);

        foreach (Entity e in nearby)
        {
            if (e == null) continue;
            if (e is not Villain other) continue;
            if (other == this) continue;
            if (!other.IsRevealed) continue;
            if (other.isSupervillain) continue;
            if (other.State == VillainState.CommittingCrime) continue;
            if (other.State == VillainState.Collaborating) continue;

            if (CurrentGroup == null && other.CurrentGroup == null)
            {
                FlockGroup group = VillainFlocker.Instance.CreateGroup(this);
                EnterFlocking(group);
                VillainFlocker.Instance.JoinGroup(group, other);
                other.EnterFlocking(group);
            }
            else if (CurrentGroup == null && other.CurrentGroup != null)
            {
                VillainFlocker.Instance.JoinGroup(other.CurrentGroup, this);
                EnterFlocking(other.CurrentGroup);
            }
            else if (CurrentGroup != null && other.CurrentGroup == null)
            {
                VillainFlocker.Instance.JoinGroup(CurrentGroup, other);
                other.EnterFlocking(CurrentGroup);
            }
            return;
        }
    }

    public void EnterFlocking(FlockGroup group)
    {
        if (State == VillainState.Flocking) 
            return;
        State = VillainState.Flocking;
        CurrentGroup = group;
        _flockTimeoutTimer = flockTimeout;
        wanderBehaviour.enabled = false;
        UpdateVisuals();
        Debug.Log($"{name} entered flocking state");
    }

    public void ExitFlock()
    {
        if (CurrentGroup != null)
        {
            VillainFlocker.Instance.RemoveMember(CurrentGroup, this);
            CurrentGroup = null;
        }
        State = VillainState.Wandering;
        wanderBehaviour.enabled = true;
        UpdateVisuals();
    }

    [Header("Reynolds Weights")]
    [SerializeField] private float separationWeight_flock = 0.7f;
    [SerializeField] private float alignmentWeight_flock = 0.3f;
    [SerializeField] private float cohesionWeight_flock = 0.25f;
    [SerializeField] private float separationRadius_flock = 2.0f;
    [SerializeField] private float flockMoveSpeed = 2.0f;

    private void ApplyReynoldsSteering()
    {
        if (CurrentGroup == null)
        {
            ExitFlock();
            return;
        }
        if (CurrentGroup.MemberCount <= 1)
        {
            ExitFlock();
            return;
        }

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int count = 0;

        foreach (Villain member in CurrentGroup.Members)
        {
            if (member == null || member == this) continue;

            float dist = Vector3.Distance(transform.position, member.transform.position);
            count++;

            if (dist < separationRadius_flock && dist > 0.01f)
            {
                Vector3 away = (transform.position - member.transform.position).normalized;
                separation += away / dist;
            }

            alignment += member.CurrentHeading;

            cohesion += member.transform.position;
        }

        if (count == 0) return;

        alignment = (alignment / count).normalized;
        cohesion = ((cohesion / count) - transform.position).normalized;
        if (separation != Vector3.zero) separation = separation.normalized;

        Villain strongest = CurrentGroup.StrongestMember();
        Vector3 leaderBias = strongest != null && strongest != this
            ? (strongest.transform.position - transform.position).normalized * 0.3f
            : Vector3.zero;

        Vector3 steering = separation * separationWeight_flock
                         + alignment * alignmentWeight_flock
                         + cohesion * cohesionWeight_flock
                         + leaderBias;

        steering = steering.normalized;

        Vector3 newPos = transform.position + steering * flockMoveSpeed * Time.deltaTime;
        newPos.x = Mathf.Clamp(newPos.x, -44f, 44f);
        newPos.y = 0f;
        newPos.z = Mathf.Clamp(newPos.z, -44f, 44f);
        transform.position = newPos;

        CurrentHeading = steering;
    }

    private void UpdateVisuals()
    {
        Color target;

        if (isSupervillain && (State == VillainState.Revealed || IsRevealed))
            target = new Color(112f, 0f, 0f);
        else
        {
            target = State switch
            {
                VillainState.Hidden => Color.white,
                VillainState.Wandering => Color.white,
                VillainState.Incentivised => Color.white,
                VillainState.CommittingCrime => Color.white,
                VillainState.Fleeing => IsRevealed ? (isWeakVillain ? new Color(1f, 0.4f, 0.7f) : Color.red) : Color.white,
                VillainState.Revealed => Color.red,
                VillainState.Collaborating => new Color(0.8f, 0.4f, 0f),
                VillainState.Flocking => new Color(0.5f, 0f, 0.5f), // dark purple
                _ => Color.white
            };
        }

        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            var mat = r.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", target);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", target);
        }
    }
}

public enum VillainState
{
    Hidden,
    Wandering,
    Incentivised,
    CommittingCrime,
    Fleeing,
    Revealed,
    Collaborating,
    Flocking
}
