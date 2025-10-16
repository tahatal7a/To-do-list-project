using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopHelper.Models.Services
{
    public class CalendarImportService
    {
        private static readonly string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        private const string ApplicationName = "Desktop Task Aid";

        private readonly string _credentialsFilePath;
        private readonly string _tokenDirectoryPath;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        private CalendarService _calendarService;

        public CalendarImportService()
            : this("google-credentials.json", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoogleCalendarTokens"))
        {
        }

        public CalendarImportService(string credentialsFilePath, string tokenDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(credentialsFilePath))
            {
                throw new ArgumentException("Credentials file path cannot be empty.", nameof(credentialsFilePath));
            }

            _credentialsFilePath = Path.IsPathRooted(credentialsFilePath)
                ? credentialsFilePath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, credentialsFilePath);

            _tokenDirectoryPath = string.IsNullOrWhiteSpace(tokenDirectoryPath)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoogleCalendarTokens")
                : (Path.IsPathRooted(tokenDirectoryPath)
                    ? tokenDirectoryPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, tokenDirectoryPath));
        }

        public async Task<List<TaskItem>> ImportFromGoogleCalendarAsync(
            string calendarId = "primary",
            DateTime? timeMin = null,
            DateTime? timeMax = null,
            CancellationToken cancellationToken = default)
        {
            await EnsureServiceAsync(cancellationToken).ConfigureAwait(false);

            var request = _calendarService.Events.List(string.IsNullOrWhiteSpace(calendarId) ? "primary" : calendarId);
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 2500;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            request.TimeMin = timeMin ?? DateTime.UtcNow.AddMonths(-1);
            request.TimeMax = timeMax ?? DateTime.UtcNow.AddYears(1);

            var events = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            var tasks = new List<TaskItem>();

            if (events?.Items == null || events.Items.Count == 0)
            {
                return tasks;
            }

            foreach (var eventItem in events.Items.Where(e => e != null))
            {
                var start = GetDateTime(eventItem.Start);
                var end = GetDateTime(eventItem.End);
                var dueDate = start ?? end;

                bool hasReminder = eventItem.Reminders?.UseDefault == true;

                if (eventItem.Reminders?.Overrides != null)
                {
                    foreach (var reminder in eventItem.Reminders.Overrides)
                    {
                        if (string.Equals(reminder?.Method, "popup", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(reminder?.Method, "email", StringComparison.OrdinalIgnoreCase))
                        {
                            hasReminder = true;
                            break;
                        }
                    }
                }

                tasks.Add(new TaskItem
                {
                    TaskName = string.IsNullOrWhiteSpace(eventItem.Summary) ? "Calendar Event" : eventItem.Summary.Trim(),
                    DueDate = dueDate,
                    HasReminder = hasReminder,
                    ExternalId = string.IsNullOrWhiteSpace(eventItem.Id) ? null : eventItem.Id.Trim()
                });
            }

            return tasks;
        }

        private async Task EnsureServiceAsync(CancellationToken cancellationToken)
        {
            if (_calendarService != null)
            {
                return;
            }

            await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_calendarService != null)
                {
                    return;
                }

                if (!File.Exists(_credentialsFilePath))
                {
                    throw new FileNotFoundException("Google API credentials file not found.", _credentialsFilePath);
                }

                Directory.CreateDirectory(_tokenDirectoryPath);

                using (var stream = new FileStream(_credentialsFilePath, FileMode.Open, FileAccess.Read))
                {
                    var clientSecrets = (await GoogleClientSecrets.FromStream(stream).ConfigureAwait(false)).Secrets;

                    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        clientSecrets,
                        Scopes,
                        "user",
                        cancellationToken,
                        new FileDataStore(_tokenDirectoryPath, true)).ConfigureAwait(false);

                    _calendarService = new CalendarService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName
                    });
                }
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private static DateTime? GetDateTime(EventDateTime eventDateTime)
        {
            if (eventDateTime == null)
            {
                return null;
            }

            if (eventDateTime.DateTime.HasValue)
            {
                return eventDateTime.DateTime.Value;
            }

            if (!string.IsNullOrWhiteSpace(eventDateTime.Date) &&
                DateTime.TryParse(eventDateTime.Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
