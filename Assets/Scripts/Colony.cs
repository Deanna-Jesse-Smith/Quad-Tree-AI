using UnityEngine;

public class Colony : MonoBehaviour
{
    public Vector2 Position => transform.position;
    public int colonyID;

    [SerializeField] private GameObject antPrefab;
    [SerializeField] private int initCount = 10;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < initCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            var ant = Instantiate(antPrefab, (Vector2)transform.position + offset, Quaternion.identity);
            ant.GetComponent<Ant>().colonyID = colonyID;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
