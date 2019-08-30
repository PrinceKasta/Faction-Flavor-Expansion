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
            TryFindFactions(out ally, out enemyFaction);
            if (ally == null || enemyFaction == null || !TryFindTile(out tile))
                return false;
            Site site = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, EndGameDefOf.Outpost_defense, tile, ally, true);
            site.sitePartsKnown = true;
            site.Tile = tile;
            List<Thing> list = new List<Thing>();
            int randomInRange = SiteTuning.QuestSiteTimeoutDaysRange.RandomInRange;
            site.GetComponent<WorldComp_SiteDefense>().StartComp(randomInRange, parms,ally);
            
            Find.WorldObjects.Add(site);
            Find.LetterStack.ReceiveLetter("LetterLabelOutpostdefense".Translate(), TranslatorFormattedStringExtensions.Translate("Outpostdefense", ally.leader)
                    , LetterDefOf.NegativeEvent, site, ally, (string)null);
            return true;

        }

        private bool TryFindTile(out int tile)
        {
            Settlement sis;
            if (!(from f in Find.WorldObjects.Settlements
                  where !f.Faction.IsPlayer && f.Faction.PlayerRelationKind == FactionRelationKind.Ally
                  select f).TryRandomElement(out sis))
            {
                tile = -1;
                return false;
            }

            IntRange siteDistanceRange = SiteTuning.DownedRefugeeQuestSiteDistanceRange;
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
                 where !x.IsPlayer && !x.defeated && x.HostileTo(ally) && x.HostileTo(Faction.OfPlayer) && x.def.humanlikeFaction
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
