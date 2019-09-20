using System.Linq;
using Verse;
using RimWorld;


namespace Flavor_Expansion
{
    class FE_MapComp_Skirmish : MapComponent
    {
        private bool active = false;
        private Faction fac1 , fac2;

        public FE_MapComp_Skirmish(Map map) : base(map) => active = false;

        public void StartComp(Faction fac1, Faction fac2)
        {
            active = true;
            this.fac1 = fac1;
            this.fac2 = fac2;
        }
        public bool IsActive() => active;

        public override void MapComponentTick()
        {
            if (!active)
                return;
            
            if(!map.mapPawns.PawnsInFaction(fac2).Any(p => GenHostility.IsActiveThreatTo(p, fac1)))
            {
                Utilities.FactionsWar().GetByFaction(fac2).resources -= FE_WorldComp_FactionsWar.MEDIUM_EVENT_RESOURCE_VALUE;
                active = false;

            }
            if (!map.mapPawns.PawnsInFaction(fac1).Any(p => GenHostility.IsActiveThreatTo(p, fac2)))
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
