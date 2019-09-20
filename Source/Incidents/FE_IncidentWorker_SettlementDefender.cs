using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class FE_IncidentWorker_SettlementDefender : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && TryFindFactions(out Faction ally, out Faction enemyFaction) && TryFindTile(ally, out Settlement tile) && EndGame_Settings.SettlementDefense;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryFindFactions(out Faction ally, out Faction enemyFaction) || !TryFindTile(ally, out Settlement sis) )
                return false;

            int random = new IntRange(Global.DayInTicks * 5, Global.DayInTicks * 7).RandomInRange;
            List<Thing> rewards = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
            {
                totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow() + 500f))
            });
            sis.GetComponent<WorldComp_SettlementDefender>().StartComp(enemyFaction,ally, random , rewards);
            
            string text = def.letterText.Formatted(ally.leader.LabelShort, ally.def.leaderTitle, ally.Name, GenLabel.ThingsLabel(rewards, string.Empty), (random / Global.DayInTicks).ToString(), GenThing.GetMarketValue(rewards).ToStringMoney(null)).CapitalizeFirst();
            GenThing.TryAppendSingleRewardInfo(ref text, rewards);
            Find.LetterStack.ReceiveLetter(def.letterLabel, text, def.letterDef, sis, ally, null);
            return true;

        }
        private bool TryFindTile(Faction ally, out Settlement sis)
        {
            if ((from s in Find.WorldObjects.Settlements
                 where s.Faction == ally && Utilities.Reachable(Find.AnyPlayerHomeMap.Tile, s.Tile, 25) && !s.GetComponent<WorldComp_SettlementDefender>().IsActive
                 select s).TryRandomElement(out sis))
            {
                return true;
            }

            sis = null;
            return false;
        }
        private bool TryFindFactions(out Faction alliedFaction,  out Faction enemyFaction)
        {
            Faction ally;
            if (!Find.FactionManager.AllFactionsVisible.Where(x => !x.IsPlayer && x.PlayerRelationKind == FactionRelationKind.Ally).TryRandomElement(out alliedFaction))
            {
                enemyFaction = null;
                alliedFaction = null;
                return false;
            }
            else
            {
                ally = alliedFaction;
            }
            if(alliedFaction!=null)
            {
                if ((from x in Find.FactionManager.AllFactions
                     where !x.IsPlayer && !x.defeated && x.HostileTo(ally) && x.HostileTo(Faction.OfPlayer) && !x.def.hidden && x.def.humanlikeFaction
                     select x).TryRandomElement(out enemyFaction))
                {
                    return true;
                }
            }
            enemyFaction = null;
            alliedFaction = null;
            return false;
        }
    }
}
