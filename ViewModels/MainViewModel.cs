using Organizer.Models;
using Organizer.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace Organizer.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly FileOrganizerService _organizer = new();
    private readonly SystemCleanerService _cleaner = new();
    private readonly GamingCleanerService _gaming = new();

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    // ── Organizador ────────────────────────────────────────────────────────
    private string _sourcePath = string.Empty;
    public string SourcePath
    {
        get => _sourcePath;
        set { SetProperty(ref _sourcePath, value); OnPropertyChanged(nameof(CanOrganizeFolder)); }
    }

    private bool _isOrganizing;
    public bool IsOrganizing
    {
        get => _isOrganizing;
        set
        {
            SetProperty(ref _isOrganizing, value);
            OnPropertyChanged(nameof(CanOrganizeFolder));
            OnPropertyChanged(nameof(CanOrganizePC));
        }
    }

    private string _organizerStatus = Loc.S("OrgDefaultStatus");
    public string OrganizerStatus
    {
        get => _organizerStatus;
        set => SetProperty(ref _organizerStatus, value);
    }

    private int _filesMoved;
    public int FilesMoved { get => _filesMoved; set => SetProperty(ref _filesMoved, value); }

    private int _filesSkipped;
    public int FilesSkipped { get => _filesSkipped; set => SetProperty(ref _filesSkipped, value); }

    public bool CanOrganizeFolder => !string.IsNullOrWhiteSpace(SourcePath) && !IsOrganizing;
    public bool CanOrganizePC => !IsOrganizing;

    public ObservableCollection<LogEntry> OrganizerLog { get; } = [];

    // ── Limpiador ─────────────────────────────────────────────────────────
    private bool _cleanTemp = true;
    public bool CleanTemp { get => _cleanTemp; set => SetProperty(ref _cleanTemp, value); }

    private bool _emptyRecycleBin = true;
    public bool EmptyRecycleBin { get => _emptyRecycleBin; set => SetProperty(ref _emptyRecycleBin, value); }

    private bool _isCleaning;
    public bool IsCleaning
    {
        get => _isCleaning;
        set { SetProperty(ref _isCleaning, value); OnPropertyChanged(nameof(CanClean)); }
    }

    private string _cleanerStatus = Loc.S("ClnDefaultStatus");
    public string CleanerStatus { get => _cleanerStatus; set => SetProperty(ref _cleanerStatus, value); }

    public bool CanClean => !IsCleaning && (CleanTemp || EmptyRecycleBin);

    public ObservableCollection<LogEntry> CleanerLog { get; } = [];

    // ── Gaming ─────────────────────────────────────────────────────────────
    private bool _gamingNvidiaCache = true;
    public bool GamingNvidiaCache { get => _gamingNvidiaCache; set => SetProperty(ref _gamingNvidiaCache, value); }

    private bool _gamingPrefetch = true;
    public bool GamingPrefetch { get => _gamingPrefetch; set => SetProperty(ref _gamingPrefetch, value); }

    private bool _gamingFreeRam = true;
    public bool GamingFreeRam { get => _gamingFreeRam; set => SetProperty(ref _gamingFreeRam, value); }

    private bool _gamingHighPerf = true;
    public bool GamingHighPerf { get => _gamingHighPerf; set => SetProperty(ref _gamingHighPerf, value); }

    private bool _gamingFlushDns = true;
    public bool GamingFlushDns { get => _gamingFlushDns; set => SetProperty(ref _gamingFlushDns, value); }

    private bool _isOptimizing;
    public bool IsOptimizing
    {
        get => _isOptimizing;
        set { SetProperty(ref _isOptimizing, value); OnPropertyChanged(nameof(CanOptimize)); }
    }

    private string _gamingStatus = Loc.S("GamDefaultStatus");
    public string GamingStatus { get => _gamingStatus; set => SetProperty(ref _gamingStatus, value); }

    private int _gamingFilesDeleted;
    public int GamingFilesDeleted { get => _gamingFilesDeleted; set => SetProperty(ref _gamingFilesDeleted, value); }

    private string _gamingBytesFreed = "0 B";
    public string GamingBytesFreed { get => _gamingBytesFreed; set => SetProperty(ref _gamingBytesFreed, value); }

    public bool CanOptimize => !IsOptimizing &&
        (GamingNvidiaCache || GamingPrefetch || GamingFreeRam || GamingHighPerf || GamingFlushDns);

    public ObservableCollection<LogEntry> GamingLog { get; } = [];

    // ── Comandos ──────────────────────────────────────────────────────────
    public RelayCommand BrowseFolderCommand { get; }
    public RelayCommand OrganizeFolderCommand { get; }
    public RelayCommand OrganizePCCommand { get; }
    public RelayCommand CleanCommand { get; }
    public RelayCommand OptimizeGamingCommand { get; }
    public RelayCommand ClearOrganizerLogCommand { get; }
    public RelayCommand ClearCleanerLogCommand { get; }
    public RelayCommand ClearGamingLogCommand { get; }

    public MainViewModel()
    {
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        OrganizeFolderCommand = new RelayCommand(async () => await OrganizeFolderAsync(), () => CanOrganizeFolder);
        OrganizePCCommand = new RelayCommand(async () => await OrganizePCAsync(), () => CanOrganizePC);
        CleanCommand = new RelayCommand(async () => await CleanAsync(), () => CanClean);
        OptimizeGamingCommand = new RelayCommand(async () => await OptimizeGamingAsync(), () => CanOptimize);
        ClearOrganizerLogCommand = new RelayCommand(() => OrganizerLog.Clear());
        ClearCleanerLogCommand = new RelayCommand(() => CleanerLog.Clear());
        ClearGamingLogCommand = new RelayCommand(() => GamingLog.Clear());
    }

    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Loc.S("OrgTargetFolder")
        };
        if (dialog.ShowDialog() == true)
            SourcePath = dialog.FolderName;
    }

    private async Task OrganizeFolderAsync()
    {
        await RunOrganizeAsync(
            sources: [SourcePath],
            startStatus: Loc.S("OrgStartFolder", SourcePath));
    }

    private async Task OrganizePCAsync()
    {
        var confirm = MessageBox.Show(
            Loc.S("OrgConfirmPC"),
            Loc.S("OrgConfirmPCTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        await RunOrganizeAsync(
            sources: FileOrganizerService.GetSystemFolders(),
            startStatus: Loc.S("OrgStartPC"));
    }

    private async Task RunOrganizeAsync(IEnumerable<string> sources, string startStatus)
    {
        IsOrganizing = true;
        OrganizerLog.Clear();
        OrganizerStatus = startStatus;
        FilesMoved = 0;
        FilesSkipped = 0;

        var progress = new Progress<string>(msg => OrganizerStatus = msg);

        try
        {
            var result = await _organizer.OrganizeAsync(sources, progress);

            foreach (var entry in result.Log)
                OrganizerLog.Add(entry);

            FilesMoved = result.FilesMoved;
            FilesSkipped = result.FilesSkipped;
            OrganizerStatus = result.FilesMoved == 0
                ? Loc.S("OrgDoneNone")
                : Loc.S("OrgDone", result.FilesMoved);
        }
        catch (Exception ex)
        {
            OrganizerLog.Add(new LogEntry { Message = $"❌ {ex.Message}", Type = LogType.Error });
            OrganizerStatus = Loc.S("OrgError");
        }
        finally
        {
            IsOrganizing = false;
        }
    }

    private async Task CleanAsync()
    {
        var confirm = MessageBox.Show(
            Loc.S("ClnConfirm"),
            Loc.S("ClnConfirmTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        IsCleaning = true;
        CleanerLog.Clear();
        CleanerStatus = Loc.S("ClnCleaning");

        var progress = new Progress<string>(msg => CleanerStatus = msg);

        try
        {
            var result = await _cleaner.CleanAsync(CleanTemp, EmptyRecycleBin, progress);

            foreach (var entry in result.Log)
                CleanerLog.Add(entry);

            CleanerStatus = Loc.S("ClnDone", result.FilesMoved);
        }
        catch (Exception ex)
        {
            CleanerLog.Add(new LogEntry { Message = $"❌ {ex.Message}", Type = LogType.Error });
            CleanerStatus = Loc.S("ClnError");
        }
        finally
        {
            IsCleaning = false;
        }
    }

    private async Task OptimizeGamingAsync()
    {
        IsOptimizing = true;
        GamingLog.Clear();
        GamingStatus = Loc.S("GamStarting");
        GamingFilesDeleted = 0;
        GamingBytesFreed = "0 B";

        var progress = new Progress<string>(msg => GamingStatus = msg);

        try
        {
            var result = await _gaming.OptimizeAsync(
                GamingNvidiaCache,
                GamingPrefetch,
                GamingFreeRam,
                GamingHighPerf,
                GamingFlushDns,
                progress);

            foreach (var entry in result.Log)
                GamingLog.Add(entry);

            GamingFilesDeleted = result.FilesDeleted;
            GamingBytesFreed = result.BytesFreedFormatted;
            GamingStatus = Loc.S("GamDone", result.FilesDeleted, result.BytesFreedFormatted);
        }
        catch (Exception ex)
        {
            GamingLog.Add(new LogEntry { Message = $"❌ {ex.Message}", Type = LogType.Error });
            GamingStatus = Loc.S("GamError");
        }
        finally
        {
            IsOptimizing = false;
        }
    }
}