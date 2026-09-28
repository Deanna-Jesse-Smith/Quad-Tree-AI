using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CivilianFlocker : MonoBehaviour
{
    public static CivilianFlocker instance { get; private set; }

    private List<CivilianFlockGroup> crowds = new List<CivilianFlockGroup>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Awake()
    {
        if (instance != null && instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        instance = this;
    }

    public CivilianFlockGroup createCrowd(Civilian trendSetter)
    {
        CivilianFlockGroup newCrowd = new CivilianFlockGroup();
        newCrowd.addMember(trendSetter);
        crowds.Add(newCrowd);
        Debug.Log("Crowd created with trendsetter: " + trendSetter.name);
        return newCrowd;
    }

    public void joinCrowd(CivilianFlockGroup crowd, Civilian civ)
    {
        crowd.addMember(civ);
    }

    public void leaveCrowd(CivilianFlockGroup crowd, Civilian civ)
    {
        crowd.removeMember(civ);
        if (crowd.crowdCount <= 1)
        {
            Debug.Log("Crowd dissipated");
            crowd.disband();
            crowds.Remove(crowd);
        }
    }
}

public class CivilianFlockGroup
{
    private List<Civilian> crowd = new List<Civilian>();
    public int crowdCount => crowd.Count;
    public IReadOnlyList<Civilian> crowds => crowd;

    public Transform SharedThreat { get; set; }

    public Vector3 SharedDestination { get; set; }

    public void addMember(Civilian c)
    {
        if (!crowd.Contains(c)) 
            crowd.Add(c);
    }

    public void removeMember(Civilian c) => crowd.Remove(c);

    public void disband()
    {
        foreach (Civilian civ in crowd.ToList())
            if (civ != null && civ.gameObject != null)
                civ.leaveCrowd();
        crowd.Clear();
    }

    public Vector3 averagePosition()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Civilian civ in crowd)
        {
            if (civ == null) 
                continue;
            sum += civ.transform.position;
            count++;
        }
        return count > 0 ? sum / count : Vector3.zero;
    }

    public void broadcastThreat(Transform threat)
    {
        SharedThreat = threat;
        foreach (Civilian civ in crowd.ToList())
        {
            if (civ == null || civ.gameObject == null) 
                continue;
            civ.onGroupThreatDetected(threat);
        }
    }

    public void broadcastDestination(Vector3 destination)
    {
        SharedDestination = destination;
        foreach (Civilian civ in crowd.ToList())
        {
            if (civ == null || civ.gameObject == null) 
                continue;
            civ.onGroupDestinationSet(destination);
        }
    }
}
