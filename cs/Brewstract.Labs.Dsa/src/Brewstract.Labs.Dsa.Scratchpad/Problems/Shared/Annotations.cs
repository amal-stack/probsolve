using System.Globalization;

namespace Brewstract.Labs.Dsa.Scratchpad.Problems.Shared;

[AttributeUsage(AttributeTargets.Method)]
public class SolutionAttribute(string description = "") : Attribute
{
    public string Description { get; } = description;

    public bool Preferred { get; init; }
}

[AttributeUsage(AttributeTargets.Method)]
public class VariantOfAttribute(string of, string description) : Attribute
{
    public string Of { get; } = of;
    public string Description { get; } = description;

    public string[] LanguageFeatures { get; init; } = [];

    public string[] Tags { get; init; } = [];
}

[AttributeUsage(AttributeTargets.Method)]
public class FlawedAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}

[AttributeUsage(AttributeTargets.Method)]
public class ComplexityAttribute : Attribute
{
    public string Time { get; init; } = string.Empty;

    public string Space { get; init; } = string.Empty;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class OnAttribute(string platform, string id) : Attribute
{
    public OnAttribute(string platform, int id)
        : this(platform, id.ToString(CultureInfo.InvariantCulture)) { }

    public string Platform { get; } = platform;

    public string Id { get; } = id;
}

public class LeetCodeAttribute(int id) : OnAttribute(Platforms.LeetCode, id);

public static class BigO
{
    public const string Constant = "O(1)";
    public const string Logarithmic = "O(log n)";
    public const string Linear = "O(n)";
    public const string Linearithmic = "O(n log n)";
    public const string Quadratic = "O(n^2)";
    public const string Cubic = "O(n^3)";
    public const string Exponential = "O(2^n)";
    public const string Factorial = "O(n!)";
}

public static class Platforms
{
    public const string LeetCode = "LeetCode";
}
