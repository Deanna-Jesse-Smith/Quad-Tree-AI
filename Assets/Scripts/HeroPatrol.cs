using System.Collections.Generic;
using UnityEngine;

public class HeroPatrol : MonoBehaviour
{
    [SerializeField] private int waypointNum = 6;
    [SerializeField] private float patrolRange = 40f;
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float arrivalRadius = 1.5f;

    private Vector3[] waypoints;
    private int curWaypoint = 0;
    private bool isEnabled = true;

    [SerializeField] private float heroSeparationRadius = 8f;
    [SerializeField] private float heroSeparationWeight = 0.4f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generateWaypoints();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isEnabled)
            return;

        moveTowardsWaypoint();
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            isEnabled = value;
            if (value)
            {
                generateWaypoints(); // from District stuff
                curWaypoint = getNearestWaypointIndex();
            }
        }
    }

    private void generateWaypoints()
    {
        waypoints = new Vector3[waypointNum];
        for (int loop = 0; loop < waypointNum; loop++)
        {
            //waypoints[loop] = new Vector3(Random.Range(-patrolRange, patrolRange), 0f, Random.Range(-patrolRange, patrolRange));
            waypoints[loop] = generateBiasedWaypoint(loop);
        }
        curWaypoint = getNearestWaypointIndex();
    }

    private void moveTowardsWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) 
            return;

        float moraleMultiplier = MoraleManager.Instance != null ? 0.8f + MoraleManager.Instance.NormalisedMorale * 0.4f : 1f;

        float currentSpeed = patrolSpeed * moraleMultiplier;

        Vector3 target = waypoints[curWaypoint];
        transform.position = Vector3.MoveTowards(transform.position, target, patrolSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < arrivalRadius)
            curWaypoint = (curWaypoint + 1) % waypoints.Length;
    }

    private int getNearestWaypointIndex()
    {
        if (waypoints == null || waypoints.Length == 0) return 0;

        int nearest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < waypoints.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, waypoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }
        return nearest;
    }

    private Vector3 generateBiasedWaypoint(int index)
    {
        Vector3 baseWaypoint;

        if (index % 2 == 0 && DistrictManager.Instance != null)
        {
            District hotspot = DistrictManager.Instance.GetHighestCrimeDistrict();

            if (hotspot.CrimeLevel > 10f)
            {
                return new Vector3(
                    hotspot.Bounds.center.x + Random.Range(-10f, 10f),
                    0f,
                    hotspot.Bounds.center.y + Random.Range(-10f, 10f)
                );
            } else
            {
                baseWaypoint = new Vector3(Random.Range(-patrolRange, patrolRange), 0f, Random.Range(-patrolRange, patrolRange));
            }
        } else
        {
            baseWaypoint = new Vector3(Random.Range(-patrolRange, patrolRange), 0f, Random.Range(-patrolRange, patrolRange));
        }

        return keepSoloPatrol(baseWaypoint);

        //return new Vector3(Random.Range(-patrolRange, patrolRange), 0f, Random.Range(-patrolRange, patrolRange));
    }

    private Vector3 keepSoloPatrol(Vector3 proposedWaypoint)
    {
        if (QTManager.instance == null) 
            return proposedWaypoint;

        Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
        List<Entity> groundZero = QTManager.instance.QueryRadius(pos2D, heroSeparationRadius);

        Vector3 separationSum = Vector3.zero;
        int count = 0;

        foreach (Entity pointOfInterest in groundZero)
        {
            if (pointOfInterest == null) 
                continue;
            if (pointOfInterest is not Hero) 
                continue;
            if (pointOfInterest.transform == transform) 
                continue;

            Vector3 away = (transform.position - pointOfInterest.transform.position).normalized;
            separationSum += away;
            count++;
        }

        if (count == 0) 
            return proposedWaypoint;

        Vector3 separationDir = separationSum.normalized;
        Vector3 biasedWaypoint = proposedWaypoint + separationDir * heroSeparationWeight * patrolRange;

        biasedWaypoint.x = Mathf.Clamp(biasedWaypoint.x, -patrolRange, patrolRange);
        biasedWaypoint.y = 0f;
        biasedWaypoint.z = Mathf.Clamp(biasedWaypoint.z, -patrolRange, patrolRange);

        return biasedWaypoint;
    }
}
