using System.Globalization;
using System.Text.RegularExpressions;

namespace LocalMind.Api.Services.Tools;

public partial class ToolIntentDetector : IToolIntentDetector
{
    public ToolIntent Detect(string message)
    {
        var normalized = Normalize(message);

        if (ContainsAny(normalized, "plan de estudio", "planificar estudio", "organiza mi estudio", "cronograma de estudio", "study plan"))
        {
            return ToolIntent.GenerateStudyPlan;
        }

        if (ContainsAny(normalized, "extrae tareas", "extraer tareas", "lista de tareas", "acciones pendientes", "pendientes", "to do", "todo"))
        {
            return ToolIntent.ExtractTasks;
        }

        if (ContainsAny(normalized, "resume", "resumen", "summarize", "sintetiza", "resumime", "resumir"))
        {
            return ToolIntent.SummarizeText;
        }

        if (ContainsAny(normalized, "calcula", "calculá", "calcular", "cuanto", "cuánto", "operacion", "operación", "porcentaje", "promedio")
            || ArithmeticExpressionRegex().IsMatch(message))
        {
            return ToolIntent.Calculator;
        }

        return ToolIntent.None;
    }

    public bool IsDocumentQuestion(string message)
    {
        var normalized = Normalize(message);

        return ContainsAny(
            normalized,
            "documento",
            "documentos",
            "archivo",
            "pdf",
            "txt",
            "fuente",
            "fuentes",
            "segun el documento",
            "según el documento",
            "en mis apuntes",
            "en el material",
            "contexto cargado",
            "rag");
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(Normalize(value), StringComparison.Ordinal));
    }

    private static string Normalize(string text)
    {
        return text.Trim().ToLower(CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"(?:\d+(?:[\.,]\d+)?\s*[+\-*/^]\s*)+\d+(?:[\.,]\d+)?")]
    private static partial Regex ArithmeticExpressionRegex();
}
