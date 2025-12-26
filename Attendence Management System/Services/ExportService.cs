using System.Text;
using Attendence_Management_System.Data;

namespace Attendence_Management_System.Services;

public class ExportService
{
    public (byte[] bytes, string contentType, string fileName) ToCsv(string fileNameBase, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers.Select(EscapeCsv)));

        foreach (var row in rows)
            sb.AppendLine(string.Join(',', row.Select(EscapeCsv)));

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return (bytes, "text/csv; charset=utf-8", $"{fileNameBase}.csv");
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (value.Contains('"')) value = value.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{value}\"" : value;
    }

    public static string StatusToString(AttendanceStatus s) => s.ToString();
}
