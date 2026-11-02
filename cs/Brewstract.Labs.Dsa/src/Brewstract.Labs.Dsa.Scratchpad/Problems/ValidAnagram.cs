namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

[On(Platforms.LeetCode, 242)]
public class ValidAnagram
{
    [Solution(Preferred = true)]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static bool HashDecrementFrequencies(string s, string t)
    {
        if (s.Length != t.Length)
        {
            return false;
        }

        Dictionary<char, int> frequencies = s.GroupBy(letter => letter)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var letter in t)
        {
            if (
                !frequencies.TryGetValue(letter, out int value) // is a new letter (a new letter doesn't exist in the map)
                || value < 0 // has the letter appeared more times than it did in s
            )
            {
                return false;
            }
            frequencies[letter] = --value;
        }

        return true;
    }


    [Solution]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static bool HashCompare(string s, string t)
    {
      throw new NotImplementedException();
    }
}
