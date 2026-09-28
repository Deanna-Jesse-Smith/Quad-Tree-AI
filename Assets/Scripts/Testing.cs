using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Testing : MonoBehaviour
{
    public QTManager qtManager;
    public QuadTree tree;
    private bool _drawEnabled = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //QTManager.instance.QueryRadius(transform.position, 5f);
        //Debug.Log(QTManager.instance.QueryRadius(transform.position, 5f).Count);

        //var tree = new QuadTree(WorldBounds.bounds);

        //// Insert 50 entities very close together — forces deep recursion
        //int count = 50;
        //for (int i = 0; i < count; i++)
        //    tree.insert(new MockEntity(new Vector2(
        //        Random.Range(-0.5f, 0.5f),
        //        Random.Range(-0.5f, 0.5f))));

        //// Query at the cluster centre — should get all 50 back
        //var results = new List<IQuadTreeEntity>();
        //tree.queryRadius(Vector2.zero, 2f, results);

        //Debug.Assert(results.Count == co saefwsunt,
        //    $"FAIL: inserted {count}, queryRadius returned {results.Count}");

        //Debug.Log($"Test 9 PASS: deep subdivision handles {count} clustered entities correctly.");

        //tree = new QuadTree(WorldBounds.bounds);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDrawGizmos()
    {
        if (!_drawEnabled || tree == null)
            return;
        DrawNode(tree);
    }

    private void DrawNode(QuadTree node)
    {
        if (node == null) return;
        Gizmos.color = Color.green;
        // Draw the boundary rect. Assuming node.Bounds is a Rect:
        Vector3 center = new Vector3(node.bounds.center.x, 0, node.bounds.center.y);
        Vector3 size = new Vector3(node.bounds.width, 0, node.bounds.height);
        Gizmos.DrawWireCube(center, size);

        if (node.isSubdivided)
        {
            DrawNode(node.NE);
            DrawNode(node.NW);
            DrawNode(node.SE);
            DrawNode(node.SW);
        }
    }

    public class MockEntity : IQuadTreeEntity
    {
        public Vector2 Position { get; }
        public MockEntity(Vector2 pos) => Position = pos;
    }
}
