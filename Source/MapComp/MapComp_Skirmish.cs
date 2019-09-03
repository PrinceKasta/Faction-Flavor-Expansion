using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.AI.Group;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;


namespace Flavor_Expansion
{
    class FE_MapComp_Skirmish : MapComponent
    {
        private bool active = false;
        
        Faction fac1 , fac2;

        public FE_MapComp_Skirmish(Map map) : base(map)
        {
            active = false;
        }
        public void StartComp(Faction fac1, Faction fac2)
        {
            this.active = true;
            this.fac1 = fac1;
            this.fac2 = fac2;
        }
        public bool IsActive()
        {
            return active;
        }

        public override void MapComponentTick()
        {
            if (!active || fac1 == null)
                return;
            
            if(map.mapPawns.FreeHumanlikesOfFaction(fac2).Count(p => !p.Dead && !p.Downed) ==0)
            {
                Utilities.FactionsWar().GetByFaction(fac2).resources -= FE_WorldComp_FactionsWar.MEDIUM_EVENT_RESOURCE_VALUE;
                active = false;

            } else if(map.mapPawns.FreeHumanlikesOfFaction(fac1).Count(p => !p.Dead && !p.Downed) == 0)
            {
                Utilities.FactionsWar().GetByFaction(fac1).resources -= FE_WorldComp_FactionsWar.MEDIUM_EVENT_RESOURCE_VALUE;
                active = false;
            }
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref fac1, "Skirmish_fac1");
            Scribe_References.Look(ref fac2, "Skirmish_fac2");
            Scribe_Values.Look(ref active, "Skirmish_active");
        }
    }
}
