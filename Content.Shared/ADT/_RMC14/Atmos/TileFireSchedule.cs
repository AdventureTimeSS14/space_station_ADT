// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

namespace Content.Shared._RMC14.Atmos;

// 🙄🙄🙄🙄🙄sandbox
public sealed class TileFireSchedule
{
    private readonly List<(TimeSpan At, EntityUid Uid)> _heap = new();

    public int Count => _heap.Count;

    public void Enqueue(EntityUid uid, TimeSpan at)
    {
        _heap.Add((at, uid));

        var child = _heap.Count - 1;
        while (child > 0)
        {
            var parent = (child - 1) / 2;
            if (_heap[parent].At <= _heap[child].At)
                break;

            (_heap[parent], _heap[child]) = (_heap[child], _heap[parent]);
            child = parent;
        }
    }

    public bool TryPeek(out EntityUid uid, out TimeSpan at)
    {
        if (_heap.Count == 0)
        {
            uid = default;
            at = default;
            return false;
        }

        (at, uid) = _heap[0];
        return true;
    }

    public void Dequeue()
    {
        if (_heap.Count == 0)
            return;

        var last = _heap.Count - 1;
        _heap[0] = _heap[last];
        _heap.RemoveAt(last);

        var parent = 0;
        while (true)
        {
            var left = parent * 2 + 1;
            if (left >= _heap.Count)
                break;

            var smallest = left;
            var right = left + 1;
            if (right < _heap.Count && _heap[right].At < _heap[left].At)
                smallest = right;

            if (_heap[parent].At <= _heap[smallest].At)
                break;

            (_heap[parent], _heap[smallest]) = (_heap[smallest], _heap[parent]);
            parent = smallest;
        }
    }

    public void Clear()
    {
        _heap.Clear();
    }
}
