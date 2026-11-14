using System;
using System.Collections.Generic;

/// <summary>
/// Min-priority queue stored as a binary heap in a list.
/// The element with the lowest priority is dequeued first.
/// </summary>
/// <remarks>
/// <para>Enqueue, Dequeue and TryDequeue run in O(log n).</para>
/// <para>Each Enqueue call adds a separate entry, including duplicate elements.</para>
/// </remarks>
/// <typeparam name="T">Type of the stored elements.</typeparam>
public class PriorityQueue<T>
{
    // Implicit heap: the children of index i live at 2i+1 and 2i+2, its parent at (i-1)/2.
    private readonly List<Entry> _heap = new List<Entry>();

    /// <summary>Number of entries in the heap.</summary>
    public int Count => _heap.Count;
    private struct Entry
    {
        public readonly T Item;
        public readonly float Priority;

        /// <summary>Creates an entry for item with priority.</summary>
        public Entry(T item, float priority)
        {
            Item = item;
            Priority = priority;
        }
    }

    /// <summary>Adds the item with the given priority.</summary>
    /// <param name="item">Element to enqueue.</param>
    /// <param name="priority">Priority of the item. Lower values are dequeued first.</param>
    public void Enqueue(T item, float priority)
    {
        _heap.Add(new Entry(item, priority));
        SiftUp(_heap.Count - 1);
    }

    /// <summary>Removes and returns the element with the lowest priority.</summary>
    /// <exception cref="InvalidOperationException">The queue is empty.</exception>
    public T Dequeue()
    {
        if (!TryDequeue(out T item, out _))
            throw new InvalidOperationException("The priority queue is empty.");

        return item;
    }

    /// <summary>Removes the element with the lowest priority.</summary>
    /// <param name="item">The removed element. Set to default when the queue is empty.</param>
    /// <param name="priority">The priority the element was enqueued with.</param>
    /// <returns>true when an element was removed.</returns>
    public bool TryDequeue(out T item, out float priority)
    {
        if (_heap.Count == 0)
        {
            item = default;
            priority = 0f;
            return false;
        }

        Entry root = _heap[0];
        int lastIndex = _heap.Count - 1;

        // Move the last leaf to the root and sink it down to its place.
        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);
        if (_heap.Count > 0)
            SiftDown(0);

        item = root.Item;
        priority = root.Priority;
        return true;
    }

    /// <summary>Removes all entries.</summary>
    public void Clear() => _heap.Clear();

    /// <summary>Moves the entry at index up while its priority is lower than its parent's.</summary>
    private void SiftUp(int index)
    {
        Entry entry = _heap[index];

        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_heap[parent].Priority <= entry.Priority)
                break;

            _heap[index] = _heap[parent];
            index = parent;
        }

        _heap[index] = entry;
    }

    /// <summary>Moves the entry at index down while a child has a lower priority.</summary>
    private void SiftDown(int index)
    {
        Entry entry = _heap[index];
        int count = _heap.Count;

        while (true)
        {
            int smallest = 2 * index + 1;
            if (smallest >= count)
                break;

            int right = smallest + 1;
            if (right < count && _heap[right].Priority < _heap[smallest].Priority)
                smallest = right;

            if (entry.Priority <= _heap[smallest].Priority)
                break;

            _heap[index] = _heap[smallest];
            index = smallest;
        }

        _heap[index] = entry;
    }
}
