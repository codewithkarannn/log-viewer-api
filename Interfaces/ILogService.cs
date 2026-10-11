using log_viewer_api.Models;

namespace log_viewer_api.Interfaces;


public interface ILogFileService
{
    List<LogFileDto> GetLogFiles(string directoryPath);
    Task<List<LogEntryDto>> ReadFile(string filePath);
    List<LogEntryDto> Parse(string text, int startId = 0);
    Task<PagedResult<LogEntryDto>> GetLogsPaged(string directoryPath, int pageNumber = 1, int pageSize = 50);
    Task<LogEntrySummary> GetLogFileSummary(string filePath);
}