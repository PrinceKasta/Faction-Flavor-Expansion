using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Harmony;

namespace Flavor_Expansion
{
    class SitePartWorker_Outpost_Defense : SitePartWorker_Outpost
    {
        public FloatRange pointsRange = new FloatRange(50f, 300f);
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            
            Faction faction = (from f in Find.FactionManager.AllFactions
                               where f.HostileTo(map.ParentFaction) && f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction && !f.def.hidden
                               select f).RandomElement();
            Faction ally = (from x in Find.FactionManager.AllFactions
                            where !x.def.permanentEnemy && !x.IsPlayer && x.PlayerRelationKind== FactionRelationKind.Ally && !x.defeated
                            select x).RandomElement();
            List<Pawn> pawns = map.mapPawns.FreeHumanlikesSpawnedOfFaction(ally).ToList();
            float factionStrength = StorytellerUtility.DefaultThreatPointsNow(map) * 5+3.5f;
            float num = this.pointsRange.RandomInRange + factionStrength;
            int count = pawns.Count();
            for (int i=0;i<count;i++)
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
            if(num>0)
            {
                SpawnPawnsFromPoints(num, CellFinder.RandomSpawnCellForPawnNear(pawns.RandomElement().RandomAdjacentCellCardinal(), map, 10), ally, map);
            }
            var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            var threatparms = storyComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, map);
            threatparms.faction = faction;
            threatparms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            threatparms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            threatparms.raidArrivalModeForQuickMilitaryAid = true;
            threatparms.points = 300 + StorytellerUtility.DefaultThreatPointsNow(map)*12;
            threatparms.raidNeverFleeIndividual = true;
            IncidentDefOf.RaidEnemy.Worker.TryExecute(threatparms);
            foreach(Pawn p in map.mapPawns.AllPawns)
                map.mapPawns.UpdateRegistryForPawn(p);
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
    }
}
