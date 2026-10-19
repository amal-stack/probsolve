namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

[On(Platforms.LeetCode, 121)]
public class BestTimeToBuyAndSellStock
{
    [Solution]
    [Complexity(Time = BigO.Quadratic, Space = BigO.Constant)]
    public static int BruteForce(int[] prices)
    {
        int maxProfit = 0;
        for (int i = 0; i < prices.Length; i++)
        {
            for (int j = i + 1; j < prices.Length; j++)
            {
                int profit = prices[j] - prices[i];
                if (profit > maxProfit)
                {
                    maxProfit = profit;
                }
            }
        }
        return maxProfit;
    }

    [Solution]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static int TrackMinimumPrice(int[] prices)
    {
        int minPrice = prices[0];
        int maxProfit = 0;

        for (int i = 1; i < prices.Length; i++)
        {
            int profit = prices[i] - minPrice;
            if (profit > maxProfit)
            {
                maxProfit = profit;
            }

            if (prices[i] < minPrice)
            {
                minPrice = prices[i];
            }
        }
        return maxProfit;
    }

    [VariantOf(nameof(TrackMinimumPrice), "Using Min and Max functions")]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static int TrackMinimumPriceMinMax(int[] prices)
    {
        int minPrice = prices[0];
        int maxProfit = 0;

        for (int i = 1; i < prices.Length; i++)
        {
            maxProfit = Math.Max(maxProfit, prices[i] - minPrice);

            minPrice = Math.Min(minPrice, prices[i]);
        }
        return maxProfit;
    }

    [VariantOf(nameof(TrackMinimumPrice), "Using Linq", LanguageFeatures = ["Linq"])]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static int TrackMinimumPriceLinq(IEnumerable<int> prices) =>
        // works for any IEnumerable
        prices
            .Aggregate(
                (MinPrice: int.MaxValue, MaxProfit: 0),
                (acc, val) =>
                    (Math.Min(acc.MinPrice, val), Math.Max(acc.MaxProfit, val - acc.MinPrice))
            )
            .MaxProfit;

    [Flawed("minPrice will always be 0")]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static int TrackMinimumPriceFlawed(int[] prices)
    {
        int minPrice = 0;
        int maxProfit = 0;
        for (int i = 0; i < prices.Length; i++)
        {
            int profit = prices[i] - minPrice;
            if (profit > 0 && profit > maxProfit)
            {
                maxProfit = profit;
            }

            if (prices[i] < minPrice)
            {
                minPrice = prices[i];
            }
        }
        return maxProfit;
    }
}
