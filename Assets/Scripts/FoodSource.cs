using UnityEngine;

public class FoodSource : MonoBehaviour, IQuadTreeEntity
{
    public int amount = 100;
    public Vector2 Position => transform.position;
    public QTManager manager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
