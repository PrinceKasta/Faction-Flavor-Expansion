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
    class WorldComp_JointRaid : WorldObjectComp
    {
        private bool active = false;
        private int timer = 0;
        private Faction ally;
        private List<Thing> rewards;
        private Thing Bonus;

        public void StartComp(int stopTime, Faction ally,List<Thing> rewards, Thing silver)
        {
            this.Bonus = silver;
            this.rewards = rewards;
            this.timer = 600;
            this.active = true;
            this.ally = ally;
        }
        public bool IsActive()
        {
            return active;
        }
        public override void CompTick()
        {
            
            base.CompTick();
            if (!active)
                return;
            
            MapParent map = (MapParent)this.parent;
            if (!map.HasMap)
            {
                if (timer <=0)
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelJointRaidFail".Translate(), TranslatorFormattedStringExtensions.Translate("JointRaidFail", ally.leader, parent, parent.Faction)
                        , LetterDefOf.NegativeEvent, null, ally, (string)null);
                    active = false;
                }
                timer--;
                return;
            }
            else
            {
                if(FriendliesDefeated())
                {
                    Bonus.stackCount = 0;
                }
                List<Pawn> pawnList = map.Map.mapPawns.SpawnedPawnsInFaction(parent.Faction);
                for (int index = 0; index < pawnList.Count; ++index)
                {
                    Pawn pawn = pawnList[index];
                    if (pawn.RaceProps.Humanlike && GenHostility.IsActiveThreatToPlayer((IAttackTarget)pawn))
                        return;
                }
                Map target = Find.AnyPlayerHomeMap;
                IntVec3 intVec3 = DropCellFinder.TradeDropSpot(target);
                if(Bonus.stackCount>0)
                    DropPodUtility.DropThingsNear(intVec3, target, new List<Thing>() { Bonus }, 110, false, false, true);
                DropPodUtility.DropThingsNear(intVec3, target, (IEnumerable<Thing>)rewards, 110, false, true, true);
                string reward="";
                foreach(Thing t in rewards)
                {
                    reward += t.Label + "\n";   
                }
                Find.LetterStack.ReceiveLetter("LetterLabelJointRaidSuccess".Translate(), TranslatorFormattedStringExtensions.Translate("JointRaidSuccess", ally.leader) + reward + (Bonus.stackCount >0 ? TranslatorFormattedStringExtensions.Translate("JointRaidSuccessBonus", ally.leader, Bonus.stackCount) : "")
                    , LetterDefOf.PositiveEvent, null, ally, (string)null);
                active = false;
            }
        }
        public override void PostMapGenerate()
        {
            if (!active)
                return;
            base.PostMapGenerate();
            MapParent map = (MapParent)this.parent;
            // Balace
            List<PawnKindDef> kindDefs = new List<PawnKindDef>();
            kindDefs.Add(DefDatabase<PawnKindDef>.GetNamed("Mercenary_Elite"));
            kindDefs.Add(DefDatabase<PawnKindDef>.GetNamed("Town_Guard"));
            kindDefs.Add(DefDatabase<PawnKindDef>.GetNamed("Grenadier_Destructive"));
            Lord lord = LordMaker.MakeNewLord(ally, new LordJob_AssaultColony(ally,false), map.Map);
            IntVec3 vec3;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out vec3, map.Map, 0.2f))
             return;
            Utilities.GenerateFighter(Math.Min(StorytellerUtility.DefaultThreatPointsNow(Find.AnyPlayerHomeMap), 1500),lord,kindDefs,map.Map, ally,vec3);
        }

        private bool FriendliesDefeated()
        {
            MapParent map = (MapParent)this.parent;

            if (map.Map.mapPawns.FreeHumanlikesOfFaction(ally).Count(p => !p.Dead || !p.Downed) <= 0)
                return true;
            return false;
        }

        public override string CompInspectStringExtra()
        {
            if(active)
                return base.CompInspectStringExtra() + "ExtraCompString_JointRaid".Translate();
            return base.CompInspectStringExtra();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "jointraid_active", defaultValue: false);
            Scribe_Values.Look(ref timer, "jointraid_timer", defaultValue : 0);
            Scribe_References.Look(ref ally, "jointraid_ally");
            Scribe_References.Look(ref Bonus, "jointraid_Bonus");
            Scribe_Collections.Look(ref rewards, "jointraid_rewards",LookMode.Reference);
        }
    }

    public class WorldObjectCompProperties_JointRaid : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_JointRaid()
        {
            this.compClass = typeof(WorldComp_JointRaid);
        }
    }
}
