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
using RimWorld.BaseGen;

namespace Flavor_Expansion
{
    class WorldComp_SettlementDefender : WorldObjectComp
    {
        private bool active = false, survivors = true;
        private int timeOut = 0;
        private Faction enemy;
        private Faction ally;
        private List<Thing> FactionThings=new List<Thing>();
        private List<Thing> rewards;

        public override void CompTick()
        {
            if (!active)
                return;
            if (!enemy.HostileTo(ally) && !ParentHasMap)
                active = false;
            MapParent map = (MapParent)this.parent;

            if (!ParentHasMap)
            {
                if (timeOut <= 0)
                {
                    active = false;
                    int tile = parent.Tile;
                    int ID = parent.ID;
                    Find.WorldObjects.Remove(parent);
                    Utilities.FactionsWar().GetByFaction(ally).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE;

                    if (!(from f in Find.WorldObjects.Settlements
                          where f.Faction == parent.Faction
                          select f).Any())
                    {
                        Find.LetterStack.ReceiveLetter("FactionDestroyed".Translate(), "LetterFactionBaseDefeated_FactionDestroyed".Translate((NamedArgument)parent.Faction.Name), LetterDefOf.PositiveEvent, null, parent.Faction, (string)null);
                        parent.Faction.defeated = true;
                        return;
                    }

                    Site resuce = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, EndGameDefOf.Outpost_SiteResuce, tile, enemy, true, StorytellerUtility.DefaultSiteThreatPointsNow());
                    
                    resuce.GetComponent<WorldComp_SettlementResuce>().StartComp(ID, ally);
                    resuce.GetComponent<TimeoutComp>().StartTimeout(6000);
                    Find.WorldObjects.Add(resuce);
                    Find.LetterStack.ReceiveLetter("LetterLabelSettlementDefenderIgnored".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementDefenderIgnored", this.parent, this.parent.Faction.leader),
                            LetterDefOf.ThreatBig, new LookTargets(parent.Tile), (Faction)null, (string)null);
                    return;
                }
                timeOut--;
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
        public void StartComp(Faction enemy, Faction ally, int timeout, List<Thing> rewards)
        {
            this.rewards = rewards;
            this.timeOut = timeout;
            this.active = true;
            this.enemy = enemy;
            this.ally = ally;
        }

        public bool IsActive()
        {
            return active;
        }

        public override void PostMyMapRemoved()
        {
            if (parent.GetComponent<TimedForcedExit>().ForceExitAndRemoveMapCountdownActive)
                active = false;
        }
        public override void PostMapGenerate()
        {
            base.PostMapGenerate();
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
            PawnGroupMakerParms parms = new PawnGroupMakerParms();
            parms.faction = enemy;
            parms.points = Math.Max(StorytellerUtility.DefaultThreatPointsNow(parent.Map)* 30 , 2500);
            parms.groupKind = PawnGroupKindDefOf.Combat;
            parms.generateFightersOnly = true;
            IEnumerable<Pawn> pawns= PawnGroupMakerUtility.GeneratePawns(parms);
            IntVec3 vec3;
            
            if (!RCellFinder.TryFindRandomPawnEntryCell(out vec3, parent.Map, 0f,false,x=> x.Standable(parent.Map) && x.Walkable(parent.Map)))
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
            MapParent map = (MapParent)this.parent;
            if (map.HasMap && map.Faction == ally && survivors
                && !GenHostility.AnyHostileActiveThreatTo(map.Map, map.Faction))
            {
                this.parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 12);
                Map target = Find.AnyPlayerHomeMap;
                IntVec3 intVec3 = DropCellFinder.TradeDropSpot(target);
                DropPodUtility.DropThingsNear(intVec3, target, (IEnumerable<Thing>)rewards, 110, false, true, true);
                string text = "" + TranslatorFormattedStringExtensions.Translate("SettlementDefenderWon", this.parent, (NamedArgument)TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks), this.parent.Faction.leader) + GenLabel.ThingsLabel(rewards, string.Empty);
                GenThing.TryAppendSingleRewardInfo(ref text, (IList<Thing>)rewards);
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementDefenderWon".Translate(), text, LetterDefOf.PositiveEvent, (LookTargets)((WorldObject)this.parent), (Faction)null, (string)null);
                map.Map.Parent.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown(Global.DayInTicks);

                return true;
            }
            return false;

        }

        private bool FriendliesDead()
        {
            MapParent map = (MapParent)this.parent;
            if (map.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(map.Map.ParentFaction).Count(p=> !p.Downed && !p.Dead && p.RaceProps.Humanlike)==0)
            {
                DestroyedSettlement destroyedSettlement = (DestroyedSettlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.DestroyedSettlement);
                destroyedSettlement.Tile = parent.Tile;
                Find.WorldObjects.Add((WorldObject)destroyedSettlement);
                map.Map.info.parent = (MapParent)destroyedSettlement;
                Find.WorldObjects.Remove((WorldObject)parent);
                destroyedSettlement.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown();
                survivors = false;
                this.parent.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown(Global.DayInTicks);
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementDefenderLost".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementDefenderLost", this.parent, (NamedArgument)TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks), this.parent.Faction.leader)
                    , LetterDefOf.NegativeEvent, new LookTargets(parent.Tile), (Faction)null, (string)null);
                return true;
            }
            return false;
        }
        public override string CompInspectStringExtra()
        {
            if(active)
                return base.CompInspectStringExtra() + "ExtraCompString_SettlementDefense".Translate(timeOut.ToStringTicksToPeriod());
            return base.CompInspectStringExtra();
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "SettlementDefender_Active", defaultValue: false);
            Scribe_References.Look(ref ally, "SettlementDefender_Ally");
            Scribe_References.Look(ref enemy, "SettlementDefender_Enemy");
            Scribe_Values.Look(ref survivors, "SettlementDefender_survivors", defaultValue: true);
            Scribe_Values.Look(ref timeOut, "SettlementDefender_timeOut", defaultValue: 0);
        }
        public WorldObjectCompProperties_SettlementDefender Props => (WorldObjectCompProperties_SettlementDefender)this.props;
        
    }
    public class WorldObjectCompProperties_SettlementDefender : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SettlementDefender()
        {
            this.compClass = typeof(WorldComp_SettlementDefender);
        }
    }
}
