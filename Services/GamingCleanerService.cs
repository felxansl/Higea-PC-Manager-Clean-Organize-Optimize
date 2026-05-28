using Organizer.Models;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Organizer.Services;

public class GamingCleanerService
{
    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    private static readonly string[] NvidiaCachePaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\DXCache"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\GLCache"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\OptixCache"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),      @"NVIDIA\ComputeCache"),
        @"C:\ProgramData\NVIDIA Corporation\NV_Cache",
    ];

    private static readonly string PrefetchPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");

    public async Task<GamingResult> OptimizeAsync(
        bool clearNvidiaCache,
        bool clearPrefetch,
        bool freeRam,
        bool setHighPerformance,
        bool flushDns,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new GamingResult();

        await Task.Run(() =>
        {
            if (clearNvidiaCache)
            {
                progress?.Report(Loc.S("GamCleaningNvidia"));
                foreach (var path in NvidiaCachePaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DeleteFolderContents(path, result);
                }
            }

            if (clearPrefetch)
            {
                progress?.Report(Loc.S("GamCleaningPrefetch"));
                DeleteFolderContents(PrefetchPath, result);
            }

            if (freeRam)
            {
                progress?.Report(Loc.S("GamFreeingRam"));
                var freed = FreeWorkingSet(result);
                result.Log.Add(new LogEntry
                {
                    Message = Loc.S("GamRamFreed", freed),
                    Type = LogType.Success
                });
            }

            if (setHighPerformance)
            {
                progress?.Report(Loc.S("GamApplyingPower"));
                SetHighPerformancePlan(result);
            }

            if (flushDns)
            {
                progress?.Report(Loc.S("GamFlushingDns"));
                FlushDns(result);
            }

        }, cancellationToken);

        return result;
    }

    private static void DeleteFolderContents(string folderPath, GamingResult result, bool isRoot = true)
    {
        if (!Directory.Exists(folderPath)) return;

        List<string> files = [];
        try
        {
            files = Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly).ToList();
        }
        catch (UnauthorizedAccessException)
        {
            if (isRoot)
                result.Log.Add(new LogEntry
                {
                    Message = Loc.S("GamNoPermission", Path.GetFileName(folderPath)),
                    Type = LogType.Info
                });
            return;
        }
        catch (Exception ex)
        {
            result.Log.Add(new LogEntry
            {
                Message = Loc.S("GamCacheReadError", Path.GetFileName(folderPath), ex.Message),
                Type = LogType.Error
            });
            return;
        }

        foreach (var file in files)
        {
            try
            {
                var size = new FileInfo(file).Length;
                File.Delete(file);
                result.FilesDeleted++;
                result.BytesFreed += size;
            }
            catch { }
        }

        List<string> subDirs = [];
        try { subDirs = Directory.EnumerateDirectories(folderPath).ToList(); }
        catch { }

        foreach (var dir in subDirs.OrderByDescending(d => d.Length))
        {
            DeleteFolderContents(dir, result, isRoot: false);
            try
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }
            catch { }
        }

        if (isRoot)
            result.Log.Add(new LogEntry
            {
                Message = Loc.S("GamCacheCleared", Path.GetFileName(folderPath)),
                Type = LogType.Success
            });
    }

    private static long FreeWorkingSet(GamingResult result)
    {
        long totalFreedMb = 0;
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var before = process.WorkingSet64;
                EmptyWorkingSet(process.Handle);
                process.Refresh();
                var after = process.WorkingSet64;
                totalFreedMb += Math.Max(0, before - after) / (1024 * 1024);
            }
            catch { }
            finally { process.Dispose(); }
        }
        return totalFreedMb;
    }

    private static void SetHighPerformancePlan(GamingResult result)
    {
        try
        {
            const string highPerfGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = $"/setactive {highPerfGuid}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };
            proc.Start();
            proc.WaitForExit(5000);

            if (proc.ExitCode == 0)
            {
                result.Log.Add(new LogEntry { Message = Loc.S("GamHighPerfOn"), Type = LogType.Success });
            }
            else
            {
                using var proc2 = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = "/setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    }
                };
                proc2.Start();
                proc2.WaitForExit(5000);

                result.Log.Add(new LogEntry
                {
                    Message = proc2.ExitCode == 0 ? Loc.S("GamUltimatePerfOn") : Loc.S("GamPowerFail"),
                    Type = proc2.ExitCode == 0 ? LogType.Success : LogType.Info
                });
            }
        }
        catch (Exception ex)
        {
            result.Log.Add(new LogEntry { Message = Loc.S("GamPowerError", ex.Message), Type = LogType.Error });
        }
    }

    private static void FlushDns(GamingResult result)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                }
            };
            proc.Start();
            proc.WaitForExit(5000);
            result.Log.Add(new LogEntry { Message = Loc.S("GamDnsDone"), Type = LogType.Success });
        }
        catch (Exception ex)
        {
            result.Log.Add(new LogEntry { Message = Loc.S("GamDnsError", ex.Message), Type = LogType.Error });
        }
    }
}