
using System.Text;


namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

[On(Platforms.LeetCode, 345)]
public class ReverseVowelsOfAString
{
    [Solution]
    [Complexity(Time = BigO.Linear, Space = BigO.Linear)]
    public static string ReverseVowels(string s)
    {
        StringBuilder buffer = new(s);

        static bool IsVowel(char c) => char.ToLowerInvariant(c) is 'a' or 'e' or 'i' or 'o' or 'u';

        for (int left = 0, right = buffer.Length - 1; left < right; right--, left++)
        {
            char leftChar =  buffer[left];
            while (left < right && !IsVowel(leftChar))
            {
                leftChar = buffer[++left];
            } 

            char rightChar = buffer[right];
            while (left < right && !IsVowel(rightChar))
            {
                rightChar = buffer[--right];
            } 

            (buffer[left], buffer[right]) = (buffer[right], buffer[left]);
        }

        return buffer.ToString();
    }

    [VariantOf(nameof(ReverseVowels), "Using char[] instead of StringBuilder")]
    public static string ReverseVowelsCharArray(string s)
    {
        char[] buffer = s.ToCharArray();

        static bool IsVowel(char c) => char.ToLower(c) is 'a' or 'e' or 'i' or 'o' or 'u';

        for (int left = 0, right = buffer.Length - 1; left < right; right--, left++)
        {
            char leftChar =  buffer[left];
            while (left < right && !IsVowel(leftChar))
            {
                leftChar = buffer[++left];
            } 

            char rightChar = buffer[right];
            while (left < right && !IsVowel(rightChar))
            {
                rightChar = buffer[--right];
            } 

            

            (buffer[left], buffer[right]) = (buffer[right], buffer[left]);
        }

        return new string(buffer);
    }


    [VariantOf(nameof(ReverseVowels), "Inline vowel check (without ToLower) + remove temporary variables", Preferred = true)]
    public string ReverseVowelsOptimal(string s)
    {
        char[] buffer = s.ToCharArray();

        static bool IsVowel(char @char) => @char is 'a' or 'e' or 'i' or 'o' or 'u' or 'A' or 'E' or 'I' or 'O' or 'U';

        for (int left = 0, right = buffer.Length - 1; left < right; right--, left++)
        {
            while (left < right && !IsVowel(buffer[left]))
            {
                left++;
            } 

            while (left < right && !IsVowel(buffer[right]))
            {
                right--;
            } 

            

            (buffer[left], buffer[right]) = (buffer[right], buffer[left]);
        }

        return new string(buffer);
    }
}

