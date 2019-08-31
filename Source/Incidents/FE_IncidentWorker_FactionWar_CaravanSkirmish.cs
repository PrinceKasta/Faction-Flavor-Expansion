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
    class FE_IncidentWorker_FactionWar_CaravanSkirmish : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Caravan caravan;
            Settlement set;
            return base.CanFireNowSub(parms) && TryFindCaravanProximity(out caravan, out set) && EndGame_Settings.FactionWar ;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Settlement set;
            Caravan caravan;
            if (!TryFindCaravanProximity(out caravan, out set) || !EndGame_Settings.FactionWar)
                return false;
            Site site = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, SitePartDefOf.AmbushEdge, caravan.Tile, set.Faction);
            War war = Utilities.FactionsWar().GetWars().FirstOrDefault(x => x.AttackerFaction() == site.Faction || x.DefenderFaction() == site.Faction);

            IntVec3 vec3;
            MapGenerator.GenerateMap(new IntVec3(100, 1, 100), site, MapGeneratorDefOf.Encounter);
            CellFinder.TryFindRandomCellNear(new IntVec3(site.Map.Center.x - 20, site.Map.Center.y, site.Map.Center.z), site.Map, 5, x => x.Standable(site.Map), out vec3);
            for (int i = 0; i < 2; i++)
            {
                if (i == 1)
                {
                    site.SetFaction(set.Faction == war.AttackerFaction() ? war.DefenderFaction() : war.AttackerFaction());
                    CellFinder.TryFindRandomCellNear(new IntVec3(site.Map.Center.x + 20, site.Map.Center.y, site.Map.Center.z), site.Map, 5, x => x.Standable(site.Map), out vec3);
                }
                Lord lord = LordMaker.MakeNewLord(set.Faction, new LordJob_DefendPoint(vec3), site.Map);
                PawnGroupMakerParms parmsGroup = new PawnGroupMakerParms();
                parmsGroup.faction = site.Faction;
                parmsGroup.points = parms.points;
                parmsGroup.groupKind = PawnGroupKindDefOf.Combat;
                IEnumerable<Pawn> pawns = PawnGroupMakerUtility.GeneratePawns(parmsGroup);
                foreach (Pawn p in pawns)
                {
                    GenSpawn.Spawn(p, vec3, site.Map);
                    vec3 = p.RandomAdjacentCell8Way().ClampInsideMap(site.Map);
                    lord.AddPawn(p);
                    site.Map.mapPawns.UpdateRegistryForPawn(p);
                }
                
                List<PawnKindDef> kindDefs = new List<PawnKindDef>();
                kindDefs.Add(DefDatabase<PawnKindDef>.GetNamed("Mercenary_Elite"));
                kindDefs.Add(DefDatabase<PawnKindDef>.GetNamed("Town_Guard"));
                kindDefs.Add(DefDatabase<PawnKindDef>.GetNamed("Grenadier_Destructive"));
                Utilities.GenerateFighter(StorytellerUtility.DefaultThreatPointsNow(parms.target)*5+ Utilities.FactionsWar().GetResouceAmount(set.Faction)*0.1f, lord, kindDefs, site.Map, site.Faction,vec3);
            }
            site.GetComponent<WorldObjectComp_Skirmish>().StartComp(set.Faction);
            Find.WorldObjects.Add(site);
            CaravanEnterMapUtility.Enter(caravan, site.Map, CaravanEnterMode.Center,CaravanDropInventoryMode.DoNotDrop,true);
            Find.LetterStack.ReceiveLetter("LetterLabelSkirmish".Translate(), "Skirmish".Translate(caravan.Label, war.AttackerFaction(), war.DefenderFaction())
                    , LetterDefOf.ThreatBig, site, site.Faction, (string)null);
            return true;

        }

        private bool TryFindCaravanProximity(out Caravan caravan, out Settlement set)
        {
            List<War> wars= Utilities.FactionsWar().GetWars().Where(x=> x.AttackerFaction().HostileTo(Faction.OfPlayer) || x.DefenderFaction().HostileTo(Faction.OfPlayer)).ToList();
            foreach(Caravan c in Find.WorldObjects.Caravans.ToList())
            {
                if(Find.WorldObjects.Settlements.Where(x=> Find.WorldGrid.ApproxDistanceInTiles(c.Tile, x.Tile)<15 && (wars.Where(war=> war.TryFindFactioninvolved(x.Faction)).Any())).TryRandomElement(out set))
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
