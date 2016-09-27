using UnityEngine;

public class NavMeshPointGenerator
{
    private float _range;

    public NavMeshPointGenerator(float range)
    {
        _range = range;
    }


    public NavMeshPointGenerator()
    {
        Bounds navMeshBounds = new Bounds();
        navMeshBounds.center = Vector3.zero;
        
        
        Vector3[] navMeshVertices = NavMesh.CalculateTriangulation().vertices;

        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;


        Vector3 mean = Vector3.zero;
        foreach (var vertex in navMeshVertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);

            mean += vertex;
        }

        mean = mean / navMeshVertices.Length;

        navMeshBounds.SetMinMax(min, max);
        navMeshBounds.Encapsulate(max);

        GameObject bb = new GameObject();
        bb.transform.position = mean;
        var col = bb.AddComponent<BoxCollider>();
        col.center = Vector3.zero;
        col.size = navMeshBounds.size;

        _range = navMeshBounds.extents.magnitude;
    }

    public Vector3 RandomPointOnNavMesh(Vector3 center)
    {
        do
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * _range;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 10.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        } while (true);
    }


}
