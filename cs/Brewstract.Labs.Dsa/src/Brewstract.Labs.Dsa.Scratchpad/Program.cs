using System.Runtime.CompilerServices;
using System.Text;

Console.WriteLine("Hello, World!");


string ReverseOf(string x)
{
    StringBuilder buffer = new(capacity: x.Length);
    for (int i = x.Length - 1; i >= 0; i--)
    {
        buffer.Append(x[i]);
    }

    return buffer.ToString();
}