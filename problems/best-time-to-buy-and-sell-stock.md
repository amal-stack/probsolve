---
title: Best Time to Buy and Sell Stock
status: pending
created: 2026-10-14
solved_on: null
implementations:
  csharp: ../cs/Brewstract.Labs.Dsa/src/Brewstract.Labs.Dsa.Scratchpad/Problems/BestTimeToBuyAndSellStock.cs
---
# Best Time to Buy and Sell Stock

<!-- solution-links:start -->
- csharp: [BestTimeToBuyAndSellStock.cs](../cs/Brewstract.Labs.Dsa/src/Brewstract.Labs.Dsa.Scratchpad/Problems/BestTimeToBuyAndSellStock.cs)
<!-- solution-links:end -->

## Observations
1. **Element order matters**
Order matters because there exists a temporal relationship between prices: a stock must first be bought before selling so a higher price before a buy is irrelevant.
Hence, sorting and finding overall and min-max not applicable.


2. **Find the maximum difference**
Find the largest difference such that the buy price precedes the sell price. Cannot use the **global min or max**.


3. We cannot return early as we need to look at the entire dataset.

## Solutions

### 1. Brute Force
compare every price against all the next prices.

### 2. Linear
For each future price, no need to compare it against each past price. We can track a **running minimum** as we iterate as need to minimize the buy price. We also track a **max profit** so far. This allows a one-pass comparison.

**Edge-case**
- Initailize minPrice to first element, starting from 0 means the minPrice will stay 0.

**Notes (AI explanation)**
- **The two ifs: exclusivity and order**
Years min be the value before processing p.

The min-update fires when p < min. Then p - min < 0, so profit > maxProfit can’t be true, because maxProfit >= 0.
The profit-update fires when p - min > maxProfit >= 0. Then p > min, so the min-update can’t fire.

- **Order doesn’t matter here**.
If you update min first, then on a new-minimum day profit = p - p = 0, which never beats maxProfit >= 0. On every other day nothing changed. Trace [7, 1, ...] at p = 1:

Profit first: 1 - 7 = -6, ignored, then min = 1.
Min first: min = 1, then 1 - 1 = 0, ignored.

## Keywords
- Running Minimum/Prefixed State
- Arrays
- Global min and max
- Greedy
