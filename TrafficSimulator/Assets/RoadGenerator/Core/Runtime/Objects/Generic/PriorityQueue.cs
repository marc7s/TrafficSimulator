using System.Collections.Generic;

/// <summary> General priority queue implementation </summary>
public class PriorityQueue<T> where T : System.IComparable<T>
{
    private List<T> _list;
    public int Count { get { return _list.Count; } }
    public readonly bool IsDescending;
    public PriorityQueue()
    {
        _list = new List<T>();
    }

    public PriorityQueue(bool isdesc)
        : this()
    {
        IsDescending = isdesc;
    }

    public PriorityQueue(int capacity)
        : this(capacity, false)
    { }

    public PriorityQueue(IEnumerable<T> collection)
        : this(collection, false)
    { }

    public PriorityQueue(int capacity, bool isdesc)
    {
        _list = new List<T>(capacity);
        IsDescending = isdesc;
    }

    public PriorityQueue(IEnumerable<T> collection, bool isdesc)
        : this()
    {
        IsDescending = isdesc;
        foreach (var item in collection)
            Enqueue(item);
    }


    public void Enqueue(T x)
    {
        _list.Add(x);
        int i = Count - 1;

        while (i > 0)
        {
            int p = (i - 1) / 2;
            if ((IsDescending ? -1 : 1) * _list[p].CompareTo(x) <= 0) break;

            _list[i] = _list[p];
            i = p;
        }

        if (Count > 0) _list[i] = x;
    }

    public T Dequeue()
    {
        T target = Peek();
        T root = _list[Count - 1];
        _list.RemoveAt(Count - 1);

        int i = 0;
        while (i * 2 + 1 < Count)
        {
            int a = i * 2 + 1;
            int b = i * 2 + 2;
            int c = b < Count && (IsDescending ? -1 : 1) * _list[b].CompareTo(_list[a]) < 0 ? b : a;

            if ((IsDescending ? -1 : 1) * _list[c].CompareTo(root) >= 0) break;
            _list[i] = _list[c];
            i = c;
        }

        if (Count > 0) _list[i] = root;
        return target;
    }

    public T Peek()
    {
        if (Count == 0) throw new System.InvalidOperationException("Queue is empty.");
        return _list[0];
    }

    public void Clear()
    {
        _list.Clear();
    }
}