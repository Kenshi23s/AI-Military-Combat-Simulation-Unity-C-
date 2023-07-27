using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System.Linq;
using System;

public class AI_Manager : MonoSingleton<AI_Manager>
{
    [SerializeField]
    LayerMask _obstacle, _walls, _ground;
    public LayerMask Obstacle => _obstacle;
    public LayerMask WallMask => _walls;
    public LayerMask GroundMask => _ground;

    public List<NewAIMovement> flockingTargets => _flockingTargets; 
    private List<NewAIMovement> _flockingTargets = new List<NewAIMovement>();

    public float MaxDistanceBetweenNodes;

    [SerializeField] bool debugNodeConnections;

    [NonSerialized] public List<Node> nodes;

    public void AddToFlockingTargets(NewAIMovement a) => _flockingTargets.Add(a);

    protected override void SingletonAwake()
    {
        nodes = transform.GetChildrenComponents<Node>().ToList();
        nodes.ForEach(x =>
        {
            x.IntializeNode();
            x.GetComponent<DebugableObject>().canDebug = debugNodeConnections;

        });
        var colliders = nodes.Select(x => x.GetComponent<BoxCollider>()).Where(x => x != null);
        foreach (var item in colliders)
        {
            item.enabled = false;
        }
    }

    public Node GetNearestNode(Vector3 pos)
    {
        return GetNearestNodeOnSight(pos);
    }

    public Node GetNearestNodeOnSight(Vector3 pos)
    {
        Node nearestOnSight = null;
        float minSqrMagnitude = float.MaxValue;

        float sqrMag;
        Vector3 dir;
        foreach (var node in nodes)
        {
            dir = pos - node.groundPosition;
            sqrMag = dir.sqrMagnitude;
            if (sqrMag > minSqrMagnitude)
                continue;

            if (!Physics.Raycast(pos, dir, Mathf.Sqrt(sqrMag), WallMask))
            {
                nearestOnSight = node;
                minSqrMagnitude = sqrMag;
            }
        }

        return nearestOnSight;
    }
}
