package com.brewstract.labs.dsa.scratchpad.problems;

public class ReverseString {

    private final CharSwappable swappable;

    public ReverseString(CharSwappable swappable) {
        this.swappable = swappable;
    }

    public ReverseString() {
        this(CharSwappable::swapWithTemp);
    }

    public char[] bruteForce(char[] string) {
        char[] result = new char[string.length];

        for (int i = 0; i < string.length; i++) {
            result[i] = string[string.length - i - 1];
        }

        return result;
    }

    public void midPointOffset(char[] string) {
        for (int i = 0; i < string.length / 2; i++) {
            swap(string, i, string.length - i - 1);
        }
    }

    public void twoPointers(char[] string) {
        for (int start = 0, end = string.length - 1; start < end; start++, end--) {
            swap(string, start, end);
        }
    }

    private void swap(char[] source, int idx1, int idx2) {
        swappable.swap(source, idx1, idx2);
    }
}

interface CharSwappable {
    void swap(char[] source, int idx1, int idx2);

    static void swapWithTemp(char[] source, int idx1, int idx2) {
        char temp = source[idx1];
        source[idx1] = source[idx2];
        source[idx2] = temp;
    }

    static void swapXor(char[] source, int idx1, int idx2) {
        source[idx1] = (char) (source[idx1] ^ source[idx2]);
        source[idx2] = (char) (source[idx1] ^ source[idx2]);
        source[idx1] = (char) (source[idx1] ^ source[idx2]);
    }

    static void swapXorCompoundAssignment(char[] source, int idx1, int idx2) {
        source[idx1] ^= source[idx2];
        source[idx2] ^= source[idx1];
        source[idx1] ^= source[idx2];
    }
}
