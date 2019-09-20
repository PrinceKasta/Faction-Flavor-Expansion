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
    class WorldComp_SiteDefense : WorldObjectComp
    {
        public FloatRange pointsRange = new FloatRange(50f, 300f);

        private bool active = false;
        private int stopTime=0;
        private IIncidentTarget parms;
        private bool survivors = true;
        private Faction enemy;
        private List<Thing> rewards;

        public void StartComp(int stopTime, IncidentParms parms, Faction enemy, List<Thing> rewards)
        {
            this.rewards = rewards;
            this.parms = parms.target;
            this.stopTime = stopTime + Find.TickManager.TicksGame;
            active = true;
            this.enemy = enemy;
        }
        public bool IsActive()
        {
            return active;
        }

        public override void CompTick()
        {
            if (!active)
                return;
            // Player let the countdown finish, ignored quest.
            if (!((MapParent)parent).HasMap)
            {
                if (Find.TickManager.TicksGame >= stopTime)
                {
                    CreateOpbase();
                    Find.WorldObjects.Remove(parent);
                    active = false;
                }
                return;
            }
            HostileDefeated();
            FriendliesDead();
        }

        private void CreateOpbase()
        {
            parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -10);
            
            Site site = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, EndGameDefOf.Outpost_opbase, parent.Tile, enemy, true, new float?());
            int randomInRange = SiteTuning.QuestSiteTimeoutDaysRange.RandomInRange;
            // Balance
            site.GetComponent<TimeoutComp>().StartTimeout(SiteTuning.QuestSiteRefugeeTimeoutDaysRange.RandomInRange * Global.DayInTicks);
            site.GetComponent<WorldComp_opbase>().StartComp();
            Find.WorldObjects.Add(site);
            
        }

        private void FriendliesDead()
        {
            MapParent map = (MapParent)parent;
            // All friendlies dead
            if (map.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(map.Map.ParentFaction).Count(p => GenHostility.IsActiveThreatTo(p, enemy)) == 0)
            {
                survivors = false;
            }
        }

        private void HostileDefeated()
        {
            // Killed all hostiles - Win outcome
            if (!GenHostility.AnyHostileActiveThreatTo(((MapParent)parent).Map, Faction.OfPlayer))
            {
                active = false;
                Map target = Find.AnyPlayerHomeMap;
                DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(target), target, rewards, 110, false, true, true);
                parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, +15, false, true);

                if (!(from s in Find.WorldObjects.Settlements
                    where s.Faction == enemy && !s.Faction.def.hidden && Find.WorldReachability.CanReach(Find.AnyPlayerHomeMap.Tile,s.Tile)
                    select s).TryRandomElement(out Settlement enemySet))
                {
                    Find.LetterStack.ReceiveLetter(EndGameDefOf.FE_JointRaid.letterLabel, TranslatorFormattedStringExtensions.Translate("Outpostdefensesuccess", parent.Faction.leader, parent.Faction.def.leaderTitle, GenLabel.ThingsLabel(rewards, string.Empty)), EndGameDefOf.FE_JointRaid.letterDef, null, parent.Faction, null);
                    active = false;
                    return;
                }

                List<Thing> rewardsNew = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
                {
                    totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow() + 500f))
                });

                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = (int)FE_IncidentWorker_Jointraid.SilverBonusRewardCurve.Evaluate(parent.Faction.PlayerGoodwill);

                int random = new IntRange(Global.DayInTicks * 15, Global.DayInTicks * 25).RandomInRange;
                enemySet.GetComponent<WorldComp_JointRaid>().StartComp(random, parent.Faction, rewards, silver);
                string text = TranslatorFormattedStringExtensions.Translate("OutpostdefensesuccessJointRaid", parent.Faction.leader, parent.Faction.def.leaderTitle, GenLabel.ThingsLabel(rewardsNew, string.Empty), (random / Global.DayInTicks).ToString(), GenThing.GetMarketValue(rewards).ToStringMoney(null), silver.stackCount.ToString(), GenLabel.ThingsLabel(rewards, string.Empty)).CapitalizeFirst();
                GenThing.TryAppendSingleRewardInfo(ref text, rewards);
                Find.LetterStack.ReceiveLetter(EndGameDefOf.FE_JointRaid.letterLabel, text, EndGameDefOf.FE_JointRaid.letterDef, enemySet, parent.Faction, null);
                
            }
        }

        public override void PostMyMapRemoved()
        {
            // All friendlies died but hostiles didn't when player left map
            if(active && !survivors)
            {
                CreateOpbase();
                Find.LetterStack.ReceiveLetter("LetterLabelOutpostdefenseFriendliesDead".Translate(), TranslatorFormattedStringExtensions.Translate("OutpostdefenseFriendliesDead", parent.Faction.leader, parent.Faction.def.leaderTitle)
                    , LetterDefOf.ThreatBig, new LookTargets(parent.Tile), null, null);
            }
        }

        
        public override void PostMapGenerate()
        {
            if (!active)
                return;
            MapParent map = (MapParent)parent;

            Faction faction = (from f in Find.FactionManager.AllFactions
                               where f.HostileTo(map.Map.ParentFaction) && f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction && !f.def.hidden
                               select f).RandomElement();

            Faction ally = map.Map.ParentFaction;
            List<Pawn> pawns = map.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(ally).ToList();
            float num = pointsRange.RandomInRange + StorytellerUtility.DefaultThreatPointsNow(map.Map);
            int count = pawns.Count();
            for (int i = 0; i < count; i++)
            {
                if (num > 0)
                    num -= pawns[i].kindDef.combatPower;
                else
                {
                    pawns[i].DeSpawn();
                    pawns[i].Destroy();
                    count--;
                }
            }
            if (num > 0)
            {
                SpawnPawnsFromPoints(num, CellFinder.RandomSpawnCellForPawnNear(pawns.RandomElement().RandomAdjacentCellCardinal(), map.Map, 10), ally, map.Map);
            }
            var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            var threatparms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map.Map);
            threatparms.faction = faction;
            threatparms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            threatparms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            threatparms.raidArrivalModeForQuickMilitaryAid = true;
            threatparms.points = StorytellerUtility.DefaultSiteThreatPointsNow() * 2;
            threatparms.raidNeverFleeIndividual = true;
            IncidentDefOf.RaidEnemy.Worker.TryExecute(threatparms);
            foreach (Pawn p in map.Map.mapPawns.AllPawns)
                map.Map.mapPawns.UpdateRegistryForPawn(p);
        }

        private void SpawnPawnsFromPoints(float num, IntVec3 intVec, Faction faction, Map map)
        {
            List<Pawn> list = new List<Pawn>();
            for (int i = 0; i < 50; i++)
            {
                PawnKindDef pawnKindDef = GenCollection.RandomElementByWeight<PawnKindDef>(from kind in DefDatabase<PawnKindDef>.AllDefsListForReading
                                                                                           where kind.RaceProps.IsFlesh && kind.RaceProps.Humanlike
                                                                                           select kind, (PawnKindDef kind) => 1f / kind.combatPower);
                list.Add(PawnGenerator.GeneratePawn(pawnKindDef, faction));
                num -= pawnKindDef.combatPower;
                if (num <= 0f)
                {
                    break;
                }
            }
            IntVec3 intVec2 = default(IntVec3);
            for (int j = 0; j < list.Count(); j++)
            {
                IntVec3 intVec3 = CellFinder.RandomSpawnCellForPawnNear(intVec, map, 10);
                intVec2 = intVec3;

                GenSpawn.Spawn(list[j], intVec3, map, Rot4.Random, WipeMode.Vanish, false);
            }
            LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(intVec2), map, list);
        }

        public override string CompInspectStringExtra()
        {
            if(active)
                return "ExtraCompString_Outpostdefense".Translate((stopTime - Find.TickManager.TicksGame).ToStringTicksToPeriod());
            return null;
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "outpostdefense_active", defaultValue: false);
            Scribe_Values.Look(ref survivors, "outpostdefense_survivors", defaultValue: true);
            Scribe_References.Look(ref parms, "outpostdefense_parms");
            Scribe_References.Look(ref enemy, "outpostdefense_enemy");
            Scribe_Values.Look(ref stopTime, "outpostdefense_stopTime", defaultValue: 0);
            Scribe_Collections.Look(ref rewards, "outpostdefense_rewards", LookMode.Deep);
        }
    }
    public class WorldObjectCompProperties_SiteDefense : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SiteDefense()
        {
            compClass = typeof(WorldComp_SiteDefense);
        }
    }
}
