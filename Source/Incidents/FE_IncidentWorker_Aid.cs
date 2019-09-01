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
    class FE_IncidentWorker_Aid : IncidentWorker
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
            IEnumerable<Pawn> enumerableFood, enumerableInjured;
            if (Find.CurrentMap.IsPlayerHome)
            {
                enumerableFood = from pawn in Find.CurrentMap.mapPawns.AllPawns
                                 where pawn.Faction.IsPlayer && pawn.IsFreeColonist && pawn.Starving()
                                 select pawn;
                enumerableInjured = from pawn in Find.CurrentMap.mapPawns.AllPawns
                                    where pawn.Faction.IsPlayer && pawn.Downed && pawn.IsFreeColonist
                                    select pawn;
            }

            else return false;
            if (enumerableFood.Count() == 0 && enumerableInjured.Count() == 0)
                return false;
            Settlement sis;
            if (!(from f in Find.WorldObjects.Settlements
                  where f.Faction == faction && f.Spawned
                  select f).TryRandomElement(out sis))
                return false;
            Map target = (Map)parms.target;
            List<Thing> thingList = GenerateRewards(faction, enumerableFood.Count(), enumerableInjured.Count(), parms);
            IntVec3 intVec3 = DropCellFinder.TradeDropSpot(target);
            DropPodUtility.DropThingsNear(intVec3, target, (IEnumerable<Thing>)thingList, 110, false, true, true);

            Find.LetterStack.ReceiveLetter("FFE_LetterLabelAid".Translate(), "FFE_Aid".Translate(sis, faction) + GenLabel.ThingsLabel(thingList,string.Empty)
            , LetterDefOf.PositiveEvent, (LookTargets)new TargetInfo(intVec3, target, false), faction, (string)null);

            return true;
        }
        private List<Thing> GenerateRewards(Faction alliedFaction, int foodCount, int injuredCount, IncidentParms parms)
        {
            if (Utilities.FactionsWar().GetByFaction(parms.faction) == null)
                return new List<Thing>();
            int totalMarketValue = (int)Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(parms.target)*5 * (1f + 0.03f * -Utilities.FactionsWar().GetByFaction(parms.faction).disposition), 200, 1000);
            List<Thing> list = new List<Thing>();
            Aid_RewardGeneratorBasedTMagic itc_ia = new Aid_RewardGeneratorBasedTMagic();
            return itc_ia.Generate(totalMarketValue, foodCount, injuredCount, list, alliedFaction);
        }
        private bool TryFindFactions(out Faction alliedFaction)
        {
            if ((from x in Find.FactionManager.AllFactions
                 where !x.IsPlayer && x.PlayerRelationKind== FactionRelationKind.Ally && !x.def.techLevel.IsNeolithicOrWorse()
                 select x).TryRandomElement(out alliedFaction))
            {
                return true;
            }
            alliedFaction = null;
            return false;
        }
    }
}
