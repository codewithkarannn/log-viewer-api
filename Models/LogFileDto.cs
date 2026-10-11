namespace log_viewer_api.Models;

public class LogFileDto
{
    public string Name { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public long Size { get; set; }

    public DateTime LastModified { get; set; }
}


public class LogEntryDto {
    public int Id { get; set; }
   
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } =  string.Empty;
    public string Source { get; set; }= string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Raw { get; set; }

}
public class LogEntrySummary {

    public int ErrorCount { get; set; }
    public int InfoCount { get; set; }
    public int WarningCount { get; set; }
    public int DebugCount { get; set; }
    public int TotalCount { get; set; }
   
   

}


// Define a simple class to hold the results and total count
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalItems { get; set; }
}