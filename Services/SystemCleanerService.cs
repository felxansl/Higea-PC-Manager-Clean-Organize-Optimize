using Organizer.Models;
using System.IO;
using System.Runtime.InteropServices;

namespace Organizer.Services;

public class SystemCleanerService
{
    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);
    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI = 0x00000002;
    private const uint SHERB_NOSOUND = 0x00000004;

    private static readonly string[] TempPaths =
    [
        Path.GetTempPath(),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
    ];

    public async Task<ScanResult> CleanAsync(
        bool cleanTemp,
        bool emptyRecycleBin,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ScanResult();

        await Task.Run(() =>
        {
            if (cleanTemp)
            {
                foreach (var tempPath in TempPaths.Distinct())
                {
                    if (!Directory.Exists(tempPath)) continue;
                    progress?.Report($"{Loc.S("ClnCleaning")} {tempPath}");
                    CleanDirectory(tempPath, result, cancellationToken);
                }
            }

            if (emptyRecycleBin)
            {
                try
                {
                    progress?.Report(Loc.S("ClnRecycleBin"));
                    SHEmptyRecycleBin(IntPtr.Zero, null,
                        SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                    result.Log.Add(new LogEntry { Message = Loc.S("ClnRecycleDone"), Type = LogType.Success });
                }
                catch (Exception ex)
                {
                    result.Log.Add(new LogEntry { Message = Loc.S("ClnRecycleError", ex.Message), Type = LogType.Error });
                }
            }

            if (result.FilesMoved == 0 && !result.HasErrors)
                result.Log.Add(new LogEntry { Message = Loc.S("ClnNoFiles"), Type = LogType.Info });

        }, cancellationToken);

        return result;
    }

    private static void CleanDirectory(string path, ScanResult result, CancellationToken ct)
    {
        try
        {
            foreach (var file in Directory.GetFiles(path))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    File.Delete(file);
                    result.FilesMoved++;
                    result.Log.Add(new LogEntry
                    {
                        Message = Loc.S("ClnFileDeleted", Path.GetFileName(file)),
                        Type = LogType.Success
                    });
                }
                catch
                {
                    result.FilesSkipped++;
                }
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    Directory.Delete(dir, recursive: true);
                    result.Log.Add(new LogEntry
                    {
                        Message = Loc.S("ClnFolderDeleted", Path.GetFileName(dir)),
                        Type = LogType.Success
                    });
                }
                catch
                {
                    result.FilesSkipped++;
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            result.Log.Add(new LogEntry { Message = Loc.S("ClnNoPermissionPath", path), Type = LogType.Warning });
        }
    }
}