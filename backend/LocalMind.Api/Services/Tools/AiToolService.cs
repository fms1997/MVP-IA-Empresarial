using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalMind.Api.Services.Tools;

public partial class AiToolService : IAiToolService
{
    public Task<ToolExecutionResult?> TryExecuteAsync(
        ToolIntent intent,
        string message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ToolExecutionResult? result = intent switch
        {
            ToolIntent.Calculator => ExecuteCalculator(message),
            ToolIntent.SummarizeText => ExecuteSummarizeText(message),
            ToolIntent.ExtractTasks => ExecuteExtractTasks(message),
            ToolIntent.GenerateStudyPlan => ExecuteGenerateStudyPlan(message),
            _ => null
        };

        return Task.FromResult(result);
    }

    private static ToolExecutionResult ExecuteCalculator(string message)
    {
        var expression = ExtractArithmeticExpression(message);

        if (expression is null)
        {
            var studyNumbers = ExtractStudyNumbers(message);
            if (studyNumbers.TotalPages.HasValue && studyNumbers.PagesPerDay.HasValue)
            {
                var days = Math.Ceiling(studyNumbers.TotalPages.Value / studyNumbers.PagesPerDay.Value);
                return new ToolExecutionResult
                {
                    ToolName = "calculator",
                    Response = $"Resultado con calculator:\n\nSi tenés que estudiar {studyNumbers.TotalPages.Value:N0} páginas y leés {studyNumbers.PagesPerDay.Value:N0} páginas por día, necesitás aproximadamente {days:N0} días.",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["totalPages"] = studyNumbers.TotalPages.Value,
                        ["pagesPerDay"] = studyNumbers.PagesPerDay.Value,
                        ["days"] = days
                    }
                };
            }

            return new ToolExecutionResult
            {
                ToolName = "calculator",
                Response = "Puedo usar calculator, pero necesito una operación clara. Ejemplo: `120 / 20` o `si tengo 120 páginas y leo 20 páginas por día`.",
                Metadata = new Dictionary<string, object?>
                {
                    ["needsMoreData"] = true
                }
            };
        }

        try
        {
            var value = ArithmeticEvaluator.Evaluate(expression);
            return new ToolExecutionResult
            {
                ToolName = "calculator",
                Response = $"Resultado con calculator:\n\n`{expression}` = **{FormatNumber(value)}**",
                Metadata = new Dictionary<string, object?>
                {
                    ["expression"] = expression,
                    ["result"] = value
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                ToolName = "calculator",
                Response = $"No pude resolver la operación con calculator: {ex.Message}",
                Metadata = new Dictionary<string, object?>
                {
                    ["expression"] = expression,
                    ["error"] = ex.Message
                }
            };
        }
    }

    private static ToolExecutionResult ExecuteSummarizeText(string message)
    {
        var text = ExtractPayload(message);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ToolExecutionResult
            {
                ToolName = "summarizeText",
                Response = "Puedo resumir texto, pero necesitás pegar el contenido después de dos puntos o en una línea nueva.",
                Metadata = new Dictionary<string, object?>
                {
                    ["needsMoreData"] = true
                }
            };
        }

        var sentences = SentenceRegex()
            .Matches(text)
            .Cast<Match>()
            .Select(match => match.Value.Trim())
            .Where(sentence => sentence.Length > 0)
            .Take(4)
            .ToList();

        if (sentences.Count == 0)
        {
            sentences = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(4)
                .ToList();
        }

        var summary = sentences.Count == 0
            ? text[..Math.Min(text.Length, 450)]
            : string.Join(" ", sentences);

        return new ToolExecutionResult
        {
            ToolName = "summarizeText",
            Response = $"Resumen con summarizeText:\n\n{summary}",
            Metadata = new Dictionary<string, object?>
            {
                ["inputCharacters"] = text.Length,
                ["summaryCharacters"] = summary.Length
            }
        };
    }

    private static ToolExecutionResult ExecuteExtractTasks(string message)
    {
        var text = ExtractPayload(message);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ToolExecutionResult
            {
                ToolName = "extractTasks",
                Response = "Puedo extraer tareas, pero necesito el texto de origen después de dos puntos o en una línea nueva.",
                Metadata = new Dictionary<string, object?>
                {
                    ["needsMoreData"] = true
                }
            };
        }

        var candidates = text.Split(new[] { '\n', '.', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tasks = candidates
            .Where(IsTaskLike)
            .Select(CleanTask)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        if (tasks.Count == 0)
        {
            tasks = candidates.Take(6).Select(CleanTask).Where(task => task.Length > 0).ToList();
        }

        var builder = new StringBuilder("Tareas detectadas con extractTasks:\n");
        for (var index = 0; index < tasks.Count; index++)
        {
            builder.AppendLine($"{index + 1}. {tasks[index]}");
        }

        return new ToolExecutionResult
        {
            ToolName = "extractTasks",
            Response = builder.ToString().TrimEnd(),
            Metadata = new Dictionary<string, object?>
            {
                ["tasksCount"] = tasks.Count
            }
        };
    }

    private static ToolExecutionResult ExecuteGenerateStudyPlan(string message)
    {
        var numbers = ExtractStudyNumbers(message);
        var totalPages = numbers.TotalPages ?? numbers.FirstNumber;
        var pagesPerDay = numbers.PagesPerDay;
        var availableDays = numbers.Days;

        if (!totalPages.HasValue)
        {
            return new ToolExecutionResult
            {
                ToolName = "generateStudyPlan",
                Response = "Puedo generar un plan de estudio, pero necesito al menos el total de páginas/temas. Ejemplo: `Generá un plan para 120 páginas leyendo 20 por día`.",
                Metadata = new Dictionary<string, object?>
                {
                    ["needsMoreData"] = true
                }
            };
        }

        if (!pagesPerDay.HasValue && !availableDays.HasValue)
        {
            pagesPerDay = 20;
        }

        var days = availableDays ?? Math.Max(1, (int)Math.Ceiling(totalPages.Value / pagesPerDay!.Value));
        var dailyPages = pagesPerDay ?? Math.Ceiling(totalPages.Value / days);
        var plan = BuildStudyPlan(totalPages.Value, dailyPages, days);

        return new ToolExecutionResult
        {
            ToolName = "generateStudyPlan",
            Response = plan,
            Metadata = new Dictionary<string, object?>
            {
                ["totalPages"] = totalPages.Value,
                ["pagesPerDay"] = dailyPages,
                ["days"] = days
            }
        };
    }

    private static string BuildStudyPlan(double totalPages, double pagesPerDay, int days)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Plan generado con generateStudyPlan:");
        builder.AppendLine();
        builder.AppendLine($"Objetivo: estudiar {totalPages:N0} páginas en {days:N0} días ({pagesPerDay:N0} páginas/día aprox.).");
        builder.AppendLine();

        var currentPage = 1;
        for (var day = 1; day <= Math.Min(days, 14); day++)
        {
            var endPage = (int)Math.Min(totalPages, Math.Ceiling(day * pagesPerDay));
            builder.AppendLine($"Día {day}: páginas {currentPage:N0}-{endPage:N0} + 10 minutos de repaso.");
            currentPage = endPage + 1;
        }

        if (days > 14)
        {
            builder.AppendLine($"... continuá el mismo ritmo hasta el día {days:N0}.");
        }

        builder.AppendLine();
        builder.AppendLine("Recomendación: cada 3 días reservá 20-30 minutos para repasar dudas y rehacer ejercicios clave.");
        return builder.ToString().TrimEnd();
    }

    private static bool IsTaskLike(string text)
    {
        var normalized = text.Trim().ToLower(CultureInfo.InvariantCulture);
        return TaskKeywordRegex().IsMatch(normalized);
    }

    private static string CleanTask(string text)
    {
        return text.Trim().Trim('-', '*', '•', '[', ']', ' ').Trim();
    }

    private static string? ExtractArithmeticExpression(string message)
    {
        var match = ArithmeticExpressionRegex().Match(message.Replace(',', '.'));
        return match.Success ? match.Value.Replace(" ", string.Empty) : null;
    }

    private static string ExtractPayload(string message)
    {
        var colonIndex = message.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex >= 0 && colonIndex < message.Length - 1)
        {
            return message[(colonIndex + 1)..].Trim();
        }

        var lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Length > 1 ? string.Join("\n", lines.Skip(1)) : string.Empty;
    }

    private static StudyNumbers ExtractStudyNumbers(string message)
    {
        double? totalPages = null;
        double? pagesPerDay = null;
        int? days = null;

        var normalized = message.ToLower(CultureInfo.InvariantCulture);

        foreach (Match match in PagesPerDayRegex().Matches(normalized))
        {
            pagesPerDay = ParseDouble(match.Groups[1].Value);
        }

        var pageValues = TotalPagesRegex()
            .Matches(normalized)
            .Cast<Match>()
            .Select(match => ParseDouble(match.Groups[1].Value))
            .ToList();

        if (pageValues.Count > 0)
        {
            totalPages = pageValues.Max();
        }

        foreach (Match match in DaysRegex().Matches(normalized))
        {
            days = (int)Math.Ceiling(ParseDouble(match.Groups[1].Value));
        }

        var firstNumberMatch = NumberRegex().Match(normalized);
        var firstNumber = firstNumberMatch.Success ? ParseDouble(firstNumberMatch.Value) : (double?)null;

        return new StudyNumbers(totalPages, pagesPerDay, days, firstNumber);
    }

    private static double ParseDouble(string value)
    {
        return double.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"(?:\d+(?:[\.,]\d+)?\s*[+\-*/^]\s*)+\d+(?:[\.,]\d+)?")]
    private static partial Regex ArithmeticExpressionRegex();

    [GeneratedRegex(@"[^.!?\n]+[.!?]?")]
    private static partial Regex SentenceRegex();

    [GeneratedRegex(@"\b(hacer|crear|preparar|enviar|revisar|terminar|llamar|comprar|investigar|definir|implementar|corregir|actualizar|entregar|organizar|agendar|resolver|estudiar|leer|todo|pendiente)\b")]
    private static partial Regex TaskKeywordRegex();

    [GeneratedRegex(@"(\d+(?:[\.,]\d+)?)\s*(?:paginas|páginas|pages)\s*(?:por|/)\s*(?:dia|día|day)")]
    private static partial Regex PagesPerDayRegex();

    [GeneratedRegex(@"(\d+(?:[\.,]\d+)?)\s*(?:paginas|páginas|pages)")]
    private static partial Regex TotalPagesRegex();

    [GeneratedRegex(@"(\d+(?:[\.,]\d+)?)\s*(?:dias|días|days)")]
    private static partial Regex DaysRegex();

    [GeneratedRegex(@"\d+(?:[\.,]\d+)?")]
    private static partial Regex NumberRegex();

    private sealed record StudyNumbers(double? TotalPages, double? PagesPerDay, int? Days, double? FirstNumber);
}
