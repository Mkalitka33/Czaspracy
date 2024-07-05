using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public static class RegisterTimeEntry
{
    [FunctionName("RegisterTimeEntry")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Received a POST request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        var startTime = (DateTime?)data?.startTime;
        var endTime = (DateTime?)data?.endTime;
        var taskNumber = (int?)data?.taskNumber;

        if (startTime == null)
        {
            return new BadRequestObjectResult(new { message = "StartTime is required." });
        }

        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        log.LogInformation($"Connection string: {connectionString}");

        if (string.IsNullOrEmpty(connectionString))
        {
            log.LogError("Connection string is null or empty.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var query = "INSERT INTO TimeEntries (TaskNumber, StartTime, EndTime) VALUES (@TaskNumber, @StartTime, @EndTime)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TaskNumber", taskNumber);
                    cmd.Parameters.AddWithValue("@StartTime", startTime);
                    cmd.Parameters.AddWithValue("@EndTime", (object)endTime ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return new OkObjectResult(new { message = "Time entry registered successfully." });
        }
        catch (Exception ex)
        {
            log.LogError($"Could not save task: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
