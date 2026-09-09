$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'source-tools.ps1')
$entry = Get-Content (Join-Path $root '1.6/Source/ModEntry.cs') -Raw
$draw = Get-CSharpMethod $entry 'DrawSettings'
$bridge = Get-Content (Join-Path $root '1.6/Source/ModEntry.IrisMenus.cs') -Raw
$stub = @'
namespace Verse {
    public class Mod { }
    public class Listing_Standard { public void Label(string text) { } }
    public static class ModsConfig {
        public static bool Active;
        public static bool IsActive(string id) { return Active && id == "Nanaloveyuki.IrisMenus"; }
    }
    public static class GenTypes {
        public static System.Type Registry;
        public static int Lookups;
        public static System.Type GetTypeInAnyAssembly(string name) {
            Lookups++;
            if (name != "IrisMenus.MenuRegistry") throw new System.Exception(name);
            return Registry;
        }
    }
    public static class Log {
        public static int Warnings, Errors;
        public static void Warning(string text) { Warnings++; }
        public static void Error(string text) { Errors++; }
    }
    public static class Translator { public static string Translate(this string text) { return text; } }
}
namespace IrisMenus {
    public static class MenuRegistry {
        public static readonly System.Collections.Generic.List<string> Ids = new System.Collections.Generic.List<string>();
        public static bool Fail;
        public static void RegisterSubItemListing(Verse.Mod owner, string id, System.Func<string> title,
            System.Action<Verse.Listing_Standard> draw, System.Action save) {
            if (Fail) throw new System.InvalidOperationException("test registration failure");
            if (Ids.Contains(id)) throw new System.Exception("duplicate page");
            if (title() != "MouseDisaster_IrisMenus_" + id.Substring(9)) throw new System.Exception("title mismatch");
            Ids.Add(id);
            draw(new Verse.Listing_Standard());
            save();
        }
    }
}
namespace MouseDisaster {
    public partial class MouseDisasterMod : Verse.Mod {
        private class FakeSettings {
            public bool enableExperimentalTailBite, enableExperimentalIdentityInheritance;
            public bool enableChaosRoomPregnancy = true;
            public void ClampValues() { }
        }
        private static FakeSettings Settings = new FakeSettings();
        private static readonly System.Collections.Generic.List<string> Calls = new System.Collections.Generic.List<string>();
        private static int Saves;
        private static void SaveSettings() { Saves++; }
        private static void DrawSafetySettings(Verse.Listing_Standard list) { Calls.Add("Safety"); }
        private static void DrawGeneralSettings(Verse.Listing_Standard list, bool legacy) { Calls.Add("General:" + legacy); }
        private static void DrawIncidentSection(Verse.Listing_Standard list, bool? original = null) { Calls.Add("Incidents:" + original); }
        private static void DrawNarrativeControls(Verse.Listing_Standard list, bool hosted = false) { Calls.Add("Narrative:" + hosted); }
        private static void DrawEndingSettings(Verse.Listing_Standard list, bool hosted) { Calls.Add("Endings:" + hosted); }
        private static void DrawBiologySettings(Verse.Listing_Standard list, bool legacy) { Calls.Add("Biology:" + legacy); }
        private static void DrawSettingsFooter(Verse.Listing_Standard list) { Calls.Add("Footer"); }
        private static void DrawCheckbox(Verse.Listing_Standard list, string key, ref bool value, string tip) { Calls.Add(key); }
        private static void Check(bool value, string message) { if (!value) throw new System.Exception(message); }
        public static void Verify() {
            var mod = new MouseDisasterMod();
            mod.RegisterIrisMenus();
            Check(Verse.GenTypes.Lookups == 0, "inactive provider must not be resolved");
            Verse.ModsConfig.Active = true;
            mod.RegisterIrisMenus();
            Check(Verse.Log.Warnings == 1, "missing provider warning");
            Verse.GenTypes.Registry = typeof(string);
            mod.RegisterIrisMenus();
            Check(Verse.Log.Warnings == 2, "incompatible API warning");
            Verse.GenTypes.Registry = typeof(IrisMenus.MenuRegistry);
            mod.RegisterIrisMenus();
            Check(string.Join(",", IrisMenus.MenuRegistry.Ids) == "settings.Safety,settings.OriginalEvents,settings.ContinuedEvents,settings.Endings,settings.General,settings.Experimental", "stable six subitems");
            Check(Saves == 6, "save callbacks");
            Check(string.Join(",", Calls) == "Safety,Footer,Incidents:True,Incidents:False,Narrative:True,Endings:True,General:False,Biology:False,MouseDisaster_Settings_EnableExperimentalTailBite,MouseDisaster_Settings_EnableExperimentalIdentityInheritance", "hosted category dispatch");
            Calls.Clear();
            DrawSettings(new Verse.Listing_Standard());
            Check(string.Join(",", Calls) == "Safety,General:True,Incidents:,Narrative:False,Endings:False,Biology:True,Footer", "legacy drawing order");
            IrisMenus.MenuRegistry.Fail = true;
            mod.RegisterIrisMenus();
            Check(Verse.Log.Errors == 1, "registration failure must be reported");
        }
__DRAW__
    }
}
'@
Add-Type -TypeDefinition ($bridge + "`n" + $stub.Replace('__DRAW__', $draw))
[MouseDisaster.MouseDisasterMod]::Verify()
foreach ($language in @('ChineseSimplified', 'English')) {
    [xml]$xml = Get-Content (Join-Path $root "Languages/$language/Keyed/MouseDisasterIrisMenus.xml") -Raw
    foreach ($page in @('Safety', 'OriginalEvents', 'ContinuedEvents', 'Endings', 'General', 'Experimental')) {
        if (!$xml.LanguageData.("MouseDisaster_IrisMenus_" + $page)) { throw "Missing title: $language/$page" }
    }
}
Write-Host 'IrisMenus integration checks passed (engine doubles, category dispatch, legacy order, localization).'
