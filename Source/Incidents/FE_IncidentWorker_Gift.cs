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
    class FE_IncidentWorker_Gift : IncidentWorker
    {

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Faction faction;
            return base.CanFireNowSub(parms) && this.TryFindFactions(out faction);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            
            Faction faction;
            if (!this.TryFindFactions(out faction))
                return false;
            Settlement sis = (from f in Find.WorldObjects.Settlements
                              where f.Faction == faction && f.Spawned
                              select f).RandomElement();
            Tile from = Find.WorldGrid[sis.Tile - 1];
            from.potentialRoads = new List<Tile.RoadLink>();
            Tile to = Find.WorldGrid[sis.Tile -2];
            to.potentialRoads = new List<Tile.RoadLink>();
            
            from.potentialRoads.Add(new Tile.RoadLink { neighbor = sis.Tile - 2, road = RoadDefOf.AncientAsphaltHighway });
            to.potentialRoads.Add(new Tile.RoadLink { neighbor = sis.Tile - 1, road = RoadDefOf.AncientAsphaltHighway });

            Find.World.renderer.SetDirty<WorldLayer_Roads>();
            Find.World.renderer.SetDirty<WorldLayer_Paths>();

            Map target = (Map)parms.target;
            List<Thing> thingList = GenerateRewards(faction, parms);
            IntVec3 intVec3 = DropCellFinder.TradeDropSpot(target);
            DropPodUtility.DropThingsNear(intVec3, target, (IEnumerable<Thing>)thingList, 110, false, true, true);
            string itemList = "";
            for (int i = 0; i < thingList.Count(); i++)
            {
                itemList += thingList[i].Label + "\n\n";
            }
            Find.LetterStack.ReceiveLetter("LetterLabelGift".Translate(), "Gift".Translate(faction, sis) + itemList
            , LetterDefOf.PositiveEvent, (LookTargets)new TargetInfo(intVec3, target, false), faction, (string)null);

            return true;
        }
        private List<Thing> GenerateRewards(Faction alliedFaction, IncidentParms parms)
        {
            if (Utilities.FactionsWar().GetByFaction(parms.faction) == null)
                return new List<Thing>();
            int totalMarketValue = (int)Mathf.Clamp(1000 * (0.99f + 0.01f * Utilities.FactionsWar().GetByFaction(parms.faction).disposition), 1000, 4000);
            List<Thing> list = new List<Thing>();
            Gift_RewardGeneratorBasedTMagic itc_ia = new Gift_RewardGeneratorBasedTMagic();
            return itc_ia.Generate(totalMarketValue, list);
        }
        private bool TryFindFactions(out Faction alliedFaction)
        {
            if ((from x in Find.FactionManager.AllFactions
                 where !x.IsPlayer && x.PlayerRelationKind== FactionRelationKind.Ally && !x.defeated
                 select x).TryRandomElement(out alliedFaction))
            {
                return true;
            }
            alliedFaction = null;
            return false;
        }
    }
}
