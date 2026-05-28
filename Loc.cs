using System.Globalization;
using System.Resources;

namespace Organizer;

/// <summary>
/// Central localization helper.
/// - Loc.S("Key")        → localized string (use in C# code)
/// - Loc.UI.KeyName      → static property (use in XAML via {x:Static loc:Loc+UI.KeyName})
/// Falls back to English for any language that is not Spanish.
/// </summary>
public static class Loc
{
    internal static readonly ResourceManager Rm =
        new ResourceManager("Organizer.Strings", typeof(Loc).Assembly);

    static Loc()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        CultureInfo.CurrentUICulture = lang == "es"
            ? new CultureInfo("es")
            : CultureInfo.InvariantCulture;
    }

    /// <summary>Localized string — for use in C# code.</summary>
    public static string S(string key)
        => Rm.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    /// <summary>Formatted localized string — for use in C# code.</summary>
    public static string S(string key, params object[] args)
        => string.Format(S(key), args);

    // ── Static properties for XAML {x:Static} bindings ──────────────────────
    // Each property just calls S() so everything stays in the .resx files.
    public static class UI
    {
        // Sidebar
        public static string AppSubtitle => Loc.S("AppSubtitle");
        public static string NavTools => Loc.S("NavTools");
        public static string NavOrganizer => Loc.S("NavOrganizer");
        public static string NavCleaner => Loc.S("NavCleaner");
        public static string NavGaming => Loc.S("NavGaming");

        // Log panel
        public static string LogTitle => Loc.S("LogTitle");
        public static string LogSubtitle => Loc.S("LogSubtitle");
        public static string LogClear => Loc.S("LogClear");

        // Organizer
        public static string OrgTitle => Loc.S("OrgTitle");
        public static string OrgSubtitle => Loc.S("OrgSubtitle");
        public static string OrgTargetFolder => Loc.S("OrgTargetFolder");
        public static string OrgNoFolder => Loc.S("OrgNoFolder");
        public static string OrgBrowse => Loc.S("OrgBrowse");
        public static string OrgStatMoved => Loc.S("OrgStatMoved");
        public static string OrgStatSkipped => Loc.S("OrgStatSkipped");
        public static string OrgStatStatus => Loc.S("OrgStatStatus");
        public static string OrgActions => Loc.S("OrgActions");
        public static string OrgBtnFolder => Loc.S("OrgBtnFolder");
        public static string OrgBtnPC => Loc.S("OrgBtnPC");
        public static string OrgOrganizing => Loc.S("OrgOrganizing");

        // Cleaner
        public static string ClnTitle => Loc.S("ClnTitle");
        public static string ClnSubtitle => Loc.S("ClnSubtitle");
        public static string ClnWhatToClean => Loc.S("ClnWhatToClean");
        public static string ClnTempFiles => Loc.S("ClnTempFiles");
        public static string ClnRecycleBin => Loc.S("ClnRecycleBin");
        public static string ClnStatStatus => Loc.S("ClnStatStatus");
        public static string ClnAction => Loc.S("ClnAction");
        public static string ClnBtnClean => Loc.S("ClnBtnClean");
        public static string ClnCleaning => Loc.S("ClnCleaning");

        // Gaming
        public static string GamTitle => Loc.S("GamTitle");
        public static string GamSubtitle => Loc.S("GamSubtitle");
        public static string GamStatDeleted => Loc.S("GamStatDeleted");
        public static string GamStatFreed => Loc.S("GamStatFreed");
        public static string GamStatStatus => Loc.S("GamStatStatus");
        public static string GamCleanup => Loc.S("GamCleanup");
        public static string GamNvidiaCache => Loc.S("GamNvidiaCache");
        public static string GamPrefetch => Loc.S("GamPrefetch");
        public static string GamFreeRam => Loc.S("GamFreeRam");
        public static string GamPerformance => Loc.S("GamPerformance");
        public static string GamHighPerf => Loc.S("GamHighPerf");
        public static string GamFlushDns => Loc.S("GamFlushDns");
        public static string GamAction => Loc.S("GamAction");
        public static string GamBtnOptimize => Loc.S("GamBtnOptimize");
        public static string GamOptimizing => Loc.S("GamOptimizing");

        // TitleBar
        public static string TitleOrganizer => Loc.S("TitleOrganizer");
        public static string TitleCleaner => Loc.S("TitleCleaner");
        public static string TitleGaming => Loc.S("TitleGaming");
    }
}