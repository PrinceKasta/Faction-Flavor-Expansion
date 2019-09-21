using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{
    class WorldComp_opbase : TimeoutComp
    {
        private bool active=false;

        public override void CompTick()
        {
            if (!active)
                return;
            if (ShouldRemoveWorldObjectNow)
            {
                var threatparms = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain).GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.AnyPlayerHomeMap);
                threatparms.faction = parent.Faction;
                threatparms.raidStrategy = DefDatabase<RaidStrategyDef>.GetRandom();
                threatparms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.AllDefs.First(def => def.defName.Contains("EdgeDropGroups"));
                threatparms.points = Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(Find.AnyPlayerHomeMap)*4,300,15000);
                threatparms.raidNeverFleeIndividual = true;
                IncidentDefOf.RaidEnemy.Worker.TryExecute(threatparms);
                active = false;
                Find.WorldObjects.Remove(parent);
            }
        }

        public void StartComp() => active = true;

        private bool ShouldRemoveWorldObjectNow
        {
            get
            {
                return parent.GetComponent<TimeoutComp>().Passed ? !ParentHasMap : false;
            }
        }
        public override string CompInspectStringExtra() => active ? "ExtraCompString_FOB".Translate(parent.GetComponent<TimeoutComp>().TicksLeft.ToStringTicksToPeriod()) : null;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "opbase_active");
        }
    }
    public class WorldObjectCompProperties_opbase : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_opbase() => compClass = typeof(WorldComp_opbase);
    }
}
