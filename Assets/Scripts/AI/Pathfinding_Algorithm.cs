using System;
using System.Collections;
using System.Collections.Generic;
using FacundoColomboMethods;
using UnityEngine;

/// <summary>
/// Pathfinding algorithms over the Node graph.
/// </summary>
/// <remarks>
/// <para>Each method receives a Tuple of nodes. Item1 is the start node. Item2 is the goal node.</para>
/// <para>The cost of entering a node is Node.cost. The heuristic is the Euclidean distance to the goal node.</para>
/// <para>A* runs in O(E log V). Theta* runs A* and then smooths the path.</para>
/// <para>Author: Facundo Colombo.</para>
/// </remarks>
public static class Pathfinding_Algorithm
{
    /// <summary>Length of the downward raycast that snaps endpos to the ground.</summary>
    private const float GroundSnapDistance = 10f;

    #region A*

    /// <summary>
    /// Runs A* from the start node to the goal node within one frame.
    /// </summary>
    /// <param name="nodes">Start node and goal node.</param>
    /// <returns>
    /// The nodes of the path from start to goal, both included.
    /// An empty list when the goal is unreachable.
    /// </returns>
    /// <remarks>O(E log V).</remarks>
    public static List<Node> CalculateAStar(Tuple<Node, Node> nodes)
    {
        var search = new AStarSearch(nodes.Item1, nodes.Item2);
        while (search.Step()) { }

        return search.Path;
    }

    /// <summary>
    /// Runs A* from the start node to the goal node as a coroutine.
    /// The search expands stepsPerFrame nodes per frame.
    /// </summary>
    /// <param name="nodes">Start node and goal node.</param>
    /// <param name="onFinish">
    /// Called once when the search ends.
    /// Receives true and the path when the goal is reached.
    /// Receives false and an empty list when the goal is unreachable.
    /// </param>
    /// <param name="stepsPerFrame">Number of nodes expanded per frame.</param>
    /// <remarks>O(E log V) in total.</remarks>
    public static IEnumerator CalculateLazyAStar(this Tuple<Node, Node> nodes, Action<bool, List<Node>> onFinish, int stepsPerFrame = 6)
    {
        var search = new AStarSearch(nodes.Item1, nodes.Item2);
        int stepCount = 0;

        while (search.Step())
        {
            if (++stepCount < stepsPerFrame)
                continue;

            stepCount = 0;
            yield return null;
        }

        if (!search.Found)
            Debug.LogWarning("No se pudo hacer el camino en Lazy A STAR");

        onFinish(search.Found, search.Path);
    }

    /// <summary>
    /// A* search state that expands one node per Step call.
    /// A cheaper path to a node enqueues the node again.
    /// Step discards the stale frontier entries.
    /// </summary>
    private sealed class AStarSearch
    {
        private readonly Node _goal;
        private readonly Vector3 _goalPosition;

        private readonly PriorityQueue<FrontierEntry> _frontier = new PriorityQueue<FrontierEntry>();

        // For each visited node: the node we came from (the start maps to null).
        private readonly Dictionary<Node, Node> _cameFrom = new Dictionary<Node, Node>();

        // For each visited node: the lowest known accumulated cost from the start.
        private readonly Dictionary<Node, float> _costSoFar = new Dictionary<Node, float>();

        private bool _finished;

        /// <summary>Nodes of the path from start to goal. Empty until the search reaches the goal.</summary>
        public List<Node> Path { get; private set; } = new List<Node>();

        /// <summary>true when the search reached the goal.</summary>
        public bool Found { get; private set; }

        /// <summary>Node in the frontier with the cost it was enqueued with.</summary>
        private readonly struct FrontierEntry
        {
            public readonly Node Node;
            public readonly float CostSoFar;

            /// <summary>Creates an entry for node with costSoFar.</summary>
            public FrontierEntry(Node node, float costSoFar)
            {
                Node = node;
                CostSoFar = costSoFar;
            }
        }

        /// <summary>Creates a search from start to goal with start in the frontier.</summary>
        public AStarSearch(Node start, Node goal)
        {
            _goal = goal;
            _goalPosition = goal.transform.position;

            _cameFrom[start] = null;
            _costSoFar[start] = 0f;
            _frontier.Enqueue(new FrontierEntry(start, 0f), 0f);
        }

        /// <summary>Expands the frontier entry with the lowest priority.</summary>
        /// <returns>true while the search is running. false when the search has finished.</returns>
        /// <remarks>O(k log V), where k is the number of Neighbors of the expanded node.</remarks>
        public bool Step()
        {
            if (_finished)
                return false;

            if (!TryPopValidEntry(out FrontierEntry entry))
            {
                _finished = true;
                return false;
            }

            Node current = entry.Node;

            if (current == _goal)
            {
                Path = BuildPath(current);
                Found = true;
                _finished = true;
                return false;
            }

            foreach (Node next in current.Neighbors)
            {
                if (next == null)
                    continue;

                float newCost = entry.CostSoFar + next.cost;

                // Only relevant if it's the first time we reach it or this path is cheaper.
                if (_costSoFar.TryGetValue(next, out float knownCost) && knownCost <= newCost)
                    continue;

                _costSoFar[next] = newCost;
                _cameFrom[next] = current;

                float priority = newCost + Vector3.Distance(next.transform.position, _goalPosition);
                _frontier.Enqueue(new FrontierEntry(next, newCost), priority);
            }

            return true;
        }

        /// <summary>
        /// Dequeues frontier entries until one has the lowest known cost for its node.
        /// </summary>
        private bool TryPopValidEntry(out FrontierEntry entry)
        {
            while (_frontier.TryDequeue(out entry, out _))
            {
                if (entry.CostSoFar <= _costSoFar[entry.Node])
                    return true;
            }

            return false;
        }

        /// <summary>Builds the path by following _cameFrom back from the goal.</summary>
        private List<Node> BuildPath(Node goal)
        {
            var path = new List<Node>();
            for (Node node = goal; node != null; node = _cameFrom[node])
                path.Add(node);

            path.Reverse();
            return path;
        }
    }

    #endregion

    #region Theta*

    /// <summary>
    /// Runs Theta* from the start node to the goal node as a coroutine.
    /// The path is smoothed by line of sight.
    /// The result is the groundPosition of each node in the smoothed path.
    /// </summary>
    /// <param name="nodes">Start node and goal node.</param>
    /// <param name="wallMask">Layers that block line of sight.</param>
    /// <param name="onFinish">
    /// Called once when the search ends.
    /// Receives true and the path positions when the goal is reached.
    /// Receives false and an empty list when the goal is unreachable.
    /// </param>
    /// <param name="endpos">Position appended at the end of the path when it differs from default.</param>
    /// <param name="iterationPerFrame">Number of nodes expanded per frame.</param>
    /// <param name="maxDistance">Maximum line of sight distance between two nodes.</param>
    /// <remarks>O(E log V) for A* plus O(L) raycasts for the smoothing, where L is the path length.</remarks>
    public static IEnumerator CalculateLazyThetaStar(this Tuple<Node, Node> nodes, LayerMask wallMask, Action<bool, List<Vector3>> onFinish, Vector3 endpos = default, int iterationPerFrame = 30, float maxDistance = 20f)
    {
        return nodes.CalculateLazyAStar((pathMade, path) =>
        {
            if (!pathMade)
            {
                onFinish(false, new List<Vector3>());
                return;
            }

            List<Node> smoothed = SmoothPath(path, (from, to) => ColomboMethods.InLineOffSight(from, to, wallMask, maxDistance));

            List<Vector3> positions = ToGroundPositions(smoothed);
            if (endpos != default)
                positions.Add(endpos);

            onFinish(true, positions);
        }, iterationPerFrame);
    }

    /// <summary>
    /// Runs Theta* from the start node to the goal node within one frame.
    /// The result is the groundPosition of each node in the smoothed path.
    /// </summary>
    /// <param name="nodes">Start node and goal node.</param>
    /// <param name="wallMask">Layers that block line of sight.</param>
    /// <param name="endpos">
    /// Position snapped to the ground and appended at the end of the path when it differs from default.
    /// The last node is removed when the second-to-last node has line of sight to endpos.
    /// </param>
    /// <returns>The positions of the path. An empty list when the goal is unreachable.</returns>
    public static List<Vector3> CalculateThetaStar(this Tuple<Node, Node> nodes, LayerMask wallMask, Vector3 endpos = default)
    {
        List<Node> smoothed = SmoothPath(CalculateAStar(nodes), (from, to) => ColomboMethods.InLineOffSight(from, to, wallMask));
        List<Vector3> positions = ToGroundPositions(smoothed);

        if (endpos == default || positions.Count == 0)
            return positions;

        if (Physics.Raycast(endpos, Vector3.down, out RaycastHit hitInfo, GroundSnapDistance, wallMask))
        {
            if (smoothed.Count > 1 && ColomboMethods.InLineOffSight(smoothed[smoothed.Count - 2].transform.position, endpos, wallMask))
                positions.RemoveAt(positions.Count - 1);

            positions.Add(hitInfo.point);
        }

        return positions;
    }

    /// <summary>
    /// Returns a new list with the smoothed path.
    /// Each pending node is removed when the anchor has line of sight to the node after it.
    /// </summary>
    /// <param name="path">Path returned by A*.</param>
    /// <param name="hasLineOfSight">Returns true when two positions have line of sight.</param>
    /// <remarks>O(L) calls to hasLineOfSight, where L is the path length.</remarks>
    private static List<Node> SmoothPath(List<Node> path, Func<Vector3, Vector3, bool> hasLineOfSight)
    {
        if (path.Count <= 2)
            return new List<Node>(path);

        var result = new List<Node>(path.Count) { path[0] };

        Node anchor = path[0];   // last node that is guaranteed to stay in the path
        Node pending = path[1];  // candidate for removal if anchor can see the next node

        for (int i = 2; i < path.Count; i++)
        {
            Node next = path[i];

            if (hasLineOfSight(anchor.transform.position, next.transform.position))
            {
                // anchor sees next directly: pending is redundant.
                pending = next;
            }
            else
            {
                // No direct line of sight: pending stays and becomes the new anchor.
                result.Add(pending);
                anchor = pending;
                pending = next;
            }
        }

        result.Add(pending);
        return result;
    }

    /// <summary>Returns the groundPosition of each node in the path.</summary>
    private static List<Vector3> ToGroundPositions(List<Node> path)
    {
        var positions = new List<Vector3>(path.Count + 1);
        foreach (Node node in path)
            positions.Add(node.groundPosition);

        return positions;
    }

    #endregion
}
