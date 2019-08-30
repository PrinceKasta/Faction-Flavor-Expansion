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
            this.stopTime = stopTime;
            this.active = true;
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
            MapParent map = (MapParent)this.parent;
            // Player let the countdown finish, ignored quest.
            if (!map.HasMap && 0 >= stopTime)
            {

                CreateOpbase();
                Find.WorldObjects.Remove(parent);
                active = false;
            }
            else stopTime--;
            if (!map.HasMap)
                return;
            HostileDefeated();
            FriendliesDead();
        }

        private void CreateOpbase()
        {
            parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -10);

            List<Thing> list = new List<Thing>();

            Site site = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, EndGameDefOf.Outpost_opbase, parent.Tile, enemy, true, new float?());
            
            int randomInRange = SiteTuning.QuestSiteTimeoutDaysRange.RandomInRange;
            // Balance
            site.GetComponent<TimeoutComp>().StartTimeout(SiteTuning.QuestSiteRefugeeTimeoutDaysRange.RandomInRange * Global.DayInTicks);
            site.GetComponent<WorldComp_opbase>().StartComp();
            
            Find.WorldObjects.Add(site);
            
        }

        private void FriendliesDead()
        {
            MapParent map = (MapParent)this.parent;
            // All friendlies dead
            if (map.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(map.Map.ParentFaction).Count(p => !p.Downed || !p.Dead) == 0)
            {
                survivors = false;
            }
        }

        private void HostileDefeated()
        {
            MapParent map = (MapParent)this.parent;
            // Killed all hostiles - Win outcome
            if (!GenHostility.AnyHostileActiveThreatTo(map.Map, Faction.OfPlayer))
            {
                active = false;
                Settlement enemySet;
                Map target = Find.AnyPlayerHomeMap;
                IntVec3 intVec3 = DropCellFinder.TradeDropSpot(target);
                DropPodUtility.DropThingsNear(intVec3, target, (IEnumerable<Thing>)rewards, 110, false, true, true);

                this.parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, +15, false, true);
                if (!(from s in Find.WorldObjects.Settlements
                    where s.Faction == enemy && !s.Faction.def.hidden && Find.WorldReachability.CanReach(Find.AnyPlayerHomeMap.Tile,s.Tile)
                    select s).TryRandomElement(out enemySet))
                {
                    Find.LetterStack.ReceiveLetter(EndGameDefOf.FE_JointRaid.letterLabel, TranslatorFormattedStringExtensions.Translate("Outpostdefensesuccess", this.parent.Faction.leader, this.parent.Faction.def.leaderTitle, GenLabel.ThingsLabel(rewards, string.Empty)), EndGameDefOf.FE_JointRaid.letterDef, null, parent.Faction, (string)null);
                    active = false;
                    return;
                }

                List<Thing> rewardsNew = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
                {
                    totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow() + 500f))
                });

                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = (int)FE_IncidentWorker_Jointraid.SilverBonusRewardCurve.Evaluate(parent.Faction.PlayerGoodwill);

                int random = new IntRange(Global.DayInTicks * 5, Global.DayInTicks * 7).RandomInRange;
                enemySet.GetComponent<WorldComp_JointRaid>().StartComp(random, parent.Faction, rewards, silver);
                string text = TranslatorFormattedStringExtensions.Translate("OutpostdefensesuccessJointRaid", (NamedArgument)parent.Faction.leader, (NamedArgument)parent.Faction.def.leaderTitle, (NamedArgument)GenLabel.ThingsLabel(rewardsNew, string.Empty), (NamedArgument)(random / Global.DayInTicks).ToString(), (NamedArgument)GenThing.GetMarketValue((IList<Thing>)rewards).ToStringMoney((string)null), silver.stackCount.ToString(),(NamedArgument)GenLabel.ThingsLabel(rewards, string.Empty)).CapitalizeFirst();
                GenThing.TryAppendSingleRewardInfo(ref text, (IList<Thing>)rewards);
                Find.LetterStack.ReceiveLetter(EndGameDefOf.FE_JointRaid.letterLabel, text, EndGameDefOf.FE_JointRaid.letterDef, (LookTargets)((WorldObject)enemySet), parent.Faction, (string)null);
                
            }
        }

        public override void PostMyMapRemoved()
        {
            // All friendlies died but hostiles didn't when player left map
            if(active && !survivors)
            {
                CreateOpbase();
                Find.LetterStack.ReceiveLetter("LetterLabelOutpostdefenseFriendliesDead".Translate(), TranslatorFormattedStringExtensions.Translate("OutpostdefenseFriendliesDead", this.parent.Faction.leader, this.parent.Faction.def.leaderTitle)
                    , LetterDefOf.ThreatBig, new LookTargets(parent.Tile), (Faction)null, (string)null);
            }
        }

        
        public override void PostMapGenerate()
        {
            if (!active)
                return;
            MapParent map = (MapParent)this.parent;

            Faction faction = (from f in Find.FactionManager.AllFactions
                               where f.HostileTo(map.Map.ParentFaction) && f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction && !f.def.hidden
                               select f).RandomElement();

            Faction ally = map.Map.ParentFaction;
            List<Pawn> pawns = map.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(ally).ToList();
            float num = this.pointsRange.RandomInRange + StorytellerUtility.DefaultThreatPointsNow(map.Map);
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
                return "ExtraCompString_Outpostdefense".Translate((NamedArgument)this.stopTime.ToStringTicksToPeriod());
            return (string)null;
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "outpostdefense_active", defaultValue: false);
            Scribe_Values.Look(ref survivors, "outpostdefense_survivors", defaultValue: true);
            Scribe_References.Look(ref parms, "outpostdefense_parms");
            Scribe_References.Look(ref enemy, "outpostdefense_enemy");
            Scribe_Values.Look(ref stopTime, "outpostdefense_stopTime", defaultValue: 0);
            Scribe_Collections.Look(ref rewards, "outpostdefense_rewards", LookMode.Reference);
        }
    }
    public class WorldObjectCompProperties_SiteDefense : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SiteDefense()
        {
            this.compClass = typeof(WorldComp_SiteDefense);
        }
    }
}
