using System.Collections;
using System.Diagnostics;

namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

[On(Platforms.LeetCode, 1)]
public class TwoSum
{
    [Solution]
    [Complexity(Time = BigO.Quadratic, Space = BigO.Constant)]
    public static List<int> BruteForce(List<int> nums, int target)
    {
        for (int i = 0; i < nums.Count; i++)
        {
            for (int j = i + 1; j < nums.Count; j++)
            {
                if (nums[i] + nums[j] == target)
                {
                    return [i, j];
                }
            }
        }

        throw Unreachable();        
        //return [];
    }

    [Solution]
    public static List<int> SortAndTwoPointers(List<int> nums, int target)
    {
        (int Number, int Index)[] sortedNums =
        [
            .. nums.Select((num, index) => (num, index)).OrderBy(pair => pair.num),
        ];

        for (int start = 0, end = nums.Count - 1; start != end; )
        {
            var (startNum, startIdx) = sortedNums[start];
            var (endNum, endIdx) = sortedNums[end];

            int result = startNum + endNum;
            if (result == target)
            {
                return startIdx < endIdx ? [startIdx, endIdx] : [endIdx, startIdx];
            }
            if (result > target)
            {
                end -= 1;
                continue;
            }

            start += 1;
        }

        throw Unreachable();
    }

    [Solution]
    public static List<int> IndirectSortAndTwoPointers(List<int> nums, int target)
    {
        int[] indexes = Sorter.SortIndirectLinq(nums);

        for (int start = 0, end = nums.Count - 1; start != end; )
        {
            int startIdx = indexes[start];
            int endIdx = indexes[end];
            int result = nums[startIdx] + nums[endIdx];
            if (result == target)
            {
                return startIdx < endIdx ? [startIdx, endIdx] : [endIdx, startIdx];
            }
            if (result > target)
            {
                end -= 1;
                continue;
            }

            start += 1;
        }

        throw Unreachable();
    }

    [VariantOf(nameof(IndirectSortAndTwoPointers))]
    public static List<int> IndirectSortAndTwoPointersRemappedList(List<int> nums, int target)
    {
        var sorted = RemappedList.Sorted(nums);

        for (int start = 0, end = nums.Count - 1; start != end; )
        {
            int result = sorted[start] + sorted[end];
            if (result == target)
            {
                var startIdx = sorted.SourceIndexOf(start);
                var endIdx = sorted.SourceIndexOf(end);

                return startIdx < endIdx ? [startIdx, endIdx] : [endIdx, startIdx];
            }
            if (result > target)
            {
                end -= 1;
                continue;
            }

            start += 1;
        }

        throw Unreachable();
    }

    [Solution(Preferred = true)]
    public static List<int> HashStoreCompliments(List<int> nums, int target)
    {
        Dictionary<int, int> complements = [];

        for (int i = 0; i < nums.Count; i++)
        {
            int complement = target - nums[i];

            if (complements.TryGetValue(nums[i], out int previous))
            {
                return [previous, i];
            }

            // This can throw exceptions on duplicates
            // e.g.: [1, 1, 5, 9], T = 14
            // complements.Add(complement, i);
            complements[complement] = i;
        }

        throw Unreachable();
    }

    [Solution(Preferred = true)]
    public static List<int> HashStoreVisited(List<int> nums, int target)
    {
        Dictionary<int, int> visited = [];

        for (int i = 0; i < nums.Count; i++)
        {
            int complement = target - nums[i];

            if (visited.TryGetValue(complement, out int previous))
            {
                return [previous, i];
            }

            // complements.Add(complement, i); // This can throw exceptions on duplicates
            visited[nums[i]] = i;
        }

        throw Unreachable();
    }

    private static UnreachableException Unreachable() =>
        throw new UnreachableException("Problem guarantees valid indices always exist");
}

/// <summary>
/// Remaps index access to a list using the given <paramref name="indexes"/>
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="source"></param>
/// <param name="indexes"></param>
class RemappedList<T>(IReadOnlyList<T> source, IReadOnlyList<int> indexes) : IReadOnlyList<T>
{
    public T this[int index] => source[indexes[index]];

    public int SourceIndexOf(int index) => indexes[index];

    public int Count => indexes.Count;

    //public IEnumerator<T> GetEnumerator() => indexes.Select(idx => source[idx]).GetEnumerator();

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

class RemappedList
{
    /// <summary>
    /// Creates a <see cref="RemappedList"/> by creating a sorted index over a list and delegating access to it via the indexer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static RemappedList<T> Sorted<T>(IReadOnlyList<T> source) =>
        new(source, Sorter.SortIndirectLinqQuery(source));
}

public class Sorter
{
    public static List<T> SortLinqQuery<T>(IEnumerable<T> nums) =>
        [.. from num in nums orderby num select num];

    public static List<T> SortLinq<T>(IEnumerable<T> nums) => [.. nums.Order()];

    public static int[] SortIndirectLinq<T>(IReadOnlyList<T> nums) =>
        [.. Enumerable.Range(0, nums.Count).OrderBy(i => nums[i])];

    public static int[] SortIndirectLinqQuery<T>(IReadOnlyList<T> nums) =>
        [.. from index in Enumerable.Range(0, nums.Count) orderby nums[index] select index];
}
