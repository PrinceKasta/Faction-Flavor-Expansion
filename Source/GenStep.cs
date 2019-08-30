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
            if (!base.CanScatterAt(c, map) || !c.Standable(map) || (c.Roofed(map) || !map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false))))
                return false;
            int min = GenStep_Rescue.SettlementSizeRange.min;
            return new CellRect(c.x - min / 2, c.z - min / 2, min, min).FullyContainedWithin(new CellRect(0, 0, map.Size.x, map.Size.z));
        }

        protected override void ScatterAt(IntVec3 c, Map map, int stackCount = 1)
        {
            int randomInRange1 = GenStep_Rescue.SettlementSizeRange.RandomInRange;
            int randomInRange2 = GenStep_Rescue.SettlementSizeRange.RandomInRange;
            CellRect cellRect = new CellRect(map.Center.x - randomInRange1 / 2, map.Center.z - randomInRange2 / 2, randomInRange1, randomInRange2);//new CellRect(c.x - randomInRange1 / 2, c.z - randomInRange2 / 2, randomInRange1, randomInRange2);
            
            Faction faction = map.ParentFaction == null || map.ParentFaction == Faction.OfPlayer ? Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined) : map.ParentFaction;
            cellRect.ClipInsideMap(map);
            
            ResolveParams resolveParams = new ResolveParams();
            resolveParams.rect = cellRect;
            resolveParams.faction = faction;
            RimWorld.BaseGen.BaseGen.globalSettings.minBuildings = 1;
            RimWorld.BaseGen.BaseGen.globalSettings.minBarracks = 1;
            RimWorld.BaseGen.BaseGen.symbolStack.Push("settlement", resolveParams);
            IntVec3 v;
            for (int i = 0; i < 3; i++)
            {

                if (!RCellFinder.TryFindRandomCellOutsideColonyNearTheCenterOfTheMap(map.AllCells.Where(x=> x.Walkable(map)&& !x.Fogged(map)).RandomElement() , map, 25, out v))
                {
                    Log.Error("genstep: didnt find random cell " + i+"index");
                    return;
                }
                Faction hostFaction = map.ParentFaction;
                CellRect var = CellRect.CenteredOn(v, 8, 8).ClipInsideMap(map);
                PrisonerWillingToJoinComp component = map.Parent.GetComponent<PrisonerWillingToJoinComp>();
                Pawn pawn = component == null || !component.pawn.Any ? PrisonerWillingToJoinQuestUtility.GeneratePrisoner(map.Tile, hostFaction) : component.pawn.Take((Thing)component.pawn[0]);
                if (pawn.equipment != null && pawn.equipment.AllEquipmentListForReading.Count > 0)
                    pawn.equipment.DestroyAllEquipment();
                pawn.SetFaction(map.ParentFaction);
                ResolveParams resolveParams1 = new ResolveParams();
                resolveParams1.rect = var;
                resolveParams1.faction = hostFaction;
                RimWorld.BaseGen.BaseGen.globalSettings.map = map;
                RimWorld.BaseGen.BaseGen.symbolStack.Push("prisonCell", resolveParams1);
                RimWorld.BaseGen.BaseGen.Generate();
                ResolveParams resolveParams2 = new ResolveParams();
                CellRect rect = new CellRect(var.CenterCell.x, var.CenterCell.z, 1, 1);
                rect.ClipInsideMap(map);
                pawn.guest.SetGuestStatus(hostFaction, true);
                resolveParams2.rect = rect;
                resolveParams2.faction = hostFaction;
                resolveParams2.singlePawnToSpawn = pawn;
                resolveParams2.postThingSpawn = (Action<Thing>)(x =>
                {
                    MapGenerator.rootsToUnfog.Add(x.Position);
                    ((Pawn)x).mindState.WillJoinColonyIfRescued = true;
                    
                });
                
                RimWorld.BaseGen.BaseGen.globalSettings.map = map;
                RimWorld.BaseGen.BaseGen.symbolStack.Push("pawn", resolveParams2);
                RimWorld.BaseGen.BaseGen.Generate();
                MapGenerator.SetVar<CellRect>("RectOfInterest", var);
                IntVec3 k;
                RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((Predicate<IntVec3>) (x=>
                !x.Roofed(map)&&x.IsValid), map, out k);
                k.ClampInsideMap(map);
                MapGenerator.rootsToUnfog.Add(k);
            }
            
        }
    }
}
