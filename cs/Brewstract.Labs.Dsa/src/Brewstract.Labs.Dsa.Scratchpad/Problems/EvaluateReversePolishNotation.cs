namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

[On(Platforms.LeetCode, 150)]
public class EvaluateReversePolishNotation
{
    [Solution]
    [Complexity(Time = BigO.Linear, Space = BigO.Linear)]
    public static int EvalRPN(string[] tokens) {
        Stack<int> operands = new();
    
        foreach(var token in tokens)
        {
            switch (token)
            {
                case "+":
                    operands.Push(operands.Pop() + operands.Pop());    
                break;
                case "-": 
                    int subtrahend = operands.Pop();
                    int minuend = operands.Pop();
                    operands.Push(minuend - subtrahend);
                break;
                case "*": 
                    operands.Push(operands.Pop() * operands.Pop());
                break;
                    
                case "/": 
                    int divisor = operands.Pop();
                    int dividend = operands.Pop();
                    operands.Push(dividend / divisor);
                break;
                default:
                    operands.Push(int.Parse(token));
                break;
            }
        }  
        return operands.Pop();  
    }

}
