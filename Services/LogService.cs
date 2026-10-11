using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using log_viewer_api.Interfaces;
using log_viewer_api.Models;

namespace log_viewer_api.Services;

public class LogFileService : ILogFileService
{
    private static readonly HashSet<string> ValidLevels =
    [
        "INFO",
        "DEBUG",
        "WARN",
        "WARNING",
        "ERROR",
        "TRACE",
        "FATAL"
    ];

    // The pattern should not ignore case for the level capture group (group 2), 
    // as we normalize it later with ToUpperInvariant.
    private static readonly Regex LogPattern = new(
        @"^(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}\.\d+)\s*\|\s*(INFO|DEBUG|WARN|WARNING|ERROR|TRACE|FATAL)\s*\|\s*([^|]+?)\s*\|\s*(.*)$",
        RegexOptions.Compiled);

    public List<LogFileDto> GetLogFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException();

        return Directory
            .GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories)
            .Select(file =>
            {
                var info = new FileInfo(file);

                return new LogFileDto
                {
                    Name = info.Name,
                    FullPath = info.FullName,
                    Size = info.Length,
                    LastModified = info.LastWriteTime
                };
            })
            .OrderByDescending(x => x.LastModified)
            .ToList();
    }

    /// <summary>
    /// Efficiently reads and parses a log file into a list of DTOs.
    /// </summary>
    private async Task<List<LogEntryDto>> GetParsedEntriesFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"The file {filePath} was not found.");

        // Read the entire file content in one go.
        var fileContent = await File.ReadAllTextAsync(filePath);
        
        // Parse the content into DTOs.
        return Parse(fileContent);
    }


    /// <summary>
    /// Retrieves a specific page of log entries from the file, sorted by timestamp descending.
    /// </summary>
    public async Task<PagedResult<LogEntryDto>> GetLogsPaged(string filePath, int pageNumber = 1, int pageSize = 50)
    {
        // Step 1: Get ALL entries, parsed and sorted.
        // This is the most efficient way to handle multi-line logs, provided
        // the file size fits in memory.
        var allEntries = await GetParsedEntriesFromFileAsync(filePath);
        
        // Step 2: Define pagination constraints.
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;

        var totalItems = allEntries.Count;
        int itemsToSkip = (pageNumber - 1) * pageSize;

        // Step 3: Apply pagination.
        // The order is guaranteed by the Parse method.
        var pagedItems = allEntries
            .Skip(itemsToSkip)
            .Take(pageSize)
            .ToList();

        // Step 4: Return the result.
        return new PagedResult<LogEntryDto>
        {
            Items = pagedItems,
            TotalItems = totalItems
        };
    }

    
    public async Task<LogEntrySummary> GetLogFileSummary(string filePath)
    {
        
        var allEntries = await GetParsedEntriesFromFileAsync(filePath);

       
        var response = new LogEntrySummary
        {
           TotalCount =  allEntries.Count(),
           ErrorCount =  allEntries.Count(i => i.Level == "ERROR"),
           DebugCount =  allEntries.Count(i => i.Level == "DEBUG"),
           InfoCount =   allEntries.Count(i => i.Level == "INFO"),
           WarningCount = allEntries.Count(i => i.Level == "WARN"),
        };

        return response;
    }
    /// <summary>
    /// (Kept for backward compatibility) Reads and parses the entire file.
    /// </summary>
    public async Task<List<LogEntryDto>> ReadFile(string filePath)
    {
        return await GetParsedEntriesFromFileAsync(filePath);
    }


    public List<LogEntryDto> Parse(string text, int startId = 0)
    {
        var entries = new List<LogEntryDto>();

        // StringSplitOptions.RemoveEmptyEntries helps avoid blank DTOs at the end.
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var id = startId;

        // Cross-platform timezone ID
        string timeZoneId = OperatingSystem.IsWindows()
            ? "India Standard Time"
            : "Asia/Kolkata";

        TimeZoneInfo indiaTimeZone;
        try
        {
             indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback to UTC if the specific timezone isn't found on the host system.
            indiaTimeZone = TimeZoneInfo.Utc;
        }


        foreach (var raw in lines)
        {
            // Trim CR for Windows-style line endings read as "\r\n".
            var line = raw.TrimEnd('\r');
            
            // Skip truly empty lines if they snuck past Split.
            if (string.IsNullOrWhiteSpace(line)) continue;

            var match = LogPattern.Match(line);

            if (match.Success)
            {
                var levelStr = match.Groups[2].Value.Trim();

                // Parse UTC timestamp
                var utcTimestamp = DateTime.SpecifyKind(
                    DateTime.Parse(match.Groups[1].Value.Trim()),
                    DateTimeKind.Utc);

                // Convert to target timezone
                var localTimestamp = TimeZoneInfo.ConvertTimeFromUtc(
                    utcTimestamp,
                    indiaTimeZone);

                var level = ValidLevels.Contains(levelStr)
                    ? levelStr
                    : "INFO";

                entries.Add(new LogEntryDto
                {
                    Id = id++,
                    Timestamp = localTimestamp,
                    Level = level,
                    Source = match.Groups[3].Value.Trim(),
                    Message = match.Groups[4].Value.Trim(),
                    Raw = line
                });
            }
            else if (entries.Count > 0)
            {
                // This line is a continuation of the previous log entry (e.g., a stack trace).
                // Append it to the message and raw properties of the *last* entry.
                entries.Last().Message += Environment.NewLine + line;
                entries.Last().Raw += Environment.NewLine + line;
            }
        }

        // Ensure logs are sorted by timestamp descending (newest first).
        return entries
            .OrderByDescending(x => x.Timestamp)
            .ToList();
    }
}