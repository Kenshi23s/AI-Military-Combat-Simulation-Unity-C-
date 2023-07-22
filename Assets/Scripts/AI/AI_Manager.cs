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

    public List<AI_Movement> flockingTargets => _flockingTargets; 
    private List<AI_Movement> _flockingTargets = new List<AI_Movement>();

    public float MaxDistanceBetweenNodes;

    [SerializeField] bool debugNodeConnections;

    [NonSerialized] public List<Node> nodes;

    public void AddToFlockingTargets(AI_Movement a) => _flockingTargets.Add(a);

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
        return ColomboMethods.GetNearestOnSigth(pos, nodes, WallMask);
    }
}
