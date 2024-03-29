﻿using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{

    class FE_IncidentWorker_FactionWar_CaravanSkirmish : IncidentWorker_Ambush
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && TryFindCaravanProximity(out Caravan caravan, out Settlement set) && EndGame_Settings.FactionWar ;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryFindCaravanProximity(out Caravan caravan, out Settlement set) || !EndGame_Settings.FactionWar)
                return false;
            List<Pawn> pawnsF1 = new List<Pawn>();
            List<Pawn> pawnsF2 = new List<Pawn>();
            War war = Utilities.FactionsWar().GetWars().FirstOrDefault(x => x.TryFindFactioninvolved(set.Faction) && (x.AttackerFaction().HostileTo(Faction.OfPlayer) || x.DefenderFaction().HostileTo(Faction.OfPlayer)));

            for (int i = 0; i < 2; i++)
            {
                List<PawnKindDef> kindDefs = new List<PawnKindDef>()
                {
                    DefDatabase<PawnKindDef>.GetNamed("Mercenary_Elite"),
                    DefDatabase<PawnKindDef>.GetNamed("Town_Guard"),
                    DefDatabase<PawnKindDef>.GetNamed("Grenadier_Destructive")
                };
                if (i == 0)
                    pawnsF1 = Utilities.GenerateFighter(Math.Max(def.minThreatPoints, Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources * 0.1f), null, kindDefs, null, war.DefenderFaction(), new IntVec3(), true);
                else pawnsF2 =Utilities.GenerateFighter(Math.Max(def.minThreatPoints, Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources * 0.1f), null, kindDefs, null, war.AttackerFaction(), new IntVec3(), true);
            }
            if (pawnsF1.NullOrEmpty() || pawnsF2.NullOrEmpty())
                return false;
            LongEventHandler.QueueLongEvent(() => DoExecute(parms, pawnsF1, pawnsF2), "GeneratingMapForNewEncounter", false, null);
            return true;

        }
        protected override List<Pawn> GeneratePawns(IncidentParms parms)
        {
            return new List<Pawn>();
        }

        protected bool DoExecute(IncidentParms parms , List<Pawn> f1, List<Pawn> f2)
        {
            bool flag = false;
            if (!(parms.target is Map map))
            {
                map = SetupCaravanAttackMap((Caravan)parms.target, f1, f2, false);
                flag = true;
            }
            else
            {
                string letterLabel = "";
                string letterText = "";
                Lord lord = LordMaker.MakeNewLord(f1[0].Faction, new LordJob_AssaultColony(f1[0].Faction, true, false, false, false, true), map, f1);
                Lord lord2 = LordMaker.MakeNewLord(f2[0].Faction, new LordJob_AssaultColony(f2[0].Faction, true, false, false, false, true), map, f2);
                for (int index = 0; index < f1.Count; ++index)
                {

                    GenSpawn.Spawn(f1[index], f1[index].InteractionCell, map, Rot4.Random, WipeMode.Vanish, false);
                    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(f1, ref letterLabel, ref letterText, GetRelatedPawnsInfoLetterText(parms), true, true);
                }
                for (int index = 0; index < f2.Count; ++index)
                {
                    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(f2, ref letterLabel, ref letterText, GetRelatedPawnsInfoLetterText(parms), true, true);
                    GenSpawn.Spawn(f2[index], f2[index].InteractionCell, map, Rot4.Random, WipeMode.Vanish, false);
                }
            }
            Find.LetterStack.ReceiveLetter(def.letterLabel, def.letterText.Formatted((parms.target as Caravan).Name, f1[0].Faction.def.pawnsPlural, f1[0].Faction, f2[0].Faction.def.pawnsPlural, f2[0].Faction)
                    , LetterDefOf.ThreatBig, new LookTargets(map.mapPawns.FreeColonists.RandomElement()), f1[0].Faction, null);
            if (flag)
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
            return true;
        }
        public static Map SetupCaravanAttackMap(Caravan caravan,List<Pawn> f1, List<Pawn> f2, bool sendLetterIfRelatedPawns)
        {
            int incidentMapSize = CaravanIncidentUtility.CalculateIncidentMapSize(caravan.PawnsListForReading, f1.Concat(f2).ToList());
            Map map = CaravanIncidentUtility.GetOrGenerateMapForIncident(caravan, new IntVec3(incidentMapSize, 1, incidentMapSize), WorldObjectDefOf.Ambush);
            IntVec3 vec3;
            map.GetComponent<FE_MapComp_Skirmish>().StartComp(f2[0].Faction, f1[0].Faction);
            CaravanEnterMapUtility.Enter(caravan, map, p => CellFinder.RandomSpawnCellForPawnNear(new IntVec3(map.Center.x, 0, map.Center.z + +(Rand.Chance(0.5f) ? 50 : -50)).ClampInsideMap(map), map, 4), CaravanDropInventoryMode.DoNotDrop,true);
            for (int index = 0; index < 2; ++index)
            {
                if (index == 0)
                {
                    Lord lord = LordMaker.MakeNewLord(f1[0].Faction, new LordJob_AssaultColony(f1[0].Faction, true, false, false, false, true), map, f1);
                    CellFinder.TryFindRandomCellNear(new IntVec3(map.Center.x - 30, map.Center.y, map.Center.z), map, 5, x => x.Standable(map), out vec3);
                    for (int i = 0; i < f1.Count(); i++)
                    {
                        GenSpawn.Spawn(f1[i], CellFinder.RandomSpawnCellForPawnNear(vec3, map, 4), map, Rot4.Random, WipeMode.Vanish, false);
                    }
                }
                else
                {
                    Lord lord = LordMaker.MakeNewLord(f1[0].Faction, new LordJob_AssaultColony(f1[0].Faction, true, false, false, false, true), map, f2);
                    CellFinder.TryFindRandomCellNear(new IntVec3(map.Center.x + 30, map.Center.y, map.Center.z), map, 5, x => x.Standable(map), out vec3);
                    for (int i = 0; i < f2.Count(); i++)
                    {
                        GenSpawn.Spawn(f2[i], CellFinder.RandomSpawnCellForPawnNear(vec3, map, 4), map, Rot4.Random, WipeMode.Vanish, false);
                    }
                }
            }
            if (sendLetterIfRelatedPawns)
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(f1.Concat(f2), "LetterRelatedPawnsGroupGeneric".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, true, true);
            return map;
        }
        private bool TryFindCaravanProximity(out Caravan caravan, out Settlement set)
        {
            List<War> wars= Utilities.FactionsWar().GetWars().Where(x=> x.AttackerFaction().HostileTo(Faction.OfPlayer) || x.DefenderFaction().HostileTo(Faction.OfPlayer)).ToList();
            foreach(Caravan c in Find.WorldObjects.Caravans.ToList())
            {
                if(Find.WorldObjects.Settlements.Where(x=> Find.WorldGrid.ApproxDistanceInTiles(c.Tile, x.Tile)<30 && wars.Where(war=> war.TryFindFactioninvolved(x.Faction)).Any()).TryRandomElement(out set))
                {
                    caravan = c;
                    return true;
                }
            }
            set = null;
            caravan = null;
            return false;
        }

    }
}
