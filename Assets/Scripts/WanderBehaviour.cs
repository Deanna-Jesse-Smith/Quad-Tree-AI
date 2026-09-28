using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class WanderBehaviour : MonoBehaviour
{
    public float speed = 2f;
    public float wanderRadius = 3f;  // og 5
    public float arrivalThreshold = 0.3f;
    private float spawnRange = 40f;

    private Vector3 target;

    [Header("crowding")]
    [SerializeField] private float cohesionRadius = 6f;
    [SerializeField] private float separationRadius = 1f;
    [SerializeField] private float cohesionWeight = 0.4f;
    [SerializeField] private float separationWeight = 0.3f;
    [SerializeField] private bool crowdingEnabled = true;

    private float crowdingQueryInterval = 0.2f;
    private float crowdingTimer = 0f;
    private Vector3 cohesionForce = Vector3.zero;
    private Vector3 separationForce = Vector3.zero;

    [Header("Flocking")]
    [SerializeField] private float flockJoinRadius = 7f;
    [SerializeField] private float flockJoinInterval = 2f;
    [SerializeField] private float flockTimeout = 30f;

    public Vector3 CurrentHeading { get; private set; } = Vector3.forward;
    public FlockGroup CurrentGroup { get; private set; }
    private float _flockJoinTimer;
    private float _flockTimeoutTimer;

    void Start()
    {
        PickNewTarget();
    }

    void Update()
    {
        if (!crowdingEnabled)
        {
            moveNormal();
            return;
        }

        crowdingTimer -= Time.deltaTime;
        if (crowdingTimer < 0f)
        {
            crowdingTimer = crowdingQueryInterval;
            updateCrowding();
        }

        moveWithCrowd();


        if (Vector3.Distance(transform.position, target) < arrivalThreshold)
            PickNewTarget();

        //Vector3 flatTarget = new Vector3(target.x, 0f, target.z); // belt and braces
        //transform.position = Vector3.MoveTowards(transform.position, flatTarget, speed * Time.deltaTime);

        //if (Vector3.Distance(transform.position, flatTarget) < arrivalThreshold)
        //    PickNewTarget();
    }

    void PickNewTarget()
    {
        float morale = MoraleManager.Instance?.NormalisedMorale ?? 0.5f;
        float effectiveRadius = wanderRadius * Mathf.Lerp(0.4f, 1.2f, morale);

        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        target = new Vector3(Mathf.Clamp(transform.position.x + offset.x, -spawnRange, spawnRange), 0f, Mathf.Clamp(transform.position.z + offset.y, -spawnRange, spawnRange));
    }

    private void moveNormal()
    {
        Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, flatTarget) < arrivalThreshold)
            PickNewTarget();
    }

    private void updateCrowding()
    {
        if (QTManager.instance == null)
            return;

        Vector2 posi2D = new Vector2(transform.position.x, transform.position.z);
        List<Entity> groundZero = QTManager.instance.QueryRadius(posi2D, cohesionRadius);

        Vector3 cohesionSum = Vector3.zero;
        Vector3 separationSum = Vector3.zero;
        int cohesionCount = 0;

        foreach (Entity person in groundZero)
        {
            if (person == null) 
                continue;
            if (person is not Civilian) 
                continue;
            if (person.transform == transform) 
                continue;

            float dist = Vector3.Distance(transform.position, person.transform.position);
            if (dist < 0.01f)
                continue;

            if (dist > separationRadius)
            {
                float falloff = 1f - (dist / cohesionRadius);
                cohesionSum += (person.transform.position - transform.position).normalized * falloff;
                cohesionCount++;
            }

            if (dist < separationRadius)
            {
                Vector3 away = (transform.position - person.transform.position).normalized;
                separationSum += away * (separationRadius / dist);
            }
        }

        cohesionForce = cohesionCount > 0 ? (cohesionSum / cohesionCount).normalized : Vector3.zero;

        separationForce = separationSum == Vector3.zero ? Vector3.zero : separationSum.normalized;
    }

    private void moveWithCrowd()
    {
        Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
        Vector3 wanderDirection = (flatTarget - transform.position).normalized;

        Vector3 blended = wanderDirection * 1f + cohesionForce * cohesionWeight + separationForce * separationWeight;

        blended = blended.normalized;

        Vector3 newPosi = transform.position + blended * speed * Time.deltaTime;
        newPosi.x = Mathf.Clamp(newPosi.x, -44f, 44f);
        newPosi.y = 0f;
        newPosi.z = Mathf.Clamp(newPosi.z, -44f, 44f);
        transform.position = newPosi;

        if (Vector3.Distance(transform.position, new Vector3(target.x, 0f, target.z)) < arrivalThreshold)
            PickNewTarget();
    }

    public void enterScatter()
    {
        cohesionWeight = 0f;
        separationWeight = 2f;
        cohesionForce = Vector3.zero;
    }

    public void exitScatter()
    {
        cohesionWeight = 0f;
        separationWeight = 0f;
    }
}
