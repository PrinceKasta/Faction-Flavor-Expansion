using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
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

        public static WorldObjectDef Site_opbase;
        public static WorldObjectDef Dispute_Camp;
        public static WorldObjectDef Dispute_FOB;
        public static WorldObjectDef Roads_Camp;
        public static ThoughtDef Security;
        public static IncidentDef FE_Gift;
        public static IncidentDef FE_Aid;
        public static IncidentDef FE_FactionAdvancment;
        public static IncidentDef FE_OutpostDefender;
        public static IncidentDef FE_SettlementDefender;
        public static IncidentDef FE_JointRaid;
        public static SitePartDef Outpost_defense;
        public static SitePartDef Outpost_opbase;
        public static SitePartDef Outpost_SiteResuce;
        public static RoadDef StoneRoad;


    }
}
