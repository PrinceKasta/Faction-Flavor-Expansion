using RimWorld;
using Verse;

namespace Flavor_Expansion
{
    [DefOf]
    public static class EndGameDefOf
    {
        static EndGameDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(EndGameDefOf));
        }
        public static RulePackDef FE_History;
        public static RulePackDef FE_WarEvent_ArtifactCache;
        public static RulePackDef FE_WarEvent_Raid;
        public static WorldObjectDef Site_opbase;
        public static WorldObjectDef Dispute_Camp;
        public static WorldObjectDef Roads_Camp;
        public static IncidentDef FE_JointRaid;
        public static SitePartDef Outpost_defense;
        public static SitePartDef Outpost_opbase;
        public static SitePartDef Outpost_SiteResuce;
        public static SiteCoreDef BattleLocation;
        public static RoadDef StoneRoad;
        public static ThingDef Bullet_Shell_HighExplosive;
    }
}
