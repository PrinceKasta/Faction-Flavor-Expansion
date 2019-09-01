using Harmony;
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
    public class Utilities
    {

        public static bool Reachable(Settlement from, Settlement to, int minDist = int.MaxValue)
        {
            return Reachable(from.Tile, to.Tile, minDist);
        }

        public static bool Reachable(int from, int to, int minDist = int.MaxValue)
        {
            return Find.WorldGrid.ApproxDistanceInTiles(from, to) < minDist && Find.WorldReachability.CanReach(from, to);
        }

        public static List<Pawn> GenerateFighter(float points, Lord lord, List<PawnKindDef> kindDefs, Map map, Faction faction ,IntVec3 vec3 ,bool toList=false)
        {
            Pawn fighter = new Pawn();
            List<Pawn> pawns = new List<Pawn>();
            PawnKindDef def = new PawnKindDef();
            while (points > 0)
            {
                PawnGenerationRequest generationRequest = new PawnGenerationRequest(kindDefs.Where(x=> x.RaceProps.Humanlike).TryRandomElementByWeight(x =>  x.combatPower, out def) ? def : null, faction, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, true);
                
                fighter = PawnGenerator.GeneratePawn(generationRequest);
                fighter.mindState.canFleeIndividual = false;
                if(fighter.equipment != null &&fighter.equipment.Primary!=null &&fighter.equipment.Primary.def.thingSetMakerTags !=null && fighter.equipment.Primary.def.thingSetMakerTags.Contains("SingleUseWeapon"))//.Where(x=> x.def.IsWeaponUsingProjectiles && x.def.thingSetMakerTags.Contains("SingleUseWeapon")).Any())
                {
                    fighter.equipment.Primary.Destroy();
                    fighter.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Revolver")));
                }
                points -= fighter.kindDef.combatPower;
                

                if (!toList)
                {
                    lord.AddPawn(fighter);
                    GenSpawn.Spawn(fighter, vec3, map);
                    vec3 = fighter.RandomAdjacentCell8Way();
                    vec3.ClampInsideMap(map);
                    map.mapPawns.UpdateRegistryForPawn(fighter);
                }
                else pawns.Add(fighter);
            }
            Log.Warning(pawns.Count.ToString());
            return pawns;
        }

        public static List<PawnKindDef> GeneratePawnKindDef(int combatpower, Faction faction)
        {
            List<PawnKindDef> kindDefs = new List<PawnKindDef>();
            kindDefs.Clear();
            foreach (PawnKindDef def in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (def.combatPower >= combatpower && def.RaceProps.Humanlike && !def.defName.Contains("StrangerInBlack") && def != PawnKindDefOf.WildMan && !def.defName.Contains("WildMan") && (faction.def.techLevel == TechLevel.Neolithic ? !def.defName.Contains("Town") : !def.defName.Contains("Tribal")))
                    kindDefs.Add(def);
            }
            if(kindDefs.Exists(x=> x == PawnKindDefOf.WildMan || x.defName.Contains("WildMan")))
            {
                Log.Error("WTF, "+kindDefs.Find(x=> x.defName.Contains("WildMan")).defName);
                kindDefs.Remove(PawnKindDefOf.WildMan);
            }
            return kindDefs;
        }

        public static FE_WorldComp_FactionsWar FactionsWar()
        {
            return Find.World.GetComponent<FE_WorldComp_FactionsWar>();
        }
    }
}