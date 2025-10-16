using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace DesktopHelper.Models.Services
{
    public class CalendarImportService
    {
        private readonly HttpClient _httpClient;

        public CalendarImportService()
            : this(new HttpClient())
        {
        }

        public CalendarImportService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<List<TaskItem>> ImportFromGoogleCalendarAsync(string calendarUrl)
        {
            if (string.IsNullOrWhiteSpace(calendarUrl))
            {
                throw new ArgumentException("Calendar URL cannot be empty.", nameof(calendarUrl));
            }

            var response = await _httpClient.GetAsync(calendarUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var icsContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return ParseIcsContent(icsContent);
        }

        private List<TaskItem> ParseIcsContent(string icsContent)
        {
            var tasks = new List<TaskItem>();
            if (string.IsNullOrWhiteSpace(icsContent))
            {
                return tasks;
            }

            var lines = UnfoldLines(icsContent);
            bool insideEvent = false;
            string summary = null;
            DateTime? dueDate = null;
            bool hasReminder = false;
            string uid = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    insideEvent = true;
                    summary = null;
                    dueDate = null;
                    hasReminder = false;
                    uid = null;
                    continue;
                }

                if (line.StartsWith("END:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    if (insideEvent)
                    {
                        tasks.Add(new TaskItem
                        {
                            TaskName = string.IsNullOrWhiteSpace(summary) ? "Calendar Event" : summary.Trim(),
                            DueDate = dueDate,
                            HasReminder = hasReminder,
                            ExternalId = string.IsNullOrWhiteSpace(uid) ? null : uid.Trim()
                        });
                    }

                    insideEvent = false;
                    continue;
                }

                if (!insideEvent)
                {
                    continue;
                }

                if (line.StartsWith("SUMMARY", StringComparison.OrdinalIgnoreCase))
                {
                    summary = ExtractValue(line);
                    continue;
                }

                if (line.StartsWith("UID", StringComparison.OrdinalIgnoreCase))
                {
                    uid = ExtractValue(line);
                    continue;
                }

                if (line.StartsWith("DUE", StringComparison.OrdinalIgnoreCase))
                {
                    dueDate = ParseDate(ExtractValue(line));
                    continue;
                }

                if (line.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase) && dueDate == null)
                {
                    dueDate = ParseDate(ExtractValue(line));
                    continue;
                }

                if (line.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase) && dueDate == null)
                {
                    dueDate = ParseDate(ExtractValue(line));
                    continue;
                }

                if (line.StartsWith("BEGIN:VALARM", StringComparison.OrdinalIgnoreCase))
                {
                    hasReminder = true;
                }
            }

            return tasks;
        }

        private static IEnumerable<string> UnfoldLines(string icsContent)
        {
            string[] rawLines = icsContent.Replace("\r\n", "\n").Split('\n');
            string currentLine = null;

            foreach (var rawLine in rawLines)
            {
                var line = rawLine?.TrimEnd('\r');
                if (string.IsNullOrEmpty(line))
                {
                    if (currentLine != null)
                    {
                        yield return currentLine;
                        currentLine = null;
                    }
                    continue;
                }

                if (line.Length > 0 && (line[0] == ' ' || line[0] == '\t'))
                {
                    if (currentLine != null)
                    {
                        currentLine += line.Substring(1);
                    }
                    continue;
                }

                if (currentLine != null)
                {
                    yield return currentLine;
                }

                currentLine = line;
            }

            if (currentLine != null)
            {
                yield return currentLine;
            }
        }

        private static string ExtractValue(string line)
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex < 0 || separatorIndex >= line.Length - 1)
            {
                return string.Empty;
            }

            return line.Substring(separatorIndex + 1).Trim();
        }

        private static DateTime? ParseDate(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            rawValue = rawValue.Trim();
            DateTime parsed;

            if (rawValue.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.TryParseExact(rawValue,
                        new[] { "yyyyMMdd'T'HHmmss'Z'", "yyyyMMdd'T'HHmm'Z'" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out parsed))
                {
                    return parsed;
                }
            }

            if (DateTime.TryParseExact(rawValue,
                    new[] { "yyyyMMdd'T'HHmmss", "yyyyMMdd" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
