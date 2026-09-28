using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class QuadTree
{
    private const int maxEntities = 4, maxDepth = 8;
    public Rect bounds;
    public int depth { get; private set; }
    private List<Entity> entities = new List<Entity>();
    public QuadTree[] children; // NW, NE, SW, SE — null if leaf
    public bool isSubdivided => NE != null;

    public QuadTree NE { get; private set; }
    public QuadTree NW { get; private set; }
    public QuadTree SE { get; private set; }
    public QuadTree SW { get; private set; }

    public QuadTree(Rect boundary, int dep = 0)
    {
        bounds = boundary;
        depth = dep;
    }

    public bool insert(Entity entity, Vector2 posi)
    {
        if (entities == null)
            return false;

        if (!containsPoint(bounds, posi))
            return false;

        if (isSubdivided)
            return insertIntoChildren(entity, posi);

        entities.Add(entity);
        //Debug.Log($"Node at depth {depth} now has {entities.Count}/{maxEntities} entities");

        if (entities.Count > maxEntities && depth < maxDepth)
            subdivide();

        return true;
    }

    private bool containsPoint(Rect rect, Vector2 point)
    {
        return point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax;
    }

    private bool insertIntoChildren(Entity entity, Vector2 posi)
    {
        if (NE.insert(entity, posi)) 
            return true;
        if (NW.insert(entity, posi)) 
            return true;
        if (SE.insert(entity, posi)) 
            return true;
        if (SW.insert(entity, posi)) 
            return true;

        QuadTree closest = closestChild(posi);
        closest.entities.Add(entity);
        Debug.LogWarning($"Fallback insert for {entity?.name} at {posi}");
        return true;
    }

    private QuadTree closestChild(Vector2 posi)
    {
        QuadTree best = NE;
        float bestDist = Vector2.Distance(posi, NE.bounds.center);
        float distance = Vector2.Distance(posi, NW.bounds.center);
        if (distance < bestDist) 
        { 
            bestDist = distance; 
            best = NW; 
        }

        distance = Vector2.Distance(posi, SE.bounds.center);
        if (distance < bestDist) 
        { 
            bestDist = distance; 
            best = SE; 
        }

        distance = Vector2.Distance(posi, SW.bounds.center);
        if (distance < bestDist) 
            best = SW; 

        return best;
    }

    public bool remove(Entity entity, Vector2 position)
    {
        if (!bounds.Contains(position))
            return false;

        if (isSubdivided)
        {
            bool removed = NE.remove(entity, position) || NW.remove(entity, position) || SE.remove(entity, position) || SW.remove(entity, position);

            if (removed)
                tryMerge();

            return removed;
        }

        return entities.Remove(entity);
    }

    private void tryMerge()
    {
        if (!isSubdivided) return;

        int total = NE.EntityCount() + NW.EntityCount()
                  + SE.EntityCount() + SW.EntityCount();

        if (total > maxEntities) 
            return;

        if (NE.isSubdivided || NW.isSubdivided
         || SE.isSubdivided || SW.isSubdivided) 
            return;

        entities.AddRange(NE.entities);
        entities.AddRange(NW.entities);
        entities.AddRange(SE.entities);
        entities.AddRange(SW.entities);

        NE = NW = SE = SW = null;
    }

    private int EntityCount()
    {
        if (!isSubdivided) 
            return entities.Count;
        return NE.EntityCount() + NW.EntityCount() + SE.EntityCount() + SW.EntityCount();
    }

    public void queryRadius(Vector2 center, float radius, List<Entity> results)
    {
        if (!overlapsCircle(bounds, center, radius))
            return;

        if (isSubdivided)
        {
            NE.queryRadius(center, radius, results);
            NW.queryRadius(center, radius, results);
            SE.queryRadius(center, radius, results);
            SW.queryRadius(center, radius, results);
            return;
        }

        foreach (Entity e in entities)
        {
            if (e == null)
                continue;

            Vector2 pos = new Vector2(e.transform.position.x, e.transform.position.z);
            if (Vector2.Distance(pos, center) <= radius)
                results.Add(e);
        }
    }

    public void cleanUpTree()
    {
        if (!isSubdivided)
        {
            entities.RemoveAll(e => e == null);
            return;
        }

        if (NE == null && NW == null && SE == null && SW == null)
        {
            // isSubdivided returned true but all children are null — corrupt state
            // treat as leaf
            entities.RemoveAll(e => e == null);
            return;
        }

        NE.cleanUpTree();
        NW.cleanUpTree();
        SE.cleanUpTree();
        SW.cleanUpTree();
    }

    private void subdivide()
    {
        //Debug.Log($"Subdividing node at depth {depth} with {entities.Count} entities, bounds: {bounds}");

        entities.RemoveAll(e => e == null);

        float midX = bounds.x + bounds.width / 2f;
        float midY = bounds.y + bounds.height / 2f;
        float halfW = bounds.width / 2f;
        float halfH = bounds.height / 2f;
        const float overlap = 0.1f;

        SW = new QuadTree(new Rect(bounds.xMin, bounds.yMin, halfW + overlap, halfH + overlap), depth + 1);
        SE = new QuadTree(new Rect(midX, bounds.yMin, halfW + overlap, halfH + overlap), depth + 1);
        NW = new QuadTree(new Rect(bounds.xMin, midY, halfW + overlap, halfH + overlap), depth + 1);
        NE = new QuadTree(new Rect(midX, midY, halfW + overlap, halfH + overlap), depth + 1);

        foreach (Entity e in entities)
        {
            if (e == null) 
                continue;
            Vector2 pos = new Vector2(e.transform.position.x, e.transform.position.z);
            bool result = insertIntoChildren(e, pos);
            if (!result)
                Debug.LogWarning($"{e.name} at {pos} failed to insert into any child. Bounds: {bounds}");
        }
        entities.Clear();
    }

    private bool overlapsCircle(Rect rect, Vector2 center, float radius)
    {
        float closestX = Mathf.Clamp(center.x, rect.xMin, rect.xMax);
        float closestY = Mathf.Clamp(center.y, rect.yMin, rect.yMax);
        float dx = center.x - closestX;
        float dy = center.y - closestY;
        return (dx * dx + dy * dy) <= (radius * radius);
    }

    public void DrawGizmos()
    {
        Vector3 center = new Vector3(bounds.center.x, 0f, bounds.center.y);
        Vector3 size = new Vector3(bounds.width, 0f, bounds.height);
        Gizmos.DrawWireCube(center, size);

        if (isSubdivided)
        {
            NE.DrawGizmos();
            NW.DrawGizmos();
            SE.DrawGizmos();
            SW.DrawGizmos();
        }
    }

    public void DrawDebugLines()
    {
        Vector3 bl = new Vector3(bounds.xMin, 0f, bounds.yMin);
        Vector3 br = new Vector3(bounds.xMax, 0f, bounds.yMin);
        Vector3 tl = new Vector3(bounds.xMin, 0f, bounds.yMax);
        Vector3 tr = new Vector3(bounds.xMax, 0f, bounds.yMax);

        Debug.DrawLine(bl, br, Color.green);
        Debug.DrawLine(br, tr, Color.green);
        Debug.DrawLine(tr, tl, Color.green);
        Debug.DrawLine(tl, bl, Color.green);

        if (isSubdivided)
        {
            NE.DrawDebugLines();
            NW.DrawDebugLines();
            SE.DrawDebugLines();
            SW.DrawDebugLines();
        }
    }
}

public interface IQuadTreeEntity
{
    Vector2 Position { get; }
}
