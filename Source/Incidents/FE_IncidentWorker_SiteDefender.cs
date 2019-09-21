using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class FE_IncidentWorker_SiteDefender : IncidentWorker
    {

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && TryFindFactions(out Faction sister, out Faction enemyFaction) && TryFindTile(out int tile) && EndGame_Settings.SiteDefender;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryFindFactions(out Faction ally, out Faction enemyFaction) || !TryFindTile(out int tile))
                return false;
            Site site = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, EndGameDefOf.Outpost_defense, tile, ally, true);

            List<Thing> rewards = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
            {
                totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow() - 500))
            });

            int randomInRange = SiteTuning.QuestSiteTimeoutDaysRange.RandomInRange * Global.DayInTicks;
            site.GetComponent<WorldComp_SiteDefense>().StartComp(enemyFaction, rewards);
            site.GetComponent<TimeoutComp>().StartTimeout(randomInRange);
            
            Find.WorldObjects.Add(site);
            string text = def.letterText.Formatted(ally.leader.LabelShort, ally.def.leaderTitle, ally.Name, GenLabel.ThingsLabel(rewards, string.Empty), randomInRange.ToStringTicksToPeriod(), GenThing.GetMarketValue(rewards).ToStringMoney(null)).CapitalizeFirst();
            GenThing.TryAppendSingleRewardInfo(ref text, rewards);
            Find.LetterStack.ReceiveLetter(def.letterLabel, text, def.letterDef, site, ally, null);
            return true;

        }

        private bool TryFindTile(out int tile)
        {
            if (!(from f in Find.WorldObjects.Settlements
                  where !f.Faction.IsPlayer && f.Faction.PlayerRelationKind == FactionRelationKind.Ally && Utilities.Reachable(f.Tile,Find.AnyPlayerHomeMap.Tile,120) && Find.WorldReachability.CanReach(Find.AnyPlayerHomeMap.Tile, f.Tile)
                  select f).TryRandomElement(out Settlement sis))
            {
                tile = -1;
                return false;
            }

            IntRange siteDistanceRange = SiteTuning.BanditCampQuestSiteDistanceRange;
            return TileFinder.TryFindNewSiteTile(out tile, siteDistanceRange.min, siteDistanceRange.max, false, true, sis.Tile);
        }
        private bool TryFindFactions(out Faction alliedFaction, out Faction enemyFaction)
        {
            if(!Find.FactionManager.AllFactionsVisible.Where(x=> !x.IsPlayer && x.PlayerRelationKind== FactionRelationKind.Ally).TryRandomElement(out Faction ally))
            if (ally==null || (ally!=null && !ally.defeated))
            {
                alliedFaction = null;
                enemyFaction = null;
                return false;
            }
            if ((from x in Find.FactionManager.AllFactions
                 where !x.IsPlayer && !x.defeated && !x.def.hidden && x.HostileTo(ally) && x.HostileTo(Faction.OfPlayer) && x.def.humanlikeFaction
                 select x).TryRandomElement(out enemyFaction))
            {
                alliedFaction = ally;
                return true;
            }
            
            enemyFaction = null;
            alliedFaction = null;
            return false;
        }
    }
}
