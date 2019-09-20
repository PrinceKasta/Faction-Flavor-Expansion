using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    

    class WorldComp_DisputeFOB : WorldObjectComp
    {
        // balance
        private static readonly IntRange timerTarget = new IntRange(3, 6);
        private int stopTimer, loop = 0;
        private bool active = false;
        private Settlement target ,set1,set2;

        public void StartComp(Settlement set1, Settlement set2)
        {
            active = true;
            this.set1 = set1;
            this.set2 = set2;
            stopTimer = timerTarget.RandomInRange * Global.DayInTicks + Find.TickManager.TicksGame;
            NextTarget(out target);
        }

        public override void CompTick()
        {
            if (!active)
                return;
            if(loop>=4)
            {
                active = false;
                Settlement factionBase = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                factionBase.SetFaction(parent.Faction);
                factionBase.Tile = parent.Tile;
                factionBase.Name = SettlementNameGenerator.GenerateSettlementName(factionBase);
                Find.WorldObjects.Remove(parent);
                Find.WorldObjects.Add(factionBase);
                return;
            }
            if (target == null)
            {
                loop = 4;
                return;
            }
            if (stopTimer <= Find.TickManager.TicksGame)
            {
                loop++;
                if (!NextTarget(out target))
                {
                    loop = 4;
                }
                stopTimer = timerTarget.RandomInRange * Global.DayInTicks + Find.TickManager.TicksGame;

                if (Rand.Chance(0.35f))
                {
                    Utilities.FactionsWar().GetByFaction(target.Faction).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE;
                    Utilities.FactionsWar().GetByFaction(parent.Faction).resources += FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE / 1.5f;
                    Find.WorldObjects.Remove(target);
                    Messages.Message("MessageFriendlyAttackSuccess".Translate(target, set1, set2),MessageTypeDefOf.PositiveEvent);
                    return;
                }
                Messages.Message("MessageFriendlyAttackFail".Translate(target,set1, set2), target, MessageTypeDefOf.NeutralEvent);
                Utilities.FactionsWar().GetByFaction(parent.Faction).resources -= FE_WorldComp_FactionsWar.MINOR_EVENT_RESOURCE_VALUE;
            }
        }

        private bool NextTarget(out Settlement target) => Find.WorldObjects.Settlements.Where(s => Utilities.Reachable(parent.Tile, s.Tile, 300) && s.Faction.HostileTo(parent.Faction) && s.Spawned).TryRandomElement(out target)
                ? true
                : false;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "DisputeFOD_active", defaultValue: false);
            Scribe_Values.Look(ref stopTimer, "DisputeFOD_stopTimer", defaultValue: 0);
            Scribe_Values.Look(ref loop, "DisputeFOD_loop", defaultValue : 0);
            Scribe_References.Look(ref target, "DisputeFOD_target");
            Scribe_References.Look(ref set1, "DisputeFOD_set1");
            Scribe_References.Look(ref set2, "DisputeFOD_set2");
        }
        public override string CompInspectStringExtra() => active ? base.CompInspectStringExtra() + "DisputeFODdesc".Translate((stopTimer - Find.TickManager.TicksGame).ToStringTicksToPeriod(), target) : null;
    }
    public class WorldObjectCompProperties_DisputeCity : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_DisputeCity() => compClass = typeof(WorldComp_DisputeFOB);
    }
}
