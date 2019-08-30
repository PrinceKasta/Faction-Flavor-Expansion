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
        private IIncidentTarget parms;
        public override void CompTick()
        {
            if (!active)
                return;
            if (this.ShouldRemoveWorldObjectNow)
            {
                SimpleCurve curvePoints = new SimpleCurve();

                var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                var threatparms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.AnyPlayerHomeMap);
                threatparms.faction = this.parent.Faction;
                threatparms.raidStrategy = DefDatabase<RaidStrategyDef>.GetRandom();
                threatparms.raidArrivalMode =(from def in DefDatabase<PawnsArrivalModeDef>.AllDefs
                                             where def.defName.Contains("Groups")
                                             select def).RandomElement();
                threatparms.points = Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(parms)*15,300,15000);
                threatparms.raidNeverFleeIndividual = true;
                IncidentDefOf.RaidEnemy.Worker.TryExecute(threatparms);
            }
            base.CompTick();
        }

        public void StartComp(IIncidentTarget parms)
        {
            active = true;
            this.parms = parms;
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
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "opbase_active");
            Scribe_References.Look(ref parms, "opbase_target");
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
