namespace Organizer.Models;

public class ScanResult
{
    public int FilesProcessed { get; set; }
    public int FilesMoved { get; set; }
    public int FilesSkipped { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<LogEntry> Log { get; set; } = [];
    public bool HasErrors => Errors.Count > 0;

    public override string ToString() =>
        $"Procesados: {FilesProcessed} | Movidos: {FilesMoved} | Omitidos: {FilesSkipped}";
}

public class LogEntry
{
    public string Message { get; set; } = string.Empty;
    public LogType Type { get; set; } = LogType.Info;
}

public enum LogType { Info, Success, Warning, Error }