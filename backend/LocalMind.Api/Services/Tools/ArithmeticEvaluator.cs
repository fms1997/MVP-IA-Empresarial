using System.Globalization;

namespace LocalMind.Api.Services.Tools;

internal static class ArithmeticEvaluator
{
	public static double Evaluate(string expression)
	{
		var output = ToReversePolishNotation(Tokenize(expression));
		var stack = new Stack<double>();

		foreach (var token in output)
		{
			if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
			{
				stack.Push(number);
				continue;
			}

			if (stack.Count < 2)
			{
				throw new InvalidOperationException("La expresión matemática no es válida.");
			}

			var right = stack.Pop();
			var left = stack.Pop();
			stack.Push(token switch
			{
				"+" => left + right,
				"-" => left - right,
				"*" => left * right,
				"/" => right == 0 ? throw new DivideByZeroException("No se puede dividir por cero.") : left / right,
				"^" => Math.Pow(left, right),
				_ => throw new InvalidOperationException("Operador no soportado.")
			});
		}

		if (stack.Count != 1)
		{
			throw new InvalidOperationException("La expresión matemática no es válida.");
		}

		return stack.Pop();
	}

	private static IReadOnlyList<string> Tokenize(string expression)
	{
		var tokens = new List<string>();
		var index = 0;

		while (index < expression.Length)
		{
			var current = expression[index];
			if (char.IsWhiteSpace(current))
			{
				index++;
				continue;
			}

			if (char.IsDigit(current) || current == '.')
			{
				var start = index;
				while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.'))
				{
					index++;
				}

				tokens.Add(expression[start..index]);
				continue;
			}

			if (IsOperator(current) || current is '(' or ')')
			{
				if (current == '-' && (tokens.Count == 0 || IsOperator(tokens[^1][0]) || tokens[^1] == "("))
				{
					tokens.Add("0");
				}

				tokens.Add(current.ToString());
				index++;
				continue;
			}

			throw new InvalidOperationException("La expresión contiene caracteres no soportados.");
		}

		return tokens;
	}

	private static IReadOnlyList<string> ToReversePolishNotation(IReadOnlyList<string> tokens)
	{
		var output = new List<string>();
		var operators = new Stack<string>();

		foreach (var token in tokens)
		{
			if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
			{
				output.Add(token);
				continue;
			}

			if (token == "(")
			{
				operators.Push(token);
				continue;
			}

			if (token == ")")
			{
				while (operators.Count > 0 && operators.Peek() != "(")
				{
					output.Add(operators.Pop());
				}

				if (operators.Count == 0)
				{
					throw new InvalidOperationException("Paréntesis desbalanceados.");
				}

				operators.Pop();
				continue;
			}

			while (operators.Count > 0
				&& operators.Peek() != "("
				&& ShouldPopOperator(operators.Peek(), token))
			{
				output.Add(operators.Pop());
			}

			operators.Push(token);
		}

		while (operators.Count > 0)
		{
			var operatorToken = operators.Pop();
			if (operatorToken == "(")
			{
				throw new InvalidOperationException("Paréntesis desbalanceados.");
			}

			output.Add(operatorToken);
		}

		return output;
	}

	private static bool ShouldPopOperator(string stackOperator, string incomingOperator)
	{
		var stackPrecedence = GetPrecedence(stackOperator);
		var incomingPrecedence = GetPrecedence(incomingOperator);

		return IsRightAssociative(incomingOperator)
			? stackPrecedence > incomingPrecedence
			: stackPrecedence >= incomingPrecedence;
	}

	private static int GetPrecedence(string operatorToken)
	{
		return operatorToken switch
		{
			"+" or "-" => 1,
			"*" or "/" => 2,
			"^" => 3,
			_ => 0
		};
	}

	private static bool IsRightAssociative(string operatorToken)
	{
		return operatorToken == "^";
	}

	private static bool IsOperator(char value)
	{
		return value is '+' or '-' or '*' or '/' or '^';
	}
}
