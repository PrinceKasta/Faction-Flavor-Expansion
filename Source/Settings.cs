using UnityEngine;
using Verse;


namespace Flavor_Expansion
{

    class EndGame_Settings : ModSettings
    {
        public static bool FactionWar = true;
        public static bool FactionExpansion = true;
        public static bool FactionHistory = true;
        public static bool FactionServitude = true;
        public static bool Bombardment = true;
        public static bool JointRaid = true;
        public static bool SettlementDefense = true;
        public static bool SiteDefender = true;
        public static bool Advancement = true;
        public static bool Aid = true;
        public static bool Gift = true;
        public static bool WarAftermath = true;
        public static float MassiveBattles = 0;

        public void DoWindowContents(Rect rect)
        {
            Listing_Standard options = new Listing_Standard();
            options.Begin(rect: rect);
            options.Gap();
            options.CheckboxLabeled("FE_WarCheckBox".Translate(), ref FactionWar);
            options.Gap();
            if(FactionWar)
                options.CheckboxLabeled("FE_WarCheckBoxAfterman".Translate(), ref WarAftermath);
            options.Gap();
            options.CheckboxLabeled("FE_ServitudeCheckBox".Translate(), ref FactionServitude);
            options.Gap();
            options.CheckboxLabeled("FE_HistoryCheckBox".Translate(), ref FactionHistory, "FE_HistoryCheckBoxDesc".Translate());
            options.Gap();
            options.CheckboxLabeled("FE_ExpansionCheckBox".Translate(), ref FactionExpansion, "FE_ExpansionCheckBoxDesc".Translate());
            options.Gap();
            options.CheckboxLabeled("FE_BombardmentCheckBox".Translate(), ref Bombardment);
            options.Gap();
            options.CheckboxLabeled("FE_JointRaidCheckBox".Translate(), ref JointRaid);
            options.Gap();
            options.CheckboxLabeled("FE_SettlementDefenseCheckBox".Translate(), ref SettlementDefense);
            options.Gap();
            options.CheckboxLabeled("FE_SiteDefenderCheckBox".Translate(), ref SiteDefender);
            options.Gap();
            options.CheckboxLabeled("FE_AdvancementCheckBox".Translate(), ref Advancement);
            options.Gap();
            options.CheckboxLabeled("FE_AidCheckBox".Translate(), ref Aid);
            options.Gap();
            options.CheckboxLabeled("FE_GiftCheckBox".Translate(), ref Gift);
            options.Gap();

            if(FactionWar)
            {
                options.Label("FE_MercenaryBattles".Translate(MassiveBattles));
                MassiveBattles = options.Slider(MassiveBattles, 0, 10000);
            }
            
            Mod.GetSettings<EndGame_Settings>().Write();
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(value: ref FactionHistory, label: "FactionHistory", defaultValue: true);
            Scribe_Values.Look(value: ref WarAftermath, label: "WarAftermath", defaultValue: true);
            Scribe_Values.Look(value: ref FactionWar, label: "FactionWar", defaultValue: true);
            Scribe_Values.Look(value: ref FactionServitude, label: "FactionServitude", defaultValue: true);
            Scribe_Values.Look(value: ref FactionExpansion, label: "FactionExpansion", defaultValue: true);
            Scribe_Values.Look(value: ref Bombardment, label: "Bombardment", defaultValue: true);
            Scribe_Values.Look(value: ref JointRaid, label: "JointRaid", defaultValue: true);
            Scribe_Values.Look(value: ref SettlementDefense, label: "SettlementDefense", defaultValue: true);
            Scribe_Values.Look(value: ref SiteDefender, label: "SiteDefender", defaultValue: true);
            Scribe_Values.Look(value: ref Advancement, label: "Advancement", defaultValue: true);
            Scribe_Values.Look(value: ref Aid, label: "Aid", defaultValue: true);
            Scribe_Values.Look(value: ref Gift, label: "Gift", defaultValue: true);
            Scribe_Values.Look(value: ref MassiveBattles, label: "MassiveBattles", defaultValue: 0);
        }
    }
}
