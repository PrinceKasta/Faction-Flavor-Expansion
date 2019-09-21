using System;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class FE_IncidentWorker_FactionWar_Mercenary : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && TryFindWar(out War war) && TryFindSuitableBattleLocation(out int tile, war) && EndGame_Settings.FactionWar;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryFindWar(out War war) || !TryFindSuitableBattleLocation(out int tile, war) || !EndGame_Settings.FactionWar)
            {
                return false;
            }

            Faction askingFaction = !war.AttackerFaction().HostileTo(Faction.OfPlayer) && !war.DefenderFaction().HostileTo(Faction.OfPlayer)
                ? Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources > Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources ? war.DefenderFaction() : war.AttackerFaction()
                : war.AttackerFaction().HostileTo(Faction.OfPlayer) ? war.DefenderFaction() : war.AttackerFaction();

            bool f1Win = false;
            //Rejection option's solution to the battle
            float f1Resources = Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources;
            float f2Resources = Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources;
            if (Rand.Chance(0.5f + f1Resources == f2Resources ? f1Resources > f2Resources ? (0.5f - (f2Resources / f1Resources / 2)) : -(0.5f - (f2Resources / f1Resources / 2)) : 0))
            {
                f1Win = true;
            }
            else
            {
                f1Win = false;
            }

            if (askingFaction.leader == null)
            {
                askingFaction.GenerateNewLeader();
            }

            DiaNode nodeRoot = new DiaNode(TranslatorFormattedStringExtensions.Translate("MercenaryBattleRequest", askingFaction.leader, war.AttackerFaction() == askingFaction ? war.DefenderFaction() : war.AttackerFaction(), askingFaction.def.leaderTitle));
            nodeRoot.options.Add(new DiaOption("MercenaryBattleRequest_Accept".Translate())
            {
                action = () =>
                {
                    Site site = SiteMaker.MakeSite(EndGameDefOf.BattleLocation, SitePartDefOf.Outpost, tile, askingFaction, true);
                    site.GetComponent<WorldObjectComp_MercenaryBattle>().StartComp(war, askingFaction, parms);
                    site.GetComponent<TimeoutComp>().StartTimeout(Global.DayInTicks * new IntRange(14, 22).RandomInRange);
                    site.customLabel = "battle location";
                    Find.WorldObjects.Add(site);
                },
                link = new DiaNode(TranslatorFormattedStringExtensions.Translate("MercenaryBattleRequestAccept", askingFaction.leader))
                {
                    options = {

                         new DiaOption("OK".Translate()) { resolveTree = true },
                         new DiaOption("JumpToLocation".Translate()) { action= ()=> CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(Find.WorldObjects.WorldObjectAt(tile,WorldObjectDefOf.Site))), resolveTree=true }
                       }
                }
            });
            nodeRoot.options.Add(new DiaOption("MercenaryBattleRequest_Reject".Translate())
            {
                action = () =>
                {
                    if (f1Win)
                    {
                        Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources -= Math.Max(Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources / 2, 1000);
                        f1Win = true;
                    }
                    else
                    {
                        Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources -= Math.Max(Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources / 2, 1000);
                        f1Win = false;
                    }
                    if ((f1Win && war.DefenderFaction() == askingFaction) || (!f1Win && war.AttackerFaction() == askingFaction))
                        askingFaction.TryAffectGoodwillWith(Faction.OfPlayer, -15);
                },
                link = new DiaNode("MercenaryBattleRequestReject".Translate(f1Win ? war.AttackerFaction() : war.DefenderFaction(), f1Win ? war.DefenderFaction() : war.AttackerFaction()))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            string title = "LetterLabelMercenaryBattleRequestTitle".Translate();
            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(nodeRoot, askingFaction, true, true, title));
            Find.Archive.Add(new ArchivedDialog(nodeRoot.text, title, askingFaction));

            return true;
        }
        private bool TryFindWar(out War war) => Utilities.FactionsWar().GetWars().Where(w => !w.AttackerFaction().HostileTo(Faction.OfPlayer) || !w.DefenderFaction().HostileTo(Faction.OfPlayer)).TryRandomElement(out war) ? true : false;

        private bool TryFindSuitableBattleLocation(out int tile, War war)
        {
            foreach (Settlement set in Find.WorldObjects.Settlements.Where(s=> s.Faction == war.AttackerFaction() || s.Faction == war.DefenderFaction()))
            {
                if (Utilities.Reachable(Find.AnyPlayerHomeMap.Tile, set.Tile,75))
                {
                    return !TileFinder.TryFindPassableTileWithTraversalDistance(set.Tile, 3, 20, out tile, x => !Find.WorldObjects.AnyWorldObjectAt(x)) ? false : true;
                }
            }
            tile = -1;
            return false;
        }
    }
}
