using Verse;
using UnityEngine;


namespace Flavor_Expansion
{
    class FlavorExpansion : Mod
    {
        public FlavorExpansion(ModContentPack content) : base(content)
        {
            GetSettings<EndGame_Settings>();
            Log.Message("[End Game] loaded...");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect: inRect);
            GetSettings<EndGame_Settings>().DoWindowContents(rect: inRect);
        }

        public override string SettingsCategory() => "Faction Expansion";

        public override void WriteSettings() => base.WriteSettings();
    }

    static class Global
    {
        public const int DayInTicks = 60000;
        public const int YearInTicks = 3600000;
    }
}
