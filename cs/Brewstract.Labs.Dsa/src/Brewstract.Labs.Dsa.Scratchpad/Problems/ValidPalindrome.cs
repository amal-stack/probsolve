namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

public class ValidPalindrome
{
    [Solution]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static bool IsPalindrome(string s)
    {
        for (int start = 0, end = s.Length - 1; start < end; start++, end--)
        {
            while (start < end && !char.IsAsciiLetterOrDigit(s[start]))
            {
                start++;
            }

            while (start < end && !char.IsAsciiLetterOrDigit(s[end]))
            {
                end--;
            }

            if (char.ToLowerInvariant(s[start]) != char.ToLowerInvariant(s[end]))
            {
                return false;
            }
        }
        return true;
    }

    [VariantOf(nameof(IsPalindrome), Preferred = true)]
    public static bool IsPalindromeSingleStep(string s)
    {
        var (start, end) = (0, s.Length - 1);

        while (start < end)
        {
            if (!char.IsAsciiLetterOrDigit(s[start]))
            {
                start++;
                continue;
            }

            if (!char.IsAsciiLetterOrDigit(s[end]))
            {
                end--;
                continue;
            }

            if (char.ToLowerInvariant(s[start]) != char.ToLowerInvariant(s[end]))
            {
                return false;
            }

            start++;
            end--;
        }

        return true;
    }

    public static bool BruteForceLinq(string s)
    {
        string normalized = string.Concat(
            from c in s
            where char.IsAsciiLetterOrDigit(c)
            select char.ToUpperInvariant(c)
        );

        for (int i = 0; i < normalized.Length / 2; i++)
        {
            if (normalized[i] != normalized[normalized.Length - 1 - i])
            {
                return false;
            }
        }
        return true;
    }
}
