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
        bool active = false, threat = true, survivors = true;
        int timer = 0;
        int timeOut = 0;
        Faction enemy;
        Faction ally;
        List<Thing> FactionThings=new List<Thing>();

        public override void CompTick()
        {
            base.CompTick();
            if (!active)
                return;
            if (!enemy.HostileTo(ally))
                active = false;
            MapParent map = (MapParent)this.parent;
            if (ParentHasMap)
            {
                //Add foribden stuff
                foreach (Thing thing in FactionThings)
                {
                    if (thing.Faction == Faction.OfPlayer || (thing.TryGetComp<CompForbiddable>() != null && !thing.IsForbidden(Faction.OfPlayer)))
                    {
                        parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -5);
                        FactionThings.Remove(thing);
                        break;
                    }
                }
                if (survivors && !FriendliesAlive())
                    survivors = false;
                else Log.Warning(map.Map.Parent.GetComponent<TimedForcedExit>().ForceExitAndRemoveMapCountdownTimeLeftString);
            }
            else if (timeOut > 1000)
            {
                active = false;
                int tile = parent.Tile;
                int ID = parent.ID;
                Find.WorldObjects.Remove(parent);
                if (!(from f in Find.WorldObjects.AllWorldObjects
                      where f.Faction == parent.Faction
                      select f).Any())
                {
                    Find.LetterStack.ReceiveLetter("Ally Faction destoryed", "LetterFactionBaseDefeated_FactionDestroyed".Translate((NamedArgument)parent.Faction.Name), LetterDefOf.NegativeEvent, null, parent.Faction, (string)null);
                    parent.Faction.defeated = true;
                    Find.FactionManager.Remove(parent.Faction);
                    return;
                }
                IntRange SettlementSizeRange = new IntRange(34, 38);
                Site resuce = (Site)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Site);
                resuce.Tile = tile;
                resuce.core = new SiteCore(SiteCoreDefOf.Nothing, SiteCoreDefOf.Nothing.Worker.GenerateDefaultParams(resuce, StorytellerUtility.DefaultSiteThreatPointsNow()));
                SiteCoreOrPartParams parms = new SiteCoreOrPartParams();
                resuce.SetFaction((from f in Find.FactionManager.AllFactions
                                  where f.HostileTo(Faction.OfPlayer) && f.HostileTo(parent.Faction) && !f.def.hidden
                                  select f).RandomElement());
                resuce.parts.Add(new SitePart(EndGameDefOf.Outpost_SiteResuce, EndGameDefOf.Outpost_SiteResuce.Worker.GenerateDefaultParams(resuce, StorytellerUtility.DefaultSiteThreatPointsNow())));
                
                resuce.GetComponent<WorldComp_SettlementResuce>().StartComp(ID, ally);
                resuce.GetComponent<TimeoutComp>().StartTimeout(6000);
                Find.WorldObjects.Add(resuce);
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementDefenderIgnored".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementDefenderIgnored", this.parent, this.parent.Faction.leader),
                        LetterDefOf.NegativeEvent, new LookTargets(parent.Tile), (Faction)null, (string)null);

            }
            else timeOut++;
            if (threat && HostileDefeated())
                threat = false;
            
            if (!threat && !ParentHasMap)
                active = false;

        }
        public void StartComp(Faction enemy, Faction ally)
        {
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
            base.PostMyMapRemoved();
            if (!survivors)
            {
                Find.WorldObjects.Remove(parent);
            }
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
            if (!RCellFinder.TryFindRandomPawnEntryCell(out vec3, parent.Map, 0.25f))
                return;
            Lord lord = LordMaker.MakeNewLord(enemy, new LordJob_AssaultColony(enemy,true,false), parent.Map);
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
            
            if(timer<3000)
            {
                if (!Find.TickManager.Paused)
                {
                    timer++;
                }
                return false;
            }

            MapParent map = (MapParent)this.parent;
            if (map.HasMap && map.Faction== ally && survivors
                && !GenHostility.AnyHostileActiveThreatTo(map.Map,map.Faction))
            {
                    this.parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 12);
                    
                    Gift_RewardGeneratorBasedTMagic gift = new Gift_RewardGeneratorBasedTMagic();
                    List<Thing> list = new List<Thing>();
                    list = gift.Generate(1000, list);
                    Map target = Find.AnyPlayerHomeMap;
                    IntVec3 intVec3 = DropCellFinder.TradeDropSpot(target);
                    DropPodUtility.DropThingsNear(intVec3, target, (IEnumerable<Thing>)list, 110, false, true, true);
                    string itemList = "";
                    for (int i = 0; i < list.Count(); i++)
                    {
                        itemList += list[i].Label + "\n\n";
                    }
                    Find.LetterStack.ReceiveLetter("LetterLabelSettlementDefenderWon".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementDefenderWon", this.parent, (NamedArgument)TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks), this.parent.Faction.leader)
                         + itemList, LetterDefOf.PositiveEvent, (LookTargets)((WorldObject)this.parent), (Faction)null, (string)null);
                    map.Map.Parent.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown(Global.DayInTicks);
                
                return true;
            }
            return false;

        }

        private bool FriendliesAlive()
        {
            MapParent map = (MapParent)this.parent;
            if ((from p in map.Map.mapPawns.PawnsInFaction(map.Map.ParentFaction)
                 where !p.Downed && !p.Dead && p.RaceProps.Humanlike
                 select p).Count() > 0)
            {
                return true;
            }
            this.parent.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown(Global.DayInTicks);
            Find.LetterStack.ReceiveLetter("LetterLabelSettlementDefenderLost".Translate(),TranslatorFormattedStringExtensions.Translate("SettlementDefenderLost", this.parent, (NamedArgument)TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks), this.parent.Faction.leader)
                , LetterDefOf.NegativeEvent, new LookTargets(parent.Tile), (Faction)null, (string)null);
            
            return false;
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "SettlementDefender_Active", defaultValue: false);
            Scribe_References.Look(ref ally, "SettlementDefender_Ally");
            Scribe_References.Look(ref enemy, "SettlementDefender_Enemy");
            Scribe_Values.Look(ref threat, "SettlementDefender_threat", defaultValue: true);
            Scribe_Values.Look(ref survivors, "SettlementDefender_survivors", defaultValue: true);
            Scribe_Values.Look(ref timer, "SettlementDefender_timer", defaultValue: 0);
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
