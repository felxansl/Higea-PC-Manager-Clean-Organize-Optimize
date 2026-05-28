using Organizer.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace Organizer.Services;

public class FileOrganizerService
{
    // ── Carpetas del sistema (independiente del idioma de Windows) ──────────
    private static readonly string PathPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    private static readonly string PathVideos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    private static readonly string PathDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private static readonly string PathMusic = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
    private static readonly string PathDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    private static readonly string PathDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    // ── Patrones de detección ───────────────────────────────────────────────
    private static readonly Regex RxIphone = new(@"^IMG_\d{4}\.(jpg|jpeg|heic|png|mov|mp4)$", RegexOptions.IgnoreCase);
    private static readonly Regex RxObs = new(@"^\d{4}-\d{2}-\d{2}\s\d{2}-\d{2}-\d{2}\.(mkv|mp4|flv)$", RegexOptions.IgnoreCase);
    private static readonly Regex RxScreenshot = new(@"(screenshot|captura\sde\spantalla|captura|capture|pantalla)", RegexOptions.IgnoreCase);
    private static readonly Regex RxInvoice = new(@"(invoice|factura|recibo|receipt)", RegexOptions.IgnoreCase);
    private static readonly Regex RxContract = new(@"(contract|contrato|agreement|acuerdo)", RegexOptions.IgnoreCase);
    private static readonly Regex RxResume = new(@"(resume|cv|curriculum|hoja\sde\svida)", RegexOptions.IgnoreCase);

    // ── Extensiones por categoría ───────────────────────────────────────────
    private static readonly HashSet<string> ExtImages = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".tiff", ".ico", ".svg"];
    private static readonly HashSet<string> ExtVideos = [".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v"];
    private static readonly HashSet<string> ExtAudio = [".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a"];
    private static readonly HashSet<string> ExtPdf = [".pdf"];
    private static readonly HashSet<string> ExtWord = [".doc", ".docx", ".odt", ".rtf"];
    private static readonly HashSet<string> ExtSheet = [".xls", ".xlsx", ".csv", ".ods"];
    private static readonly HashSet<string> ExtPpt = [".ppt", ".pptx", ".odp"];
    private static readonly HashSet<string> ExtInstallers = [".exe", ".msi", ".msix", ".appx"];
    private static readonly HashSet<string> ExtCompressed = [".zip", ".rar", ".7z", ".tar", ".gz", ".iso"];
    private static readonly HashSet<string> ExtDocs = [.. ExtPdf, .. ExtWord, .. ExtSheet, .. ExtPpt, ".txt"];

    // ── Subcarpetas válidas conocidas por carpeta del sistema ───────────────
    private static readonly HashSet<string> KnownVideosSubfolders = ["OBS Recordings", "iPhone Videos", "Screen Recordings", "Downloaded Videos", "Captures"];
    private static readonly HashSet<string> KnownPicturesSubfolders = ["iPhone Photos", "Screenshots", "Others"];
    private static readonly HashSet<string> KnownMusicSubfolders = ["Downloaded Music"];

    // ── Carpetas del sistema que NO se tocan como raíz (solo se procesa su contenido) ──
    private static readonly HashSet<string> SystemRoots = new(StringComparer.OrdinalIgnoreCase)
    {
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    };

    // ── Devuelve las carpetas del sistema para el modo "Organizar PC completo" ──
    public static IEnumerable<string> GetSystemFolders() =>
    [
        PathDesktop,
        PathDownloads,
        PathDocuments,
        PathPictures,
        PathVideos,
        PathMusic,
    ];

    // ── Overload para carpeta única (modo "Organizar carpeta específica") ───
    public Task<ScanResult> OrganizeAsync(
        string sourceFolderPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
        => OrganizeAsync([sourceFolderPath], progress, cancellationToken);

    // ── Método principal: acepta múltiples carpetas fuente ──────────────────
    public async Task<ScanResult> OrganizeAsync(
        IEnumerable<string> sourceFolders,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ScanResult();

        await Task.Run(() =>
        {
            // Recolectamos archivos de todas las carpetas fuente
            var allFiles = new List<string>();

            foreach (var folder in sourceFolders)
            {
                if (!Directory.Exists(folder))
                {
                    result.Log.Add(new LogEntry { Message = $"❌ Folder not found: {folder}", Type = LogType.Error });
                    continue;
                }

                var files = GetFilesSafe(folder)
                    .Where(f => !IsAlreadyInFinalDestination(f));

                allFiles.AddRange(files);
            }

            if (allFiles.Count == 0)
            {
                result.Log.Add(new LogEntry { Message = "ℹ️ No files found to organize.", Type = LogType.Info });
                return;
            }

            // ── PASO 0: mover carpetas mal ubicadas en Videos/Pictures/Music ──
            MoveMisplacedFolders(sourceFolders, result, progress, cancellationToken);

            // Clasificamos y agrupamos por destino
            var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = ClassifyFile(file);
                if (destination is null) continue;

                if (!groups.ContainsKey(destination))
                    groups[destination] = [];
                groups[destination].Add(file);
            }

            // Movemos con la regla de mínimo 2 archivos para subcarpeta
            foreach (var (destinationFolder, files) in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var effectiveDestination = destinationFolder;
                if (files.Count == 1 && IsSubFolder(destinationFolder))
                    effectiveDestination = Directory.GetParent(destinationFolder)!.FullName;

                Directory.CreateDirectory(effectiveDestination);

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.FilesProcessed++;

                    var fileName = Path.GetFileName(file);
                    progress?.Report($"Moving: {fileName}");

                    if (Path.GetFullPath(Path.GetDirectoryName(file)!).Equals(
                        Path.GetFullPath(effectiveDestination), StringComparison.OrdinalIgnoreCase))
                    {
                        result.FilesSkipped++;
                        result.Log.Add(new LogEntry { Message = $"⏭️ Already in place: {fileName}", Type = LogType.Info });
                        continue;
                    }

                    try
                    {
                        var dest = GetSafeDestinationPath(effectiveDestination, fileName);
                        File.Move(file, dest);
                        result.FilesMoved++;

                        var subfolder = Path.GetFileName(effectiveDestination);
                        var systemFolder = Path.GetFileName(Directory.GetParent(effectiveDestination)!.FullName);
                        result.Log.Add(new LogEntry
                        {
                            Message = $"✅ {fileName}  →  {systemFolder}\\{subfolder}\\",
                            Type = LogType.Success
                        });
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(ex.Message);
                        result.Log.Add(new LogEntry { Message = $"❌ Error: {fileName} — {ex.Message}", Type = LogType.Error });
                    }
                }
            }

            // Limpiamos carpetas vacías en todas las fuentes
            foreach (var folder in sourceFolders.Where(Directory.Exists))
                CleanEmptyFolders(folder, result);

        }, cancellationToken);

        return result;
    }

    // Carpetas que NUNCA se tocan aunque estén en un lugar "incorrecto"
    private static readonly HashSet<string> NeverMoveFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "My Games", "MyGames",
        "DeSmuME", "melonDS", "RetroArch", "PCSX2", "RPCS3", "Dolphin", "Cemu", "Yuzu", "Ryujinx",
        "Steam", "Epic Games", "GOG", "Ubisoft", "EA",
        "saves", "save", "savegames", "screenshots", "mods", "plugins", "shaders",
    };

    // ── Mover carpetas mal ubicadas en carpetas de media ───────────────────
    // Detecta subcarpetas directas de Videos/Pictures/Music que no son conocidas
    // y las mueve completas a Documents\Others
    private static void MoveMisplacedFolders(
        IEnumerable<string> sourceFolders,
        ScanResult result,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        // Pares: (carpeta del sistema, subcarpetas válidas conocidas)
        var mediaPairs = new[]
        {
            (Root: PathVideos,   Known: KnownVideosSubfolders),
            (Root: PathPictures, Known: KnownPicturesSubfolders),
            (Root: PathMusic,    Known: KnownMusicSubfolders),
        };

        // Solo actuamos si esa carpeta del sistema está entre las fuentes escaneadas
        var sourceSet = new HashSet<string>(
            sourceFolders.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);

        foreach (var (root, known) in mediaPairs)
        {
            if (!sourceSet.Contains(Path.GetFullPath(root))) continue;
            if (!Directory.Exists(root)) continue;

            foreach (var dir in Directory.GetDirectories(root))
            {
                ct.ThrowIfCancellationRequested();

                var name = Path.GetFileName(dir);
                var attrs = File.GetAttributes(dir);

                // Saltar junction points, subcarpetas conocidas y carpetas de juegos
                if (attrs.HasFlag(FileAttributes.ReparsePoint)) continue;
                if (known.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                if (NeverMoveFolders.Contains(name)) continue;

                // Carpeta desconocida → moverla a Documents\Others
                var destRoot = Path.Combine(PathDocuments, "Others");
                Directory.CreateDirectory(destRoot);

                var destPath = GetSafeDestinationFolder(destRoot, name);
                progress?.Report($"Moving folder: {name}");

                try
                {
                    Directory.Move(dir, destPath);
                    result.FilesMoved++;
                    result.Log.Add(new LogEntry
                    {
                        Message = $"📁 {name}\\  →  Documents\\Others\\",
                        Type = LogType.Success
                    });
                }
                catch (Exception ex)
                {
                    result.Errors.Add(ex.Message);
                    result.Log.Add(new LogEntry
                    {
                        Message = $"❌ Error moving folder: {name} — {ex.Message}",
                        Type = LogType.Error
                    });
                }
            }
        }
    }

    // ── Clasificador principal ──────────────────────────────────────────────
    private string? ClassifyFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var ext = Path.GetExtension(filePath).ToLower();

        // ── IMÁGENES ──────────────────────────────────────
        if (ExtImages.Contains(ext))
        {
            if (RxIphone.IsMatch(fileName)) return Sub(PathPictures, "iPhone Photos");
            if (RxScreenshot.IsMatch(fileName)) return Sub(PathPictures, "Screenshots");
            return Sub(PathPictures, "Others");
        }

        // ── VIDEOS ────────────────────────────────────────
        if (ExtVideos.Contains(ext))
        {
            if (RxObs.IsMatch(fileName)) return Sub(PathVideos, "OBS Recordings");
            if (RxIphone.IsMatch(fileName)) return Sub(PathVideos, "iPhone Videos");
            if (RxScreenshot.IsMatch(fileName)) return Sub(PathVideos, "Screen Recordings");
            return Sub(PathVideos, "Downloaded Videos");
        }

        // ── AUDIO ─────────────────────────────────────────
        if (ExtAudio.Contains(ext))
            return Sub(PathMusic, "Downloaded Music");

        // ── DOCUMENTOS (tema primero, luego tipo) ─────────
        if (ExtDocs.Contains(ext))
        {
            if (RxInvoice.IsMatch(fileName)) return Sub(PathDocuments, "Invoices");
            if (RxContract.IsMatch(fileName)) return Sub(PathDocuments, "Contracts");
            if (RxResume.IsMatch(fileName)) return Sub(PathDocuments, "Resume");

            if (ExtPdf.Contains(ext)) return Sub(PathDocuments, "PDFs");
            if (ExtWord.Contains(ext)) return Sub(PathDocuments, "Word Files");
            if (ExtSheet.Contains(ext)) return Sub(PathDocuments, "Spreadsheets");
            if (ExtPpt.Contains(ext)) return Sub(PathDocuments, "Presentations");

            return Sub(PathDocuments, "Others");
        }

        // ── INSTALADORES / COMPRIMIDOS ────────────────────
        if (ExtInstallers.Contains(ext)) return Sub(PathDownloads, "Installers");
        if (ExtCompressed.Contains(ext)) return Sub(PathDownloads, "Compressed");

        return null;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    // Enumera archivos recursivamente saltando carpetas sin permisos
    // (junction points del sistema como Documents\My Music, My Videos, etc.)
    private static IEnumerable<string> GetFilesSafe(string folder)
    {
        IEnumerable<string> files = [];
        try { files = Directory.EnumerateFiles(folder); }
        catch { yield break; }

        foreach (var f in files)
            yield return f;

        IEnumerable<string> subDirs = [];
        try { subDirs = Directory.EnumerateDirectories(folder); }
        catch { yield break; }

        foreach (var dir in subDirs)
        {
            // Saltar junction points / reparse points del sistema
            var attrs = File.GetAttributes(dir);
            if (attrs.HasFlag(FileAttributes.ReparsePoint)) continue;

            foreach (var f in GetFilesSafe(dir))
                yield return f;
        }
    }

    private static string Sub(string systemFolder, string subFolder)
        => Path.Combine(systemFolder, subFolder);

    private static bool IsSubFolder(string path)
    {
        return SystemRoots.Any(sp =>
            path.StartsWith(sp, StringComparison.OrdinalIgnoreCase) &&
            path.Length > sp.Length + 1);
    }

    private bool IsAlreadyInFinalDestination(string filePath)
    {
        var expectedDestFolder = ClassifyFile(filePath);
        if (expectedDestFolder is null) return false;

        var currentFolder = Path.GetFullPath(Path.GetDirectoryName(filePath)!);
        var expectedFolder = Path.GetFullPath(expectedDestFolder);

        return currentFolder.Equals(expectedFolder, StringComparison.OrdinalIgnoreCase);
    }

    private static void CleanEmptyFolders(string rootPath, ScanResult result)
    {
        try
        {
            foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                                         .OrderByDescending(d => d.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                    result.Log.Add(new LogEntry
                    {
                        Message = $"🗑️ Empty folder removed: {Path.GetFileName(dir)}\\",
                        Type = LogType.Info
                    });
                }
            }
        }
        catch { }
    }

    private static string GetSafeDestinationPath(string folder, string fileName)
    {
        var dest = Path.Combine(folder, fileName);
        if (!File.Exists(dest)) return dest;

        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var i = 1;
        do { dest = Path.Combine(folder, $"{name} ({i++}){ext}"); }
        while (File.Exists(dest));
        return dest;
    }

    private static string GetSafeDestinationFolder(string parentFolder, string folderName)
    {
        var dest = Path.Combine(parentFolder, folderName);
        if (!Directory.Exists(dest)) return dest;

        var i = 1;
        do { dest = Path.Combine(parentFolder, $"{folderName} ({i++})"); }
        while (Directory.Exists(dest));
        return dest;
    }
}