namespace Organizer.Models;

public class GamingResult
{
    public int FilesDeleted { get; set; }
    public long BytesFreed { get; set; }
    public List<LogEntry> Log { get; } = [];

    public string BytesFreedFormatted => BytesFreed switch
    {
        >= 1_073_741_824 => $"{BytesFreed / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{BytesFreed / 1_048_576.0:F1} MB",
        >= 1_024 => $"{BytesFreed / 1_024.0:F1} KB",
        _ => $"{BytesFreed} B"
    };
}