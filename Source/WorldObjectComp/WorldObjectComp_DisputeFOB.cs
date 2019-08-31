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
    

    class WorldComp_DisputeFOB : WorldObjectComp
    {
        // balance
        private static readonly IntRange timerTarget = new IntRange(100, 200);
        private int stopTimer, loop = 0;
        

        private bool active = false;
        Settlement target ,set1,set2;

        public void StartComp(Settlement set1, Settlement set2)
        {
            this.active = true;
            this.set1 = set1;
            this.set2 = set2;
        }

        public override void CompTick()
        {
            if (!active)
                return;
            if(loop==4)
            {
                active = false;
                
                Settlement factionBase = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                factionBase.SetFaction(parent.Faction);
                factionBase.Tile = parent.Tile;
                factionBase.Name = SettlementNameGenerator.GenerateSettlementName(factionBase);
                Find.WorldObjects.Add(factionBase);
                Find.WorldObjects.Remove(parent);
                return;
            }
            stopTimer--;
            base.CompTick();
            if (target == null && NextTarget(out target))
            {
                stopTimer = timerTarget.RandomInRange;
            }
            if (target == null)
            {
                loop = 4;
                return;
            }
            if (stopTimer<=0)
            {
                loop++;
                // balance
                if (Rand.Chance(0.35f))
                {
                    Utilities.FactionsWar().GetByFaction(target.Faction).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE;
                    Utilities.FactionsWar().GetByFaction(parent.Faction).resources += FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE / 1.5f;
                    Find.WorldObjects.Remove(target);
                    Messages.Message("MessageFriendlyAttackSuccess".Translate(target, set1, set2),MessageTypeDefOf.PositiveEvent);
                    target = null;
                    // Not complete
                    
                    return;
                }
                Messages.Message("MessageFriendlyAttackFail".Translate(target,set1, set2), target, MessageTypeDefOf.NeutralEvent);
                target = null;
            }
        }

        private bool NextTarget(out Settlement target)
        {
            if ((from s in Find.WorldObjects.Settlements
                 where Utilities.Reachable(this.parent.Tile, s.Tile, 300) && s.Faction.HostileTo(this.parent.Faction) && s.Spawned
                 select s).TryRandomElement(out target))
            {
                return true;
            }
            target = null;
            return false;
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref stopTimer, "DisputeFOD_stopTimer", defaultValue: 0);
            Scribe_Values.Look(ref loop, "DisputeFOD_loop", defaultValue : 0);
            Scribe_References.Look(ref target, "DisputeFOD_target");
            Scribe_References.Look(ref set1, "DisputeFOD_set1");
            Scribe_References.Look(ref set2, "DisputeFOD_set2");
        }
        public override string CompInspectStringExtra()
        {
            if(active)
                return base.CompInspectStringExtra()+ "DisputeFODdesc".Translate(stopTimer.ToStringTicksToPeriod(), target);
            return (string) null;
        }
    }
    public class WorldObjectCompProperties_DisputeCity : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_DisputeCity()
        {
            this.compClass = typeof(WorldComp_DisputeFOB);
        }
    }
}
