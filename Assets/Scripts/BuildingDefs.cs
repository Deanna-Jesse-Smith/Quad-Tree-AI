using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingDefs
{

}
public class Building : Entity
{
    [Header("Building Config")]
    [SerializeField] public BuildingType buildingType;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float rebuildDelay = 10f;  // seconds before rebuild starts
    [SerializeField] private float rebuildDuration = 15f; // seconds to fully rebuild

    public BuildingState State { get; private set; } = BuildingState.Intact;
    public float health { get; private set; }
    public bool isBeingAided { get; private set; } = false;
    public CrimeType IncentivisedCrime => BuildingCrimeMap.Incentives[buildingType];

    private float rebuildTimer = 0f;

    protected override void Start()
    {
        base.Start();
        health = maxHealth;
        UpdateVisuals();
    }

    protected override void Update()
    {
        HandleStateTimer();

        Mouse mouse = Mouse.current;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            TakeDamage(maxHealth);
        }
    }

    //public void OnMouseDown()
    //{
    //    TakeDamage(maxHealth);
    //}

    private void HandleStateTimer()
    {
        switch (State)
        {
            case BuildingState.Destroyed:
                rebuildTimer -= Time.deltaTime;
                if (rebuildTimer <= 0f)
                {
                    State = BuildingState.Rebuilding;
                    rebuildTimer = rebuildDuration;
                    UpdateVisuals();
                }
                break;

            case BuildingState.Rebuilding:
                rebuildTimer -= Time.deltaTime;
                if (!isBeingAided)
                    health = Mathf.Lerp(0f, maxHealth, 1f - (rebuildTimer / rebuildDuration));
                if (health >= maxHealth || rebuildTimer <= 0f)
                    EnterIntact();
                break;
        }
    }

    public void TakeDamage(float amount)
    {
        if (State != BuildingState.Intact) 
            return;

        health -= amount;
        UpdateVisuals();
        if (health <= 0f)
        {
            health = 0f;
            State = BuildingState.Destroyed;
            rebuildTimer = rebuildDelay;
            MoraleManager.Instance.OnBuildingDestroyed(buildingType);
            DistrictManager.Instance.RaiseCrime(new Vector2(transform.position.x, transform.position.z), 10f);
            //QTManager.instance.Deregister(this, WorldToTreePosition());
            UpdateVisuals();
        }
    }

    private void enterDestroyed()
    {
        health = 0f;
        State = BuildingState.Destroyed;
        rebuildTimer = rebuildDelay;
        MoraleManager.Instance.OnBuildingDestroyed(buildingType);
        DistrictManager.Instance.RaiseCrime(new Vector2(transform.position.x, transform.position.z), 10f);
        //QTManager.instance.Deregister(this, WorldToTreePosition());
        UpdateVisuals();
    }

    private void EnterRebuilding()
    {
        State = BuildingState.Rebuilding;
        rebuildTimer = rebuildDuration;
        UpdateVisuals();
    }

    private void EnterIntact()
    {
        health = maxHealth;
        State = BuildingState.Intact;
        rebuildTimer = 0f;
        isBeingAided = false;
        DistrictManager.Instance.ReduceCrime(new Vector2(transform.position.x, transform.position.z), 8f);
        //QTManager.instance.Register(this, WorldToTreePosition());
        UpdateVisuals();
    }

    public void setAided(bool setAid)
    {
        Debug.Log($"{name} isBeingAide");
        isBeingAided = setAid;
    }

    private void UpdateVisuals()
    {
        Color target;

        switch (State)
        {
            case BuildingState.Intact:
                float hp = health / maxHealth;
                target = Color.Lerp(new Color(0.1f, 0.1f, 0.5f), Color.blue, hp);
                break;
            case BuildingState.Destroyed:
                target = Color.black;
                break;
            case BuildingState.Rebuilding:
                float rp = health / maxHealth;
                target = Color.Lerp(Color.black, Color.yellow, rp);
                break;
            default:
                target = Color.blue;
                break;
        }

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
    public void getBoost(float amount)
    {
        if (State != BuildingState.Rebuilding && State != BuildingState.Destroyed) 
            return;

        health = Mathf.Min(health + amount, maxHealth);
        UpdateVisuals();
        if (health >= maxHealth)
            EnterIntact();
    }
}

public static class BuildingCrimeMap
{
    public static readonly Dictionary<BuildingType, CrimeType> Incentives
        = new Dictionary<BuildingType, CrimeType>
    {
        { BuildingType.Bank,            CrimeType.Robbery  },
        { BuildingType.JewelleryStore,  CrimeType.Theft    },
        { BuildingType.PowerPlant,      CrimeType.Sabotage },
        { BuildingType.ResidentialBlock,CrimeType.Assault  }
    };
}

public enum BuildingType
{
    Bank,
    JewelleryStore,
    PowerPlant,
    ResidentialBlock
}

public enum CrimeType
{
    Robbery,
    Theft,
    Sabotage,
    Assault
}

public enum BuildingState
{
    Intact,
    Destroyed,
    Rebuilding
}
