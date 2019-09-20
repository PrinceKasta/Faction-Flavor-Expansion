using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
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
        private List<Thing> rewards = new List<Thing>();
        private Thing Bonus = new Thing();

        public WorldComp_JointRaid()
        {
            rewards = new List<Thing>();
            Bonus = new Thing();
        }

        public void StartComp(int stopTime, Faction ally,List<Thing> rewards, Thing silver)
        {
            Bonus = silver;
            this.rewards = rewards;
            timer = stopTime + Find.TickManager.TicksGame;
            active = true;
            this.ally = ally;
        }
        public bool IsActive => active;

        public override void CompTick()
        {
            if (!active)
                return;

            if (!((MapParent)parent).HasMap)
            {
                if (timer <= Find.TickManager.TicksGame)
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelJointRaidFail".Translate(), TranslatorFormattedStringExtensions.Translate("JointRaidFail", ally.leader, parent, parent.Faction, ally.def.leaderTitle)
                        , LetterDefOf.NegativeEvent, null, ally);
                    active = false;
                }
                if (parent.GetComponent<EnterCooldownComp>().Active)
                {
                    active = false;
                    if (Bonus.stackCount > 0)
                    {
                        ally.TryAffectGoodwillWith(Faction.OfPlayer, -25);
                        Find.LetterStack.ReceiveLetter("LetterLabelJointRaidAbandoned".Translate(), TranslatorFormattedStringExtensions.Translate("JointRaidAbandoned", ally.def.leaderTitle, ally.leader), LetterDefOf.NegativeEvent);
                    }
                }
                return;
            }
            else
            {
                if(FriendliesDefeated)
                {
                    Bonus.stackCount = 0;
                }
                if (!EnemiesDefeated)
                    return;
                if (Bonus.stackCount>0)
                {
                    rewards.Add(Bonus);
                }

                DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap), Find.AnyPlayerHomeMap, rewards, 110, false, true, true);

                Find.LetterStack.ReceiveLetter("LetterLabelJointRaidSuccess".Translate(), TranslatorFormattedStringExtensions.Translate("JointRaidSuccess", ally.leader) + GenLabel.ThingsLabel(rewards,string.Empty) + (Bonus.stackCount > 0 ? "\n\n"+TranslatorFormattedStringExtensions.Translate("JointRaidSuccessBonus", ally.leader) : "")
                    , LetterDefOf.PositiveEvent, null, ally, null);
                active = false;
            }
        }
        public override void PostMapGenerate()
        {
            if (!active)
                return;

            MapParent map = (MapParent)parent;
            // Balance
            List<PawnKindDef> kindDefs = new List<PawnKindDef>
            {
                DefDatabase<PawnKindDef>.GetNamed("Mercenary_Elite"),
                DefDatabase<PawnKindDef>.GetNamed("Town_Guard"),
                DefDatabase<PawnKindDef>.GetNamed("Grenadier_Destructive")
            };
            Lord lord = LordMaker.MakeNewLord(ally, new LordJob_AssaultColony(ally,false), map.Map);
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 vec3, map.Map, 0.2f))
             return;
            Utilities.GenerateFighter(Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(Find.AnyPlayerHomeMap),400, 1500),lord,kindDefs,map.Map, ally,vec3);
        }

        private bool FriendliesDefeated => !((MapParent)parent).Map.mapPawns.SpawnedPawnsInFaction(ally).Any(p => p.RaceProps.Humanlike && GenHostility.IsActiveThreatTo(p, parent.Faction));

        private bool EnemiesDefeated => !((MapParent)parent).Map.mapPawns.SpawnedPawnsInFaction(parent.Faction).Any(pawn => pawn.RaceProps.Humanlike && GenHostility.IsActiveThreatToPlayer(pawn));
        

        public override string CompInspectStringExtra() => active
                ? base.CompInspectStringExtra() + "ExtraCompString_JointRaid".Translate((timer- Find.TickManager.TicksGame).ToStringTicksToPeriod())
                : base.CompInspectStringExtra();

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "jointraid_active", defaultValue: false);
            Scribe_Values.Look(ref timer, "jointraid_timer", defaultValue: 0);
            if (!active)
                return;
            Scribe_References.Look(ref ally, "jointraid_ally");
            Scribe_Deep.Look(ref Bonus, "jointraid_Bonus");
            Scribe_Collections.Look(ref rewards, "jointraid_rewards", LookMode.Deep);

        }
    }

    public class WorldObjectCompProperties_JointRaid : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_JointRaid() => compClass = typeof(WorldComp_JointRaid);
    }
}
