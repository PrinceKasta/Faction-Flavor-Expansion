using System;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;

namespace Flavor_Expansion
{
    public class GenStep_Rescue : GenStep_Scatterer
    {
        private static readonly IntRange SettlementSizeRange = new IntRange(34, 38);

        public override int SeedPart
        {
            get
            {
                return 1806208471;
            }
        }

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            if (!base.CanScatterAt(c, map) || !c.Standable(map) || c.Roofed(map) || !map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
            {
                return false;
            }

            int min = SettlementSizeRange.min;
            return new CellRect(c.x - (min / 2), c.z - (min / 2), min, min).FullyContainedWithin(new CellRect(0, 0, map.Size.x, map.Size.z));
        }

        protected override void ScatterAt(IntVec3 c, Map map, int stackCount = 1)
        {
            int randomInRange1 = SettlementSizeRange.RandomInRange;
            int randomInRange2 = SettlementSizeRange.RandomInRange;
            CellRect cellRect = new CellRect(map.Center.x - (randomInRange1 / 2), map.Center.z - (randomInRange2 / 2), randomInRange1, randomInRange2);

            cellRect.ClipInsideMap(map);

            BaseGen.globalSettings.minBuildings = 1;
            BaseGen.globalSettings.minBarracks = 1;
            BaseGen.symbolStack.Push("settlement", new ResolveParams
            {
                rect = cellRect,
                faction = map.ParentFaction == null || map.ParentFaction == Faction.OfPlayer ? Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined) : map.ParentFaction
            });
            
            for (int i = 0; i < 3; i++)
            {

                if (!RCellFinder.TryFindRandomCellOutsideColonyNearTheCenterOfTheMap(map.AllCells.Where(x=> x.Walkable(map)&& !x.Fogged(map)).RandomElement() , map, 25, out IntVec3 v))
                {
                    Log.Error("genstep: didnt find random cell " + i+"index");
                    return;
                }
                CellRect var = CellRect.CenteredOn(v, 8, 8).ClipInsideMap(map);
                Pawn pawn = map.Parent.GetComponent<PrisonerWillingToJoinComp>() == null || !map.Parent.GetComponent<PrisonerWillingToJoinComp>().pawn.Any ? PrisonerWillingToJoinQuestUtility.GeneratePrisoner(map.Tile, map.ParentFaction) : map.Parent.GetComponent<PrisonerWillingToJoinComp>().pawn.Take(map.Parent.GetComponent<PrisonerWillingToJoinComp>().pawn[0]);
                if (pawn.equipment != null && pawn.equipment.AllEquipmentListForReading.Count > 0)
                    pawn.equipment.DestroyAllEquipment();
                pawn.SetFaction(map.ParentFaction);
                BaseGen.globalSettings.map = map;
                BaseGen.symbolStack.Push("prisonCell", new ResolveParams
                {
                    rect = var,
                    faction = map.ParentFaction
                });
                BaseGen.Generate();
                CellRect rect = new CellRect(var.CenterCell.x, var.CenterCell.z, 1, 1);
                rect.ClipInsideMap(map);
                pawn.guest.SetGuestStatus(map.ParentFaction, true);
                BaseGen.globalSettings.map = map;
                BaseGen.symbolStack.Push("pawn", new ResolveParams
                {
                    rect = rect,
                    faction = map.ParentFaction,
                    singlePawnToSpawn = pawn,
                    postThingSpawn = x =>
                    {
                        MapGenerator.rootsToUnfog.Add(x.Position);
                        ((Pawn)x).mindState.WillJoinColonyIfRescued = true;

                    }
                });
                BaseGen.Generate();
                MapGenerator.SetVar("RectOfInterest", var);
                RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(x =>
               !x.Roofed(map) && x.IsValid, map, out IntVec3 k);
                k.ClampInsideMap(map);
                MapGenerator.rootsToUnfog.Add(k);
            }
            
        }
    }
}
