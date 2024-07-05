using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public static class GetTimeEntry
{
    [FunctionName("GetTimeEntry")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Received a GET request.");

        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        log.LogInformation($"Connection string: {connectionString}");

        if (string.IsNullOrEmpty(connectionString))
        {
            log.LogError("Connection string is null or empty.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        try
        {
            var tasks = new List<TimeEntry>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var query = "SELECT TaskNumber, StartTime, EndTime FROM TimeEntries";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var task = new TimeEntry
                            {
                                TaskNumber = reader.GetInt32(0),
                                StartTime = reader.GetDateTime(1),
                                EndTime = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2)
                            };

                            if (task.EndTime.HasValue)
                            {
                                task.Duration = (task.EndTime.Value - task.StartTime).TotalHours.ToString("F2") + " hours";
                            }
                            else
                            {
                                task.Duration = "task not finished";
                            }

                            tasks.Add(task);
                        }
                    }
                }
            }

            return new OkObjectResult(tasks);
        }
        catch (Exception ex)
        {
            log.LogError($"Could not retrieve tasks: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    public class TimeEntry
    {
        public int TaskNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Duration { get; set; }
    }
}
