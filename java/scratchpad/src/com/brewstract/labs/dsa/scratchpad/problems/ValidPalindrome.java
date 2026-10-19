package com.brewstract.labs.dsa.scratchpad.problems;

import com.brewstract.labs.dsa.scratchpad.problems.shared.BigO;
import com.brewstract.labs.dsa.scratchpad.problems.shared.Complexity;
import com.brewstract.labs.dsa.scratchpad.problems.shared.On;
import com.brewstract.labs.dsa.scratchpad.problems.shared.Platforms;
import com.brewstract.labs.dsa.scratchpad.problems.shared.Solution;

@On(platform = Platforms.LEETCODE, id = "125")
public class ValidPalindrome {

    @Solution
    @Complexity(time = BigO.LINEAR, space = BigO.CONSTANT)
    public boolean isPalindrome(String s) {
        for (int start = 0, end = s.length() - 1; start < end; start++, end--) {
            while (start < end && !Character.isLetterOrDigit(s.charAt(start))) {
                start++;
            }

            while (start < end && !Character.isLetterOrDigit(s.charAt(end))) {
                end--;
            }

            if (Character.toLowerCase(s.charAt(start)) != Character.toLowerCase(s.charAt(end))) {
                return false;
            }

        }
        return true;

    }

    @Solution
    @Complexity(time = BigO.LINEAR, space = BigO.LINEAR)
    public boolean isPalindromeNormalizeAndCompare(String s) {
        StringBuilder normalized = s.chars().filter(Character::isLetterOrDigit).map(Character::toUpperCase)
                .collect(StringBuilder::new,
                        StringBuilder::appendCodePoint,
                        StringBuilder::append);

        for (int i = 0; i < normalized.length() / 2; i++) {
            if (normalized.charAt(i) != normalized.charAt(normalized.length() - 1 - i)) {
                return false;
            }
        }

        return true;
    }

}
