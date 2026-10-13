namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

public class Bin(Bin? previous, Bin? next, int frequency, HashSet<string> keys)
{
    public Bin? Previous { get; set; } = previous;
    public Bin? Next { get; set; } = next;
    public int Frequency { get; } = frequency;
    public HashSet<string> Keys { get; } = keys;

    public static Bin Head(string Key) => new(null, null, 1, [Key]);

    public static Bin Tail(Bin previous, string Key) =>
        new(previous, null, previous.Frequency + 1, [Key]);
}

public class AllOne
{
    private readonly Dictionary<string, int> _strings = new();

    private Bin? _head;

    private Bin? _tail;

    public AllOne() { }

    public void Inc(string key)
    {
        if (!_strings.TryGetValue(key, out int count))
        {
            // New key

            _strings[key] = 1;

            if (_head is null)
            {
                _head = Bin.Head(key);
                return;
            }

            // Head exists
            _head.Keys.Add(key);
            return;
        }

        // Existing key
        var newCount = count + 1;
        _strings[key] = newCount;

        // Head will be non-null
        Bin target = _head!;

        // Find target frequency node (always exists since key present)
        while (target.Next is not null && target.Frequency < count)
        {
            target = target.Next;
        }

        // - first node where !(freq < count) is freq == count
        // - remove existing
        target!.Keys.Remove(key);

        // append new tail if this is tail
        if (target.Next is null)
        {
            target.Next = Bin.Tail(target, key);
            return;
        }

        // insert if non-tail and node for freq exists

        if (target.Next.Frequency == newCount)
        {
            target.Next.Keys.Add(key);
            return;
        }

        // create new node if next freq > newCount
        var next = target.Next;
        target.Next = new Bin(target, next, newCount, [key]);
    }

    // public void Dec(string key)
    // {
    //     // Return as is already absent
    //     if (!_strings.TryGetValue(key, out int count))
    //     {
    //         return;
    //     }

    //     // Present

    //     // Remove if only occurs once
    //     if (count == 1)
    //     {
    //         _strings.Remove(key);
    //         _head!.Keys.Remove(key);
    //     }

    //     // Occurs > 1 times
    //     _strings[key] = count - 1;
    //     var target = _head!;

    //     while (target.Next is not null && target)

    // }

    // public string GetMaxKey()
    // {

    // }

    // public string GetMinKey() { }
}

/**
 * Your AllOne object will be instantiated and called as such:
 * AllOne obj = new AllOne();
 * obj.Inc(key);
 * obj.Dec(key);
 * string param_3 = obj.GetMaxKey();
 * string param_4 = obj.GetMinKey();
 */
