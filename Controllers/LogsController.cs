using System.Net;
using log_viewer_api.Interfaces;
using log_viewer_api.Models;
using log_viewer_api.Services;
using Microsoft.AspNetCore.Mvc;
using MyApp.Models;

namespace log_viewer_api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class LogsController(ILogFileService logFileService) : ControllerBase
{

    [HttpGet]
    public ActionResult<ApiResponse<List<LogFileDto>>> GetFiles([FromQuery]string rootDirectory)
    {
        rootDirectory = WebUtility.UrlDecode(rootDirectory);
        var files = logFileService.GetLogFiles(rootDirectory);
        if (files.Count <= 0)
        {
            // Standardized Error
            return NotFound(ApiResponse<List<LogFileDto>>.Fail($"No files found for {rootDirectory}"));
        }
        return Ok(ApiResponse<List<LogFileDto>>.Success(files, "Files retrieved successfully."));
    }

    [HttpGet]
    public IActionResult HealthCheck()
    {
        return Ok(ApiResponse<object>.Success( "Healthy",  "Healthy"  ));
  
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LogEntryDto>>>> ReadFile([FromQuery] string filePath)
    {
   

        var logs = await logFileService.ReadFile(filePath);
        if (logs.Count <= 0)
        {
            // Standardized Error
            return NotFound(ApiResponse<List<LogFileDto>>.Fail($"No files found for {filePath}"));
        }
        
        return Ok(ApiResponse<List<LogEntryDto>>.Success(logs, "Files read successfully."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<LogEntrySummary>>> GetLogSummary([FromQuery] string filePath)
    {
        var logSummary = await logFileService.GetLogFileSummary(filePath);
        if (logSummary.TotalCount == 0)
        {
            // Standardized Error
            return NotFound(ApiResponse<List<LogFileDto>>.Fail($"No logs found for {filePath}"));
        }
        return Ok(ApiResponse<LogEntrySummary>.Success(logSummary, "File read successfully."));
        
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<LogEntryDto>>>> GetLogsPaged(string filePath, int pageNumber = 1, int pageSize = 50)
    {
   

        var logs = await logFileService.GetLogsPaged(filePath ,  pageNumber, pageSize);
        if (logs.Items.Count <= 0)
        {
            // Standardized Error
            return NotFound(ApiResponse<List<LogFileDto>>.Fail($"No logs found for {filePath}"));
        }
        
        return Ok(ApiResponse<PagedResult<LogEntryDto>>.Success(logs, "File read successfully."));
    }
}