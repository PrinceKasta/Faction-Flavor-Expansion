using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class WorldComp_SiteDefense : WorldObjectComp
    {
        private bool active = false;
        private bool survivors = true;
        private Faction enemy;
        private List<Thing> rewards;

        public void StartComp(Faction enemy, List<Thing> rewards)
        {
            this.rewards = rewards;
            active = true;
            this.enemy = enemy;
        }
        public bool IsActive => active;

        public override void CompTick()
        {
            if (!active)
                return;
            // Player let the countdown finish, ignored quest.
            if (!((MapParent)parent).HasMap)
            {
                if (parent.GetComponent<TimeoutComp>().Passed)
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
            site.GetComponent<TimeoutComp>().StartTimeout(SiteTuning.QuestSiteRefugeeTimeoutDaysRange.RandomInRange * Global.DayInTicks);
            site.GetComponent<WorldComp_opbase>().StartComp();
            Find.WorldObjects.Add(site);
        }

        private void FriendliesDead()
        {
            // All friendlies dead
            if (survivors && ((MapParent)parent).Map.mapPawns.FreeHumanlikesSpawnedOfFaction(parent.Faction).Count(p => GenHostility.IsActiveThreatTo(p, enemy)) == 0)
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
                DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap), Find.AnyPlayerHomeMap, rewards, 110, false, true, true);
                parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, +15, false, true);

                if (!Find.WorldObjects.Settlements.Where(s=> s.Faction == enemy && !s.Faction.def.hidden && Find.WorldReachability.CanReach(Find.AnyPlayerHomeMap.Tile, s.Tile)).TryRandomElement(out Settlement enemySet))
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelOutpostdefensesuccess".Translate(), TranslatorFormattedStringExtensions.Translate("Outpostdefensesuccess", parent.Faction.leader, parent.Faction.def.leaderTitle, GenLabel.ThingsLabel(rewards, string.Empty)), EndGameDefOf.FE_JointRaid.letterDef, null, parent.Faction, null);
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
                enemySet.GetComponent<WorldComp_JointRaid>().StartComp(random, parent.Faction, rewardsNew, silver);
                string text = TranslatorFormattedStringExtensions.Translate("OutpostdefensesuccessJointRaid", parent.Faction.leader, parent.Faction.def.leaderTitle, GenLabel.ThingsLabel(rewardsNew, string.Empty), random.ToStringTicksToPeriod(), GenThing.GetMarketValue(rewards).ToStringMoney(null), silver.stackCount.ToString(), GenLabel.ThingsLabel(rewards, string.Empty)).CapitalizeFirst();
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

            List<Pawn> pawns = map.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(parent.Faction).ToList();
            float num = 0;
            for (int i = 0; i < pawns.Count(); i++)
            {
                num += pawns[i].kindDef.combatPower;
            }
            if (num > 0)
            {
                SpawnPawnsFromPoints(num, CellFinder.RandomSpawnCellForPawnNear(pawns.RandomElement().RandomAdjacentCellCardinal(), map.Map, 10), map.Map);
            }
            var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            var threatparms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map.Map);
            threatparms.faction = enemy;
            threatparms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            threatparms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            threatparms.points = StorytellerUtility.DefaultSiteThreatPointsNow() * 2;
            threatparms.raidNeverFleeIndividual = true;
            IncidentDefOf.RaidEnemy.Worker.TryExecute(threatparms);
            foreach (Pawn p in map.Map.mapPawns.AllPawns)
            {
                map.Map.mapPawns.UpdateRegistryForPawn(p);
            }
        }

        private void SpawnPawnsFromPoints(float num, IntVec3 intVec, Map map)
        {
            List<PawnKindDef> kindDefs = Utilities.GeneratePawnKindDef(65, parent.Faction);
            Utilities.GenerateFighter(num, LordMaker.MakeNewLord(parent.Faction, new LordJob_DefendBase(parent.Faction, map.Center), map), kindDefs, map, parent.Faction, intVec);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "outpostdefense_active", defaultValue: false);
            Scribe_Values.Look(ref survivors, "outpostdefense_survivors", defaultValue: true);
            Scribe_References.Look(ref enemy, "outpostdefense_enemy");
            Scribe_Collections.Look(ref rewards, "outpostdefense_rewards", LookMode.Deep);
        }
    }
    public class WorldObjectCompProperties_SiteDefense : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SiteDefense() => compClass = typeof(WorldComp_SiteDefense);
    }
}
