using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class WorldComp_SettlementResuce : WorldObjectComp
    {
        private static readonly IntRange CorpseAmountRange = new IntRange(6, 14);
        private const float UnWillingChance = 0.75f;
        private bool active = false, resurrectSet = false;
        private int pawnStaying = 0;
        private Faction ally;

        public void StartComp(Faction ally)
        {
            active = true;
            this.ally = ally;
        }
        public override void CompTick()
        {
            if (!active)
                return;
            if (parent.GetComponent<TimeoutComp>().Passed && !((MapParent)parent).HasMap)
            {
                ally.TryAffectGoodwillWith(Faction.OfPlayer, -30);
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementRescueIgnored".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementRescueIgnored", ally.leader, ally.def.leaderTitle), LetterDefOf.NegativeEvent, null, ally);
                active = false;
            }
            if (HostileDefeated())
            {
                parent.GetComponent<TimedForcedExit>().StartForceExitAndRemoveMapCountdown(Global.DayInTicks);
            }
        }
        public override void PostMyMapRemoved()
        {
            if (active)
            {
                ally.TryAffectGoodwillWith(Faction.OfPlayer, -20, false, true, TranslatorFormattedStringExtensions.Translate("SettlementRescueAbandoned", ally.leader, ally.def.leaderTitle, (-20).ToString()));
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementRescueIgnored".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementRescueAbandoned", ally.leader, ally.def.leaderTitle, (-20).ToString()), LetterDefOf.NegativeEvent, null, ally);
                active = false;
            }
            else
            {
                Ressurect();
            }
        }

        private void Ressurect()
        {
            if (resurrectSet && !ally.defeated)
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
            MapParent map = (MapParent)parent;

            foreach (var thing in map.Map.listerThings.AllThings)
            {
                if (!(thing is Pawn) && thing.def.Claimable)
                {
                    if (thing.def.CanHaveFaction)
                        thing.SetFactionDirect(map.Map.ParentFaction);
                }
            }
            SpawnAdditonalPrisoners(map.Map);
            SpawnCorpses(map.Map);
        }
        private void SpawnAdditonalPrisoners(Map map)
        {
            PrisonerWillingToJoinComp component = parent.GetComponent<PrisonerWillingToJoinComp>();
            List<Pawn> prisoner = map.mapPawns.AllPawnsSpawned.Where(p=> p.kindDef == PawnKindDefOf.Slave || p.IsPrisoner).ToList();
            DamageInfo info = new DamageInfo(DamageDefOf.Blunt, 20, 0);
            info.SetAllowDamagePropagation(true);

            for (int i = 0; i <  prisoner.ToList().Count; i++)
            {
                prisoner[i].SetFaction(ally);
                prisoner[i].guest.SetGuestStatus(map.ParentFaction, true);
                prisoner[i].mindState.WillJoinColonyIfRescued = true;
                Pawn pawn = component == null || !component.pawn.Any ? PrisonerWillingToJoinQuestUtility.GeneratePrisoner(map.Tile, map.ParentFaction) : component.pawn.Take(component.pawn[0]);
                pawn.mindState.WillJoinColonyIfRescued = true;

                pawn.SetFaction(ally);
                IntVec3 result = CellFinder.RandomSpawnCellForPawnNear(prisoner[i % prisoner.ToList().Count].CellsAdjacent8WayAndInside().Where(x=>x.Standable(map) && x.Walkable(map)).RandomElement(), map);
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
            bool baseValidator(IntVec3 x)
            {
                x.Walkable(map);
                return map.reachability.CanReachMapEdge(x, TraverseParms.For(TraverseMode.PassAllDestroyableThings, Danger.Deadly, false));
            }
            int corpseAmount = CorpseAmountRange.RandomInRange;
            for (int i = 0; i < corpseAmount; i++)
            {
                Pawn corpse = PawnGenerator.GeneratePawn(PawnKindDefOf.Villager, ally);
                if (corpse.inventory.innerContainer.Count > 0)
                    corpse.inventory.DestroyAll();
                if (!CellFinder.TryFindRandomCellNear(map.Center, map, 20, baseValidator, out IntVec3 result))
                    return;
                GenSpawn.Spawn(corpse, result, map);
                corpse.Kill(parent.Faction.def.techLevel.IsNeolithicOrWorse() ? new DamageInfo(DamageDefOf.Cut, 25) : new DamageInfo(DamageDefOf.Bullet, 40));
            }
        }
        private bool HostileDefeated()
        {
            MapParent map = (MapParent)parent;
            if (map.HasMap && !GenHostility.AnyHostileActiveThreatTo(map.Map, Faction.OfPlayer))
            {
                active = false;
                List<Pawn> prisoner= map.Map.mapPawns.AllPawns.Where(x => !x.Dead && !x.Downed && (x.IsPrisoner || x.kindDef == PawnKindDefOf.Slave)).ToList();
                int pawnSaved = prisoner.Count(x => !x.Dead);
                if (pawnSaved == 0)
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelSettlementRescueFail".Translate(), TranslatorFormattedStringExtensions.Translate("SettlementRescueFail", parent.Faction, TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks),
                    ally.leader, ally.def.pawnsPlural), LetterDefOf.NegativeEvent, parent, null, null);
                    return true;
                }
                else
                {
                    prisoner[0].Faction.TryAffectGoodwillWith(Faction.OfPlayer, 10 * pawnSaved);

                }
               
                foreach (Pawn p in prisoner)
                {
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
                DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap), Find.AnyPlayerHomeMap, new Gift_RewardGeneratorBasedTMagic().Generate(500, new List<Thing>()), 110, false, true, true);
                string text = TranslatorFormattedStringExtensions.Translate("SettlementRescueWin", parent.Faction, TimedForcedExit.GetForceExitAndRemoveMapCountdownTimeLeftString(Global.DayInTicks), ally.leader , ally.def.leaderTitle) + GenLabel.ThingsLabel(new Gift_RewardGeneratorBasedTMagic().Generate(500, new List<Thing>()), string.Empty);
                Find.LetterStack.ReceiveLetter("LetterLabelSettlementRescue".Translate(), text , LetterDefOf.PositiveEvent, parent, null, null);
                return true;
            }
            return false;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref active, "SettlementResuce_active", defaultValue : false);
            Scribe_Values.Look(ref resurrectSet, "SettlementResuce_resurrectSet", defaultValue: false);
            Scribe_References.Look(ref ally, "SettlementResuce_ally");
        }
    }

    public class WorldObjectCompProperties_SiteResuce : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SiteResuce() => compClass = typeof(WorldComp_SettlementResuce);
    }
}
