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
    class WorldComp_SettlementResuce : WorldObjectComp
    {
        private static readonly IntRange CorpseAmountRange = new IntRange(6, 14);
        private const float UnWillingChance = 0.75f;
        private int ID;
        private bool active = false, resurrectSet = false, threat = true, wasEntered=false;
        private int pawnStaying = 0;
        private Faction ally;

        public void StartComp(int ID , Faction ally)
        {
            this.ID = ID;
            this.active = true;
            this.ally = ally;
        }
        public override void CompTick()
        {
            if (!active)
                return;
            
            if (threat && HostileDefeated())
                parent.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown(Global.DayInTicks);

        }
        public override void PostMyMapRemoved()
        {
            Ressurect();
        }
        public override void PostPostRemove()
        {
            MapParent map = (MapParent)this.parent;
            if (active && this.parent.GetComponent<TimeoutComp>().Passed && !map.HasMap && !wasEntered)
            {
                ally.TryAffectGoodwillWith(Faction.OfPlayer, -30);
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementRescueIgnored".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementRescueIgnored", this.parent, ally.leader, ally.def.leaderTitle), LetterDefOf.NegativeEvent, null, ally);

            } else if(active && wasEntered && threat)
            {
                ally.TryAffectGoodwillWith(Faction.OfPlayer, -20, false, true, TranslatorFormattedStringExtensions.Translate("SettlementRescueAbandoned", this.parent, ally.leader, ally.def.leaderTitle));
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementRescueIgnored".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementRescueAbandoned", this.parent, ally.leader, ally.def.leaderTitle, (-20).ToString()), LetterDefOf.NegativeEvent, null, ally);

            }
        }
        private void Ressurect()
        {
            MapParent map = (MapParent)this.parent;
            
            if ((this.parent.GetComponent<TimeoutComp>().Passed || resurrectSet) && !ally.defeated && !map.HasMap)
            {
                Settlement resurrect = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                Faction faction = ally;
                resurrect.SetFaction(faction);
                resurrect.Tile = parent.Tile;
                resurrect.creationGameTicks = parent.creationGameTicks;
                resurrect.Name = SettlementNameGenerator.GenerateSettlementName(resurrect);
                Find.WorldObjects.AllWorldObjects.Add(resurrect);
                Utilities.FactionsWar().GetByFaction(resurrect.Faction).resources += FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE;
            }

        }
        public override void PostMapGenerate()
        {
            if (!active)
                return;
            MapParent map = (MapParent)this.parent;

            foreach (var thing in map.Map.listerThings.AllThings)
            {
                if ((!(thing is Pawn) && thing.def.Claimable))
                {
                    if (thing.def.CanHaveFaction)
                        thing.SetFactionDirect(map.Map.ParentFaction);
                }
            }

            SpawnAdditonalPrisoners(map.Map);
            SpawnCorpses(map.Map);
            wasEntered = true;

        }
        private void SpawnAdditonalPrisoners(Map map)
        {
            PrisonerWillingToJoinComp component = this.parent.GetComponent<PrisonerWillingToJoinComp>();
            List<Pawn> prisoner = (from p in map.mapPawns.AllPawnsSpawned
                                   where p.kindDef == PawnKindDefOf.Slave || p.IsPrisoner
                                   select p).ToList();
            DamageInfo info = new DamageInfo(DamageDefOf.Blunt, 20, 0);
            info.SetAllowDamagePropagation(true);

            for (int i = 0; i <  prisoner.ToList().Count; i++)
            {
                prisoner[i].SetFaction(ally);
                prisoner[i].guest.SetGuestStatus(map.ParentFaction, true);
                prisoner[i].mindState.WillJoinColonyIfRescued = true;
                Pawn pawn = component == null || !component.pawn.Any ? PrisonerWillingToJoinQuestUtility.GeneratePrisoner(map.Tile, map.ParentFaction) : component.pawn.Take((Thing)component.pawn[0]);
                pawn.mindState.WillJoinColonyIfRescued = true;

                pawn.SetFaction(ally);
                
                IntVec3 result;
                result = CellFinder.RandomSpawnCellForPawnNear(prisoner[i % prisoner.ToList().Count].CellsAdjacent8WayAndInside().Where(x=>x.Standable(map) && x.Walkable(map)).RandomElement(), map);
                if (result == null)
                    Log.Warning("spawn pawn cell null");
                pawn.guest.SetGuestStatus(map.ParentFaction, true);
                GenSpawn.Spawn(pawn, result, map);
                if (pawn.equipment != null && pawn.equipment.AllEquipmentListForReading.Count > 0)
                    pawn.equipment.DestroyAllEquipment();
                pawn.TakeDamage(info);
                prisoner[i % prisoner.ToList().Count].TakeDamage(info);
            }
        }
        private void SpawnCorpses(Map map)
        {
            Predicate<IntVec3> baseValidator = (Predicate<IntVec3>)(x =>
            {
                TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassAllDestroyableThings, Danger.Deadly, false);
                
                x.Walkable(map);
                return map.reachability.CanReachMapEdge(x, traverseParms);
            });
            int corpseAmount = CorpseAmountRange.RandomInRange;
            DamageInfo info = (this.parent.Faction.def.techLevel.IsNeolithicOrWorse()) ? new DamageInfo(DamageDefOf.Cut, 25) : new DamageInfo(DamageDefOf.Bullet, 40);
            for (int i = 0; i < corpseAmount; i++)
            {
                Pawn corpse = PawnGenerator.GeneratePawn(PawnKindDefOf.Villager, ally);
                if(corpse.inventory.innerContainer.Count>0)
                   corpse.inventory.DestroyAll();
                IntVec3 v= CellFinder.RandomClosewalkCellNear(map.Center,map,25,baseValidator);
                
                GenSpawn.Spawn(corpse, v,map);
                corpse.Kill(info);
            }
        }
        private bool HostileDefeated()
        {
            MapParent map = (MapParent)this.parent;
            if (map.HasMap && !GenHostility.AnyHostileActiveThreatTo(map.Map, Faction.OfPlayer))
            {
                int pawnSaved = 0;
                threat = false;
               List<Pawn> prisoner= map.Map.mapPawns.AllPawns.Where(x => (!x.Dead && !x.Downed) && (x.IsPrisoner || x.kindDef == PawnKindDefOf.Slave)).ToList();
                if(prisoner.Where(x => !x.Dead || x.Downed).Count() == 0)
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelSettlementRescueFail".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementRescueFail",this.parent, (NamedArgument)TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks),
                    this.parent.Faction.leader), LetterDefOf.NegativeEvent, (LookTargets)((WorldObject)this.parent), (Faction)null, (string)null);
                    return true;
                }
                foreach (Pawn p in prisoner)
                {
                    pawnSaved++;
                    if (Rand.Chance(UnWillingChance))
                    {
                        pawnStaying++;
                        p.mindState.WillJoinColonyIfRescued = false;
                    }
                    
                }
                if (pawnStaying >= 3)
                {
                    resurrectSet = true;
                }
                if(pawnSaved>0)
                    prisoner[0].Faction.TryAffectGoodwillWith(Faction.OfPlayer, 10*pawnSaved);
                // Balance
                Gift_RewardGeneratorBasedTMagic gift = new Gift_RewardGeneratorBasedTMagic();
                List<Thing> list = new List<Thing>();
                list = gift.Generate(500, list);
                Map target = Find.AnyPlayerHomeMap;
                IntVec3 intVec3 = DropCellFinder.TradeDropSpot(target);
                DropPodUtility.DropThingsNear(intVec3, target, (IEnumerable<Thing>)list, 110, false, true, true);
                string text = TranslatorFormattedStringExtensions.Translate("SettlementRescueWin", this.parent, (NamedArgument)TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks),this.parent.Faction.leader ,this.parent.Faction.def.leaderTitle) + GenLabel.ThingsLabel(list);
                
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementRescue".Translate(), text , LetterDefOf.PositiveEvent, (LookTargets)((WorldObject)this.parent), (Faction)null, (string)null);
                return true;
            }
            return false;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "SettlementResuce_active", defaultValue : false);
            Scribe_Values.Look(ref threat, "SettlementResuce_threat", defaultValue: true);
            Scribe_Values.Look(ref resurrectSet, "SettlementResuce_resurrectSet", defaultValue: false);
            Scribe_Values.Look(ref wasEntered, "SettlementResuce_wasEntered", defaultValue: false);
            Scribe_Values.Look(ref ID, "SettlementResuce_ID");
            Scribe_References.Look(ref ally, "SettlementResuce_ally");
        }
    }

    public class WorldObjectCompProperties_SiteResuce : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SiteResuce()
        {
            this.compClass = typeof(WorldComp_SettlementResuce);
        }
    }
}
