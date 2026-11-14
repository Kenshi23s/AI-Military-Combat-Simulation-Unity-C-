using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Point of the pathfinding graph.
/// Stores its Neighbors, its cost and its groundPosition.
/// </summary>
[RequireComponent(typeof(DebugableObject))]
public class Node : MonoBehaviour
{
    /// <summary>Radius of the SphereCast that checks line of sight to a neighbor.</summary>
    private const float NeighborCastRadius = 1f;

    /// <summary>Nodes connected to this node.</summary>
    public List<Node> Neighbors { get; private set; } = new List<Node>();

    /// <summary>Cost of entering this node.</summary>
    [SerializeField] public int cost = 0;

    /// <summary>Point on the ground below this node.</summary>
    public Vector3 groundPosition;

    // Same nodes as Neighbors, for O(1) lookups in NodeGizmo.
    private HashSet<Node> _neighborSet = new HashSet<Node>();

    /// <summary>Adds value to cost.</summary>
    public void AddCost(int value) => cost += value;

    /// <summary>Subtracts value from cost. The minimum cost is 0.</summary>
    public void SubstractCost(int value) => cost = Mathf.Max(cost - value, 0);

    /// <summary>
    /// Sets groundPosition and Neighbors.
    /// Neighbors are the nodes of AI_Manager closer than MaxDistanceBetweenNodes with line of sight.
    /// </summary>
    /// <param name="nodePositions">Position of each node in AI_Manager.nodes, by index.</param>
    /// <remarks>O(N) distance checks plus one SphereCast per node in range.</remarks>
    public void IntializeNode(IReadOnlyList<Vector3> nodePositions)
    {
        AI_Manager manager = AI_Manager.instance;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, Mathf.Infinity, manager.GroundMask))
            groundPosition = hitInfo.point;
        else
            groundPosition = transform.position;

        Neighbors = FindNeighbors(manager, nodePositions);
        _neighborSet = new HashSet<Node>(Neighbors);

        GetComponent<DebugableObject>().AddGizmoAction(NodeGizmo);
        GetComponent<MeshRenderer>().enabled = false;
    }

    /// <summary>Returns true when node is in Neighbors.</summary>
    public bool IsNeighbor(Node node) => _neighborSet.Contains(node);

    private List<Node> FindNeighbors(AI_Manager manager, IReadOnlyList<Vector3> nodePositions)
    {
        Vector3 position = transform.position;
        float sqrMaxDistance = manager.MaxDistanceBetweenNodes * manager.MaxDistanceBetweenNodes;
        var neighbors = new List<Node>();

        for (int i = 0; i < manager.nodes.Count; i++)
        {
            Node candidate = manager.nodes[i];
            if (candidate == this)
                continue;

            // The distance check runs first. The SphereCast runs only for the nodes in range.
            Vector3 dir = nodePositions[i] - position;
            if (dir.sqrMagnitude >= sqrMaxDistance)
                continue;

            if (!Physics.SphereCast(position, NeighborCastRadius, dir, out _, dir.magnitude, manager.WallMask))
                neighbors.Add(candidate);
        }

        return neighbors;
    }

    // Draws the connections where both nodes have each other as neighbors.
    private void NodeGizmo()
    {
        Gizmos.color = Color.blue;

        foreach (Node node in Neighbors)
        {
            if (node.IsNeighbor(this))
                Gizmos.DrawLine(node.transform.position, transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundPosition, 1f);
    }
}
