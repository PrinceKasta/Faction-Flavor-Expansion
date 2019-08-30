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
    class FE_IncidentWorker_SettlementDefender : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Faction enemyFaction, ally;
            Settlement tile;
            return base.CanFireNowSub(parms) && TryFindFactions(out ally, out enemyFaction) && TryFindTile(ally, out tile) && EndGame_Settings.SettlementDefense;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Faction enemyFaction, ally;
            Settlement sis;
            if (!TryFindFactions(out ally, out enemyFaction) || !TryFindTile(ally, out sis) )
                return false;

            int random = new IntRange(Global.DayInTicks * 5, Global.DayInTicks * 7).RandomInRange;
            List<Thing> rewards = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
            {
                totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow() + 500f))
            });
            sis.GetComponent<WorldComp_SettlementDefender>().StartComp(enemyFaction,ally, random , rewards);
            
            string text = this.def.letterText.Formatted((NamedArgument)ally.leader.LabelShort, (NamedArgument)ally.def.leaderTitle, (NamedArgument)ally.Name, (NamedArgument)GenLabel.ThingsLabel(rewards, string.Empty), (NamedArgument)(random / Global.DayInTicks).ToString(), (NamedArgument)GenThing.GetMarketValue((IList<Thing>)rewards).ToStringMoney((string)null)).CapitalizeFirst();
            GenThing.TryAppendSingleRewardInfo(ref text, (IList<Thing>)rewards);
            Find.LetterStack.ReceiveLetter(this.def.letterLabel, text, this.def.letterDef, (LookTargets)((WorldObject)sis), ally, (string)null);
            return true;

        }
        private bool TryFindTile(Faction ally, out Settlement sis)
        {
            if ((from s in Find.WorldObjects.Settlements
                 where s.Faction == ally && Utilities.Reachable(Find.AnyPlayerHomeMap.Tile, s.Tile, 25) && !s.GetComponent<WorldComp_SettlementDefender>().IsActive()
                 select s).TryRandomElement(out sis))
                return true;

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
