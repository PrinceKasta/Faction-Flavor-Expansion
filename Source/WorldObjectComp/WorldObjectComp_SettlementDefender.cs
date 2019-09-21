using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class WorldComp_SettlementDefender : WorldObjectComp
    {
        private bool active = false, survivors = true;
        private int timer = 0;
        private Faction enemy;
        private Faction ally;
        private List<Thing> FactionThings=new List<Thing>();
        private List<Thing> rewards;

        public override void CompTick()
        {
            if (!active)
                return;

            if (!ParentHasMap)
            {
                if(!enemy.HostileTo(ally))
                {
                    active = false;
                    return;
                }
                if (timer <= Find.TickManager.TicksGame)
                {
                    active = false;
                    
                    Utilities.FactionsWar().GetByFaction(ally).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE;
                    if (!Find.WorldObjects.Settlements.Where(f=> f.Faction == parent.Faction).Any())
                    {
                        Find.LetterStack.ReceiveLetter("FactionDestroyed".Translate(), "LetterFactionBaseDefeated_FactionDestroyed".Translate(parent.Faction.Name), LetterDefOf.PositiveEvent, null, parent.Faction, null);
                        parent.Faction.defeated = true;
                        return;
                    }

                    Site resuce = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, EndGameDefOf.Outpost_SiteResuce, parent.Tile, enemy, true, StorytellerUtility.DefaultSiteThreatPointsNow());
                    resuce.GetComponent<WorldComp_SettlementResuce>().StartComp(ally);
                    resuce.GetComponent<TimeoutComp>().StartTimeout(new IntRange(9 * Global.DayInTicks, 15 * Global.DayInTicks).RandomInRange);
                    Find.WorldObjects.Remove(parent);
                    Find.WorldObjects.Add(resuce);

                    Find.LetterStack.ReceiveLetter("LetterLabelSettlementDefenderIgnored".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementDefenderIgnored", parent, parent.Faction.leader),
                            LetterDefOf.ThreatBig, new LookTargets(parent.Tile), null, null);
                }
                return;
            }
            //Goodwill cost to unforbid items in the ally map
            foreach (Thing thing in FactionThings)
            {
                if (thing.Faction == Faction.OfPlayer || (thing.TryGetComp<CompForbiddable>() != null && !thing.IsForbidden(Faction.OfPlayer)))
                {
                    parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -5);
                    FactionThings.Remove(thing);
                    break;
                }
            }
            FriendliesDead();
            HostileDefeated();
        }
        public void StartComp(Faction enemy, Faction ally, int timer, List<Thing> rewards)
        {
            this.rewards = rewards;
            this.timer = timer + Find.TickManager.TicksGame;
            active = true;
            this.enemy = enemy;
            this.ally = ally;
        }

        public bool IsActive => active;

        public override void PostMyMapRemoved()
        {
            if (parent.GetComponent<TimedForcedExit>().ForceExitAndRemoveMapCountdownActive)
                active = false;
        }
        public override void PostMapGenerate()
        {
            if (!active)
                return;
            MapParent parent = (MapParent)this.parent;
            foreach (var thing in parent.Map.listerThings.AllThings)
            {
                if ((!(thing is Pawn) && thing.def.Claimable && !thing.def.defName.Contains("Door")) || thing.def.CountAsResource)
                {
                    if(thing.TryGetComp<CompForbiddable>()!=null)
                    {
                        thing.SetForbidden(true);
                    }
                    if(thing.def.CanHaveFaction)
                        thing.SetFactionDirect(ally);
                    FactionThings.Add(thing);
                }
            }
            PawnGroupMakerParms parms = new PawnGroupMakerParms
            {
                faction = enemy,
                points = Math.Max(StorytellerUtility.DefaultThreatPointsNow(parent.Map) * 30, 2500),
                groupKind = PawnGroupKindDefOf.Combat,
                generateFightersOnly = true
            };
            IEnumerable<Pawn> pawns= PawnGroupMakerUtility.GeneratePawns(parms);
            
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 vec3, parent.Map, 0f,false,x=> x.Standable(parent.Map) && x.Walkable(parent.Map)))
                return;
            Lord lord = LordMaker.MakeNewLord(enemy, new LordJob_AssaultColony(enemy,true,true), parent.Map);
            foreach (Pawn p in pawns)
            {
                GenSpawn.Spawn(p, vec3, parent.Map);
                vec3 = p.RandomAdjacentCell8Way().ClampInsideMap(parent.Map);
                lord.AddPawn(p);
                parent.Map.mapPawns.UpdateRegistryForPawn(p);
            }
        }
        private bool HostileDefeated()
        {
            if (parent.GetComponent<TimedForcedExit>().ForceExitAndRemoveMapCountdownActive)
            {
                return false;
            }
            MapParent map = (MapParent)parent;
            if (map.HasMap && map.Faction == ally && survivors && !GenHostility.AnyHostileActiveThreatTo(map.Map, map.Faction))
            {
                parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 12);
                DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap), Find.AnyPlayerHomeMap, rewards, 110, false, true, true);
                string text = TranslatorFormattedStringExtensions.Translate("SettlementDefenderWon", parent, TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks), parent.Faction.leader) + GenLabel.ThingsLabel(rewards, string.Empty);
                GenThing.TryAppendSingleRewardInfo(ref text, rewards);
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementDefenderWon".Translate(), text, LetterDefOf.PositiveEvent, parent, null, null);
                map.Map.Parent.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown(Global.DayInTicks);
                return true;
            }
            return false;
        }

        private bool FriendliesDead()
        {
            MapParent map = (MapParent)parent;
            if (map.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(map.Map.ParentFaction).Count(p=> !p.Downed && !p.Dead && p.RaceProps.Humanlike)==0)
            {
                DestroyedSettlement destroyedSettlement = (DestroyedSettlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.DestroyedSettlement);
                destroyedSettlement.Tile = parent.Tile;
                Find.WorldObjects.Add(destroyedSettlement);
                map.Map.info.parent = destroyedSettlement;
                Find.WorldObjects.Remove(parent);
                destroyedSettlement.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown();
                survivors = false;
                parent.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown(Global.DayInTicks);
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementDefenderLost".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementDefenderLost", parent, TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks), parent.Faction.leader)
                    , LetterDefOf.NegativeEvent, new LookTargets(parent.Tile), null, null);
                return true;
            }
            return false;
        }
        public override string CompInspectStringExtra() => active
                ? base.CompInspectStringExtra() + "ExtraCompString_SettlementDefense".Translate((timer - Find.TickManager.TicksGame).ToStringTicksToPeriod())
                : base.CompInspectStringExtra();

        public override void PostExposeData()
        {
            
            Scribe_Values.Look(ref active, "SettlementDefender_Active", defaultValue: false);
            if (!active)
                return;
            Scribe_References.Look(ref ally, "SettlementDefender_Ally");
            Scribe_References.Look(ref enemy, "SettlementDefender_Enemy");
            Scribe_Values.Look(ref survivors, "SettlementDefender_survivors", defaultValue: true);
            Scribe_Values.Look(ref timer, "SettlementDefender_timer", defaultValue: 0);
            Scribe_Collections.Look(ref rewards, "SettlementDefender_rewards",LookMode.Deep);
            Scribe_Collections.Look(ref FactionThings, "SettlementDefender_Factionthings",LookMode.Reference);
        }
        public WorldObjectCompProperties_SettlementDefender Props => (WorldObjectCompProperties_SettlementDefender)props;
        
    }
    public class WorldObjectCompProperties_SettlementDefender : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SettlementDefender() => compClass = typeof(WorldComp_SettlementDefender);
    }
}
