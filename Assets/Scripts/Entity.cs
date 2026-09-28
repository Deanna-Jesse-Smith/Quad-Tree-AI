using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class Entity : MonoBehaviour, IQuadTreeEntity
{
    public Vector2 Position => new Vector2(transform.position.x, transform.position.z);
    private Vector2 registeredPosi;
    private bool isRegistered = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        RegisterWithTree();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        updateRegistration();
    }

    protected virtual void OnDestroy()
    {
        if (isRegistered && QTManager.instance != null)
            QTManager.instance.Deregister(this, registeredPosi);
    }

    private void RegisterWithTree()
    {
        if (QTManager.instance == null)
        {
            return;
        }
        registeredPosi = worldToTreePosition();
        QTManager.instance.Register(this, registeredPosi);
        isRegistered = true;
    }

    protected void updateRegistration()
    {
        if (!isRegistered || QTManager.instance == null) 
            return;

        Vector2 current = worldToTreePosition();
        if (Vector2.Distance(current, registeredPosi) > 0.5f)
        {
            QTManager.instance.Deregister(this, registeredPosi);
            registeredPosi = current;
            QTManager.instance.Register(this, registeredPosi);
        }
    }

    public void die()
    {
        if (isRegistered && QTManager.instance != null)
        {
            QTManager.instance.Deregister(this, registeredPosi);
            isRegistered = false;
        }
        Destroy(gameObject);
    }

    protected Vector2 worldToTreePosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }
}
