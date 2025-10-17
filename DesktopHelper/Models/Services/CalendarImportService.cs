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

        public string CredentialsFilePath => _credentialsFilePath;

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

        public bool CredentialsFileExists()
        {
            return File.Exists(_credentialsFilePath);
        }

        public void CopyCredentialsFile(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("Credentials source path cannot be empty.", nameof(sourcePath));
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("The selected Google credentials file could not be found.", sourcePath);
            }

            using (var validationStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            {
                ValidateCredentialStream(validationStream);
            }

            var destinationFullPath = Path.GetFullPath(_credentialsFilePath);
            var sourceFullPath = Path.GetFullPath(sourcePath);

            if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var destinationDirectory = Path.GetDirectoryName(_credentialsFilePath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourcePath, _credentialsFilePath, overwrite: true);

            if (Directory.Exists(_tokenDirectoryPath))
            {
                Directory.Delete(_tokenDirectoryPath, recursive: true);
            }

            _calendarService = null;
        }

        public bool TryValidateCredentials(out string errorMessage)
        {
            try
            {
                using (var stream = new FileStream(_credentialsFilePath, FileMode.Open, FileAccess.Read))
                {
                    ValidateCredentialStream(stream);
                }

                errorMessage = null;
                return true;
            }
            catch (FileNotFoundException)
            {
                errorMessage = "Google credentials file is missing.";
            }
            catch (InvalidDataException ex)
            {
                errorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                errorMessage = $"Unable to read google-credentials.json ({ex.Message}).";
            }

            return false;
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
            var (defaultTimeMin, defaultTimeMax) = GetNextMonthWindow();
            var effectiveTimeMin = timeMin ?? defaultTimeMin;
            var effectiveTimeMax = timeMax ?? defaultTimeMax;
            request.TimeMin = effectiveTimeMin;
            request.TimeMax = effectiveTimeMax;

            var events = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            var tasks = new List<TaskItem>();

            if (events?.Items == null || events.Items.Count == 0)
            {
                return tasks;
            }

            foreach (var eventItem in events.Items.Where(e => e != null))
            {
                var start = GetDateTimeOffset(eventItem.Start);
                var end = GetDateTimeOffset(eventItem.End);
                var pivot = start ?? end;
                var dueDate = pivot?.LocalDateTime;

                if (pivot.HasValue)
                {
                    var pivotUtc = pivot.Value.UtcDateTime;
                    if (pivotUtc < effectiveTimeMin || pivotUtc >= effectiveTimeMax)
                    {
                        continue;
                    }
                }

                if (!dueDate.HasValue)
                {
                    var fallback = GetDateTime(eventItem.Start) ?? GetDateTime(eventItem.End);
                    dueDate = fallback;
                }

                if (dueDate.HasValue && (dueDate.Value.ToUniversalTime() < effectiveTimeMin || dueDate.Value.ToUniversalTime() >= effectiveTimeMax))
                {
                    continue;
                }

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

        private static (DateTime TimeMin, DateTime TimeMax) GetNextMonthWindow()
        {
            var localNow = DateTime.Now;
            var timeMin = localNow.ToUniversalTime();
            var timeMax = localNow.AddDays(90).ToUniversalTime();

            return (timeMin, timeMax);
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
                    var clientSecrets = LoadClientSecrets(stream);

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

        private static DateTimeOffset? GetDateTimeOffset(EventDateTime eventDateTime)
        {
            if (eventDateTime == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(eventDateTime.DateTimeRaw) &&
                DateTimeOffset.TryParse(eventDateTime.DateTimeRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedRaw))
            {
                return parsedRaw;
            }

            if (eventDateTime.DateTime.HasValue)
            {
                var dateTime = eventDateTime.DateTime.Value;

                if (!string.IsNullOrWhiteSpace(eventDateTime.TimeZone) && dateTime.Kind == DateTimeKind.Unspecified)
                {
                    try
                    {
                        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(eventDateTime.TimeZone);
                        var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
                        var offset = timeZone.GetUtcOffset(unspecified);
                        return new DateTimeOffset(unspecified, offset);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        // Fall back to treating the time as local below.
                    }
                    catch (InvalidTimeZoneException)
                    {
                        // Fall back to treating the time as local below.
                    }
                }

                if (dateTime.Kind == DateTimeKind.Unspecified)
                {
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                }

                return new DateTimeOffset(dateTime);
            }

            if (!string.IsNullOrWhiteSpace(eventDateTime.Date) &&
                DateTime.TryParse(eventDateTime.Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDate))
            {
                var localDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Local);
                return new DateTimeOffset(localDate);
            }

            return null;
        }

        private static ClientSecrets LoadClientSecrets(Stream stream)
        {
            try
            {
                var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

                if (secrets == null ||
                    string.IsNullOrWhiteSpace(secrets.ClientId) ||
                    string.IsNullOrWhiteSpace(secrets.ClientSecret))
                {
                    throw new InvalidDataException("The google-credentials.json file is missing the client_id or client_secret fields.");
                }

                return secrets;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Unable to parse google-credentials.json. Download a new OAuth client secret JSON file and try again.", ex);
            }
        }

        private static void ValidateCredentialStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new InvalidDataException("Unable to read the google-credentials.json file.");
            }

            LoadClientSecrets(stream);

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
        }
    }
}
