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
    class FE_IncidentWorker_SiteDefender : IncidentWorker
    {

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Faction sister, enemyFaction;
            int tile;
            return base.CanFireNowSub(parms) && TryFindFactions(out sister, out enemyFaction) && TryFindTile(out tile) && EndGame_Settings.SiteDefender;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Faction ally, enemyFaction;
            int tile;
            
            if (!TryFindFactions(out ally, out enemyFaction) || !TryFindTile(out tile))
                return false;
            Site site = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, EndGameDefOf.Outpost_defense, tile, ally, true);

            List<Thing> rewards = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
            {
                totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow() - 500))
            });

            int randomInRange = SiteTuning.QuestSiteTimeoutDaysRange.RandomInRange;
            site.GetComponent<WorldComp_SiteDefense>().StartComp(randomInRange * Global.DayInTicks, parms, enemyFaction, rewards);
            
            Find.WorldObjects.Add(site);
            string text = this.def.letterText.Formatted((NamedArgument)ally.leader.LabelShort, (NamedArgument)ally.def.leaderTitle, (NamedArgument)ally.Name, (NamedArgument)GenLabel.ThingsLabel(rewards, string.Empty), (NamedArgument)(randomInRange / Global.DayInTicks).ToString(), (NamedArgument)GenThing.GetMarketValue((IList<Thing>)rewards).ToStringMoney((string)null)).CapitalizeFirst();
            GenThing.TryAppendSingleRewardInfo(ref text, (IList<Thing>)rewards);
            Find.LetterStack.ReceiveLetter(this.def.letterLabel, text, this.def.letterDef, (LookTargets)((WorldObject)site), ally, (string)null);
            return true;

        }

        private bool TryFindTile(out int tile)
        {
            Settlement sis;
            if (!(from f in Find.WorldObjects.Settlements
                  where !f.Faction.IsPlayer && f.Faction.PlayerRelationKind == FactionRelationKind.Ally && Utilities.Reachable(f.Tile,Find.AnyPlayerHomeMap.Tile,120) && Find.WorldReachability.CanReach(Find.AnyPlayerHomeMap.Tile, f.Tile)
                  select f).TryRandomElement(out sis))
            {
                tile = -1;
                return false;
            }

            IntRange siteDistanceRange = SiteTuning.BanditCampQuestSiteDistanceRange;
            return TileFinder.TryFindNewSiteTile(out tile, siteDistanceRange.min, siteDistanceRange.max, false, true, sis.Tile);
        }
        private bool TryFindFactions(out Faction alliedFaction, out Faction enemyFaction)
        {
            Faction ally;
            if(!Find.FactionManager.AllFactionsVisible.Where(x=> !x.IsPlayer && x.PlayerRelationKind== FactionRelationKind.Ally).TryRandomElement(out ally))
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
