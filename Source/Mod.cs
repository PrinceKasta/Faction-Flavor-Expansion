using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;


namespace Flavor_Expansion
{
    class FlavorExpansion : Mod
    {
        
        public FlavorExpansion(ModContentPack content) : base(content)
        {
            LongEventHandler.QueueLongEvent(new Action(Init), "LibraryStartup", false, null);
            this.GetSettings<EndGame_Settings>();
            Log.Message("[End Game] loaded...");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect: inRect);
            this.GetSettings<EndGame_Settings>().DoWindowContents(rect: inRect);
        }

        public override string SettingsCategory() => "Faction Expansion";

        public override void WriteSettings()
        {
            base.WriteSettings();
        }

        private static StringBuilder tmpSettleFailReason = new StringBuilder();
        
        private static void Init()
        {

        }
    }

    static class Global
    {
        public const int DayInTicks = 60000;
        public const int YearInTicks = 3600000;
    }
}
