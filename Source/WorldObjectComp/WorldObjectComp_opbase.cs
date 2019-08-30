using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using System.Reflection;
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
            if (this.ShouldRemoveWorldObjectNow)
            {
                var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                var threatparms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.AnyPlayerHomeMap);
                threatparms.faction = this.parent.Faction;
                threatparms.raidStrategy = DefDatabase<RaidStrategyDef>.GetRandom();
                threatparms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.AllDefs.First(def => def.defName.Contains("EdgeDropGroups"));
                threatparms.points = Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(Find.AnyPlayerHomeMap)*4,300,15000);
                threatparms.raidNeverFleeIndividual = true;
                IncidentDefOf.RaidEnemy.Worker.TryExecute(threatparms);
                active = false;
                Find.WorldObjects.Remove(this.parent);
            }
        }

        public void StartComp()
        {
            active = true;
        }
        private bool ShouldRemoveWorldObjectNow
        {
            get
            {
                if (this.parent.GetComponent<TimeoutComp>().Passed)
                    return !this.ParentHasMap;
                return false;
            }
        }
        public override string CompInspectStringExtra()
        {
            if (active)
                return "ExtraCompString_FOB".Translate((NamedArgument)this.parent.GetComponent<TimeoutComp>().TicksLeft.ToStringTicksToPeriod());
            return (string)null;
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "opbase_active");
        }

    }
    public class WorldObjectCompProperties_opbase : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_opbase()
        {
            this.compClass = typeof(WorldComp_opbase);
        }
    }
}
