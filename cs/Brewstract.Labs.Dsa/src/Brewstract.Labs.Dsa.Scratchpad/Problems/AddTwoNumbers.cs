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
        ListNode head = new();
        ListNode current = head; 

        while(l1 is not null || l2 is not null)
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

        return head.next;
    }

    private static ListNode NumberToNode(BigInteger number)
    {
        var head = new ListNode();
        var current = head;
        while (number > 0)
        {
            current = current.next = new((int)(number % 10));
            number /= 10;
        }
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
