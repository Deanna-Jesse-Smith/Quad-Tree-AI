using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VillainFlocker : MonoBehaviour
{
    public static VillainFlocker Instance { get; private set; }

    [SerializeField] private List<FlockGroup> _groups = new List<FlockGroup>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public FlockGroup CreateGroup(Villain founder)
    {
        FlockGroup group = new FlockGroup();
        group.AddMember(founder);
        _groups.Add(group);
        Debug.Log($"Flock group created by {founder.name}");
        return group;
    }

    public void JoinGroup(FlockGroup group, Villain villain)
    {
        group.AddMember(villain);
        Debug.Log($"{villain.name} joined flock group (size:{group.MemberCount})");
    }

    public void RemoveMember(FlockGroup group, Villain villain)
    {
        if (group == null)
            return;
        group.RemoveMember(villain);
        if (group.MemberCount <= 1)
        {
            group.Disband();
            _groups.Remove(group);
            Debug.Log("Flock group disbanded — too few members");
        }
    }

    public void CleanUp()
    {
        _groups.RemoveAll(g => g.MemberCount == 0);
    }
}

public class FlockGroup
{
    private List<Villain> _members = new List<Villain>();

    public int MemberCount => _members.Count;
    public IReadOnlyList<Villain> Members => _members;

    public void AddMember(Villain v)
    {
        if (!_members.Contains(v))
            _members.Add(v);
    }

    public void RemoveMember(Villain v) => _members.Remove(v);

    public void Disband()
    {
        foreach (Villain v in _members.ToList())
        {
            if (v != null && v.gameObject != null)
                v.ExitFlock();
        }
        _members.Clear();
    }

    public Vector3 AverageHeading()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Villain v in _members)
        {
            if (v == null) continue;
            sum += v.CurrentHeading;
            count++;
        }
        return count > 0 ? (sum / count).normalized : Vector3.forward;
    }

    public Vector3 AveragePosition()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Villain v in _members)
        {
            if (v == null) continue;
            sum += v.transform.position;
            count++;
        }
        return count > 0 ? sum / count : Vector3.zero;
    }

    public Villain StrongestMember()
    {
        Villain strongest = null;
        float maxPower = 0f;
        foreach (Villain v in _members)
        {
            if (v == null) continue;
            if (v.powerLvl > maxPower)
            {
                maxPower = v.powerLvl;
                strongest = v;
            }
        }
        return strongest;
    }
}
