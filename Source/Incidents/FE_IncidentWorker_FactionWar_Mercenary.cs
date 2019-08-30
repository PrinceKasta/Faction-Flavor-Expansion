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
    class FE_IncidentWorker_FactionWar_Mercenary : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            int tile;
            War war;
            return base.CanFireNowSub(parms) && TryFindWar(out war) && TryFindSuitableBattleLocation(out tile) && EndGame_Settings.FactionWar;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            int tile;
            War war;
            if (!TryFindWar(out war) || !TryFindSuitableBattleLocation(out tile) || !EndGame_Settings.FactionWar)
                return false;
            Faction askingFaction;
            if (!war.AttackerFaction().HostileTo(Faction.OfPlayer) && !war.DefenderFaction().HostileTo(Faction.OfPlayer))
                askingFaction = Utilities.FactionsWar().GetResouceAmount(war.AttackerFaction()) > Utilities.FactionsWar().GetResouceAmount(war.DefenderFaction()) ? war.DefenderFaction() : war.AttackerFaction();
            else askingFaction = war.AttackerFaction().HostileTo(Faction.OfPlayer) ? war.DefenderFaction() : war.AttackerFaction();
            bool f1Win = false;
            //Rejection option's solution to the battle
            float f1Resources = Utilities.FactionsWar().GetResouceAmount(war.AttackerFaction());
            float f2Resources = Utilities.FactionsWar().GetResouceAmount(war.DefenderFaction());
            if (Rand.Chance(0.5f + f1Resources == f2Resources ? f1Resources > f2Resources ? (0.5f - f2Resources / f1Resources / 2) : -(0.5f - f2Resources / f1Resources / 2) : 0))
                f1Win = true;
            else f1Win = false;

            string text = TranslatorFormattedStringExtensions.Translate("MercenaryBattleRequest",askingFaction.leader, war.AttackerFaction() == askingFaction ? war.DefenderFaction() : war.AttackerFaction());
            DiaNode nodeRoot = new DiaNode(text);
            nodeRoot.options.Add(new DiaOption("MercenaryBattleRequest_Accept".Translate())
            {

                action = (Action)(() =>
                {
                    Site site = SiteMaker.MakeSite(SiteCoreDefOf.PreciousLump, SitePartDefOf.Outpost, tile, askingFaction, true);
                    site.GetComponent<WorldObjectComp_MercenaryBattle>().StartComp(war,askingFaction,parms);
                    site.GetComponent<TimeoutComp>().StartTimeout(Global.DayInTicks * new IntRange(14, 22).RandomInRange);
                    site.customLabel = "Battle Location";
                    LookTargetsUtility.TryHighlight(site);
                    Find.WorldObjects.Add(site);
                }),
                link = new DiaNode(TranslatorFormattedStringExtensions.Translate("MercenaryBattleRequestAccept",askingFaction.leader))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            nodeRoot.options.Add(new DiaOption("MercenaryBattleRequest_Reject".Translate())
            {
                
                action = (Action)(() =>
                {
                    
                    if(f1Win)
                    {
                        Utilities.FactionsWar().GetResouceAmount(war.DefenderFaction(), -Math.Max(Utilities.FactionsWar().GetResouceAmount(war.DefenderFaction()) / 2, 1000));
                        f1Win = true;
                    }
                    else
                    {
                        Utilities.FactionsWar().GetResouceAmount(war.AttackerFaction(), -Math.Max(Utilities.FactionsWar().GetResouceAmount(war.AttackerFaction()) / 2, 1000));
                        f1Win = false;
                    }
                    if((f1Win && war.DefenderFaction() == askingFaction)|| (!f1Win && war.AttackerFaction()==askingFaction))
                        askingFaction.TryAffectGoodwillWith(Faction.OfPlayer, -15);
                    
                }),
                link = new DiaNode("MercenaryBattleRequestReject".Translate(f1Win ? war.AttackerFaction() : war.DefenderFaction(), f1Win ? war.DefenderFaction() : war.AttackerFaction()))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            string title = "LetterLabelMercenaryBattleRequestTitle".Translate();
            Find.WindowStack.Add((Window)new Dialog_NodeTreeWithFactionInfo(nodeRoot, askingFaction, true, true, title));
            Find.Archive.Add((IArchivable)new ArchivedDialog(nodeRoot.text, title, askingFaction));

            return true;
        }
        private bool TryFindWar(out War war)
        {
            if(Utilities.FactionsWar().GetWars().Where(w=> !w.AttackerFaction().HostileTo(Faction.OfPlayer) || !w.DefenderFaction().HostileTo(Faction.OfPlayer)).TryRandomElement(out war))
                return true;
            return false;
        }
        private bool TryFindSuitableBattleLocation(out int tile)
        {
            foreach (Settlement set in Find.WorldObjects.Settlements)
            {
                if (Utilities.Reachable(Find.AnyPlayerHomeMap.Tile, set.Tile,75))
                {
                    if (!TileFinder.TryFindPassableTileWithTraversalDistance(set.Tile, 1, 8, out tile, x => !Find.WorldObjects.AnyWorldObjectAt(x)))
                        return false;
                    return true;
                }
            }
            tile = -1;
            return false;
        }
    }
}
