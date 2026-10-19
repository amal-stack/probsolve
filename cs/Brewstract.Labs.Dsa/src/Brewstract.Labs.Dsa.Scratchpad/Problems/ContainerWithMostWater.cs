namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

[On(Platforms.LeetCode, 11)]
public class ContainerWithMostWater
{
    [Solution]
    [Complexity(Time = BigO.Quadratic, Space = BigO.Constant)]
    public static int BruteForce(int[] heights)
    {
        int maxArea = 0;

        for (int i = 0; i < heights.Length; i++)
        {
            for (int j = i + 1; j < heights.Length; j++)
            {
                int width = j - i;
                int height = int.Min(heights[i], heights[j]);

                maxArea = int.Max(maxArea, width * height);
            }
        }

        return maxArea;
    }

    [Solution(Preferred = true)]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static int TwoPointers(int[] heights)
    {
        int maxArea = 0;

        for (var (start, end) = (0, heights.Length - 1); start < end; )
        {
            int width = end - start;

            // The minimum of the two heights as water overflows beyond it
            int height = int.Min(heights[start], heights[end]);

            maxArea = int.Max(maxArea, width * height);

            if (heights[start] < heights[end])
            {
                start++;
                continue;
            }

            end--;
        }

        return maxArea;
    }
}
