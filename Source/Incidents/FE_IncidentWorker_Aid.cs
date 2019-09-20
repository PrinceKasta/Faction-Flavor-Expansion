using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{
    class FE_IncidentWorker_Aid : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && TryFindFactions(out Faction faction) && TryFindStravingPawns(out IEnumerable<Pawn> enumerableFood, (Map)parms.target) && !TryFindInjuredPawns(out IEnumerable<Pawn> enumerableInjured, (Map)parms.target);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map target = (Map)parms.target;
            if (!TryFindFactions(out Faction faction) || !TryFindStravingPawns(out IEnumerable<Pawn> enumerableFood, target) || !TryFindInjuredPawns(out IEnumerable<Pawn> enumerableInjured, target))
                return false;
            
            List<Thing> thingList = GenerateRewards(faction, enumerableFood.Count(), enumerableInjured.Count(), parms);
            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(target), target, thingList, 110, false, true, true);
            Find.LetterStack.ReceiveLetter("FFE_LetterLabelAid".Translate(), "FFE_Aid".Translate(faction.leader, faction.def.leaderTitle, faction, GenLabel.ThingsLabel(thingList, string.Empty)) 
            , LetterDefOf.PositiveEvent, new TargetInfo(DropCellFinder.TradeDropSpot(target), target, false), faction, null);
            return true;
        }
        private List<Thing> GenerateRewards(Faction alliedFaction, int foodCount, int injuredCount, IncidentParms parms) => Utilities.FactionsWar().GetByFaction(parms.faction) == null
                ? new List<Thing>()
                : new Aid_RewardGeneratorBasedTMagic().Generate((int)Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(parms.target) * 5 * (1f + (0.03f * -Utilities.FactionsWar().GetByFaction(parms.faction).disposition)), 200, 1000), foodCount, injuredCount, new List<Thing>(), alliedFaction);

        private bool TryFindFactions(out Faction alliedFaction) => Find.FactionManager.AllFactions.Where(x => !x.IsPlayer && !x.def.hidden && x.PlayerRelationKind == FactionRelationKind.Ally && !x.def.techLevel.IsNeolithicOrWorse()).TryRandomElement(out alliedFaction)
                ? true
                : false;

        private bool TryFindStravingPawns( out IEnumerable<Pawn> enumerableFood, Map target)
        {
            enumerableFood = target.mapPawns.FreeColonists.Where(pawn => pawn.Faction.IsPlayer && pawn.Starving());
            if (enumerableFood.Count() != 0)
                return true;
            return false;
        }
        private bool TryFindInjuredPawns(out IEnumerable<Pawn> enumerableInjured, Map target)
        {
            enumerableInjured = target.mapPawns.FreeColonists.Where(pawn => pawn.Faction.IsPlayer && pawn.health.HasHediffsNeedingTendByPlayer());
            if (enumerableInjured.Count() != 0)
                return true;
            return false;
        }
    }
}
