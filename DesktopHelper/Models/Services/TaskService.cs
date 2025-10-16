using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

namespace DesktopHelper.Models.Services
{
    public class TaskService
    {
        private readonly string _filePath = "tasks.json";

        public async Task<List<TaskItem>> LoadFromFileAsync()
        {
            if (!File.Exists(_filePath))
                return new List<TaskItem>();

            string json = await Task.Run(() => File.ReadAllText(_filePath));
            Debug.WriteLine($"Loaded JSON: {json}");

            var options = new JsonSerializerOptions
            {
                Converters = { new CustomDateTimeConverter("yyyy-MM-ddTHH:mm:ss.fffZ") }
            };

            return JsonSerializer.Deserialize<List<TaskItem>>(json, options) ?? new List<TaskItem>();
        }

        public async Task SaveToFileAsync(List<TaskItem> tasks)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new CustomDateTimeConverter("yyyy-MM-ddTHH:mm:ss.fffZ") }
            };

            string json = JsonSerializer.Serialize(tasks, options);
            Debug.WriteLine($"Saving JSON: {json}");
            await Task.Run(() => File.WriteAllText(_filePath, json));
        }

        public async Task AddTaskAsync(TaskItem newTask)
        {
            var tasks = await LoadFromFileAsync();
            tasks.Add(newTask);
            await SaveToFileAsync(tasks);
        }

        public async Task EditTaskAsync(TaskItem updatedTask)
        {
            var tasks = await LoadFromFileAsync();
            var taskIndex = tasks.FindIndex(t => t.TaskName == updatedTask.TaskName);
            if (taskIndex != -1)
            {
                tasks[taskIndex] = updatedTask;
                await SaveToFileAsync(tasks);
            }
        }

        public async Task DeleteTaskAsync(string taskName)
        {
            var tasks = await LoadFromFileAsync();
            var taskToRemove = tasks.Find(t => t.TaskName == taskName);
            if (taskToRemove != null)
            {
                tasks.Remove(taskToRemove);
                await SaveToFileAsync(tasks);
            }
        }

        public async Task<TaskItem> GetTaskAsync(string taskName)
        {
            var tasks = await LoadFromFileAsync();
            return tasks.Find(t => t.TaskName == taskName);
        }
    }

    public class CustomDateTimeConverter : JsonConverter<DateTime?>
    {
        private readonly string _dateFormat;

        public CustomDateTimeConverter(string dateFormat)
        {
            _dateFormat = dateFormat;
        }

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && DateTime.TryParseExact(reader.GetString(), _dateFormat, null, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                return date;
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToString(_dateFormat));
        }
    }
}
