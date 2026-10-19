using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

[On(Platforms.LeetCode, id: 2)]
public class AddTwoNumbers
{
    [Solution]
    [Complexity(Time = BigO.Quadratic, Space = BigO.Constant)]
    public static ListNode BruteForce(ListNode l1, ListNode l2) =>
        NumberToNode(NodeToNumber(l1) + NodeToNumber(l2));

    [Solution(Preferred = true)]
    public static ListNode AddWithCarry(ListNode? l1, ListNode? l2)
    {
        int carry = 0;
        ListNode dummyHead = new();
        ListNode current = dummyHead;

        while (l1 is not null || l2 is not null)
        {
            int n1 = l1?.val ?? 0;
            int n2 = l2?.val ?? 0;

            int sum = n1 + n2 + carry;
            carry = sum / 10;

            current = current.next = new(sum % 10);

            (l1, l2) = (l1?.next, l2?.next);
        }

        if (carry > 0)
        {
            current.next = new(carry);
        }

        return dummyHead.next;
    }

    [VariantOf(nameof(AddWithCarry), "Merge carry condition into while")]
    public static ListNode AddWithCarryConditionMerged(ListNode? l1, ListNode? l2)
    {
        int carry = 0;
        ListNode dummyHead = new();
        ListNode current = dummyHead;

        while (l1 is not null || l2 is not null || carry > 0)
        {
            int n1 = l1?.val ?? 0;
            int n2 = l2?.val ?? 0;

            int sum = n1 + n2 + carry;
            carry = sum / 10;

            current = current.next = new(sum % 10);

            (l1, l2) = (l1?.next, l2?.next);
        }

        return dummyHead.next;
    }

    [VariantOf(nameof(AddWithCarry), Tags = ["Recursion"])]
    public static ListNode? AddWithCarryRecursive(ListNode? l1, ListNode? l2, int carry = 0)
    {
        if (l1 is null && l2 is null && carry == 0)
        {
            return null;
        }

        var (newCarry, digit) = Math.DivRem(l1?.val ?? 0 + l2?.val ?? 0 + carry, 10);

        return new(digit, AddWithCarryRecursive(l1?.next, l2?.next, newCarry));
    }

    private static ListNode NumberToNode(BigInteger number)
    {
        // Dummy head:
        var head = new ListNode();

        var current = head;
        while (number > 0)
        {
            current = current.next = new((int)(number % 10));
            number /= 10;
        }

        // Returns dummy head if head.next (real head) is zero
        // This happens when number is 0
        // Safe to return dummy as its val is already 0 and next is null
        return head.next ?? head;
    }

    [SuppressMessage("CodeQuality", "IDE0051")]
    private static ListNode NumberToNodeWithoutDummy(BigInteger number)
    {
        if (number == 0)
        {
            return new();
        }

        ListNode? node = null;
        ListNode? head = null;
        while (number > 0)
        {
            BigInteger digit = number % 10;
            number /= 10;
            ListNode current = new((int)digit);

            head ??= current;
            node?.next = current;
            node = current;
        }

        return head!;
    }

    private static BigInteger NodeToNumber(ListNode node)
    {
        BigInteger number = 0;
        BigInteger place = 1;
        while (node != null)
        {
            number = node.val * place + number;
            place *= 10;
            node = node.next;
        }

        return number;
    }
}

#nullable disable
public class ListNode(int val = 0, ListNode next = null)
{
    public int val = val;
    public ListNode next = next;
}
#nullable enable
