using System;
using System.Collections.Generic;
using FacundoColomboMethods;
using UnityEngine;

/// <summary>
/// Builds the Node graph from the Node children of this transform.
/// Holds the layer masks used by the AI.
/// </summary>
public class AI_Manager : MonoSingleton<AI_Manager>
{
    [SerializeField]
    private LayerMask _obstacle, _walls, _ground;

    /// <summary>Layers of the obstacles.</summary>
    public LayerMask Obstacle => _obstacle;

    /// <summary>Layers that block line of sight.</summary>
    public LayerMask WallMask => _walls;

    /// <summary>Layers of the ground.</summary>
    public LayerMask GroundMask => _ground;

    /// <summary>The NewAIMovement components added with AddToFlockingTargets.</summary>
    public List<NewAIMovement> flockingTargets => _flockingTargets;
    private List<NewAIMovement> _flockingTargets = new List<NewAIMovement>();

    /// <summary>Maximum distance between two connected nodes.</summary>
    public float MaxDistanceBetweenNodes;

    [SerializeField] private bool debugNodeConnections;

    /// <summary>Every Node child of this transform.</summary>
    [NonSerialized] public List<Node> nodes;

    // Squared distance from the queried position to each node, by index. Reused by GetNearestNodeOnSight.
    private float[] _sqrDistances = new float[0];

    /// <summary>Adds the NewAIMovement to flockingTargets.</summary>
    public void AddToFlockingTargets(NewAIMovement a) => _flockingTargets.Add(a);

    protected override void SingletonAwake()
    {
        nodes = new List<Node>();
        foreach (Node node in transform.GetChildrenComponents<Node>())
        {
            if (node != null)
                nodes.Add(node);
        }

        // Positions are read once. Each IntializeNode call compares against all of them.
        var nodePositions = new Vector3[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
            nodePositions[i] = nodes[i].transform.position;

        foreach (Node node in nodes)
        {
            node.IntializeNode(nodePositions);
            node.GetComponent<DebugableObject>().canDebug = debugNodeConnections;
        }

        foreach (Node node in nodes)
        {
            if (node.TryGetComponent(out BoxCollider boxCollider))
                boxCollider.enabled = false;
        }

        _sqrDistances = new float[nodes.Count];
    }

    /// <summary>Returns the nearest node on sight of pos.</summary>
    public Node GetNearestNode(Vector3 pos)
    {
        return GetNearestNodeOnSight(pos);
    }

    /// <summary>
    /// Returns the node with the nearest groundPosition that has line of sight to pos.
    /// Returns null when every node is behind a wall.
    /// </summary>
    /// <remarks>
    /// One O(N) pass computes the distances.
    /// Each raycast tests the nearest untested node in O(N).
    /// The total is O(N · r), where r is the number of raycasts.
    /// </remarks>
    public Node GetNearestNodeOnSight(Vector3 pos)
    {
        int count = nodes.Count;
        if (_sqrDistances.Length < count)
            _sqrDistances = new float[count];

        for (int i = 0; i < count; i++)
            _sqrDistances[i] = (nodes[i].groundPosition - pos).sqrMagnitude;

        for (int tested = 0; tested < count; tested++)
        {
            int nearest = IndexOfMinDistance(count);
            if (nearest < 0)
                break;

            Node node = nodes[nearest];
            Vector3 dir = node.groundPosition - pos;

            if (!Physics.Raycast(pos, dir, Mathf.Sqrt(_sqrDistances[nearest]), WallMask))
                return node;

            // The node is behind a wall. Infinity marks it as tested.
            _sqrDistances[nearest] = float.PositiveInfinity;
        }

        return null;
    }

    // Returns the index of the lowest value in _sqrDistances, or -1 when every value is infinity.
    private int IndexOfMinDistance(int count)
    {
        int index = -1;
        float min = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            if (_sqrDistances[i] < min)
            {
                min = _sqrDistances[i];
                index = i;
            }
        }

        return index;
    }
}
