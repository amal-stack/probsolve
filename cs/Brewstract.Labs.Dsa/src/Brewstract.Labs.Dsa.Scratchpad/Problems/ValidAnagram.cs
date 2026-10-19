using Brewstract.Labs.Dsa.Scratchpad.Problems.Shared;

namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

[On(Platforms.LeetCode, 242)]
public class ValidAnagram
{
    [Solution]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static bool Hash(string s, string t)
    {
        if (s.Length != t.Length)
        {
            return false;
        }

        Dictionary<char, int> frequencies = s.GroupBy(letter => letter)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var letter in t)
        {
            if (!frequencies.TryGetValue(letter, out int value))
            {
                return false;
            }
            frequencies[letter] = --value;

            
        }

        return true;
    }
}
