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
        private static readonly FloatRange valueRange = new FloatRange(300f, 700f);
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && this.TryFindFactions(out Faction faction);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!this.TryFindFactions(out Faction faction))
                return false;

            Map target = (Map)parms.target;
            List<Thing> thingList = GenerateRewards(faction);
            IntVec3 intVec3 = DropCellFinder.TradeDropSpot(target);
            DropPodUtility.DropThingsNear(intVec3, target, (IEnumerable<Thing>)thingList, 110, false, true, true);
            faction.TryAffectGoodwillWith(Faction.OfPlayer, 10, false, true);
            string text = this.def.letterText.Formatted(faction.leader, faction.def.leaderTitle, faction, GenThing.GetMarketValue((IList<Thing>)thingList).ToStringMoney((string)null), GenLabel.ThingsLabel(thingList, string.Empty));
            Find.LetterStack.ReceiveLetter(this.def.letterLabel.Formatted(faction), text , LetterDefOf.PositiveEvent, (LookTargets)new TargetInfo(intVec3, target, false), faction, (string)null);

            return true;
        }
        private List<Thing> GenerateRewards(Faction alliedFaction)
        {
            if (Utilities.FactionsWar().GetByFaction(alliedFaction) == null)
            {
                return new List<Thing>();
            }
            int totalMarketValue = (int)Mathf.Clamp(valueRange.RandomInRange * (1f + 0.01f * -Utilities.FactionsWar().GetByFaction(alliedFaction).disposition), 200, 2000);
            List<Thing> list = new List<Thing>();
            Gift_RewardGeneratorBasedTMagic itc_ia = new Gift_RewardGeneratorBasedTMagic();
            return itc_ia.Generate(totalMarketValue, list);
        }
        private bool TryFindFactions(out Faction alliedFaction)
        {
            if ((from x in Find.FactionManager.AllFactions
                 where !x.IsPlayer && !x.def.hidden && x.PlayerGoodwill>=0 && !x.defeated
                 select x).TryRandomElement(out alliedFaction))
            {
                return true;
            }
            alliedFaction = null;
            return false;
        }
    }
}
