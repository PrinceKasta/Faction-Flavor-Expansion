using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class FE_GrammarUtility
    {
        public static string WarEvent(Faction raider = null, Faction victim = null, Settlement town = null, bool fail = false)
        {
            GrammarRequest request = new GrammarRequest();
            if (victim == null && town == null)
            {
                request.Includes.Add(EndGameDefOf.FE_WarEvent_ArtifactCache);
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", raider));
                return GrammarResolver.Resolve("FE_ArtifactCache", request, null, false, null);
            }
            if(town == null)
            {
                request.Includes.Add(EndGameDefOf.FE_WarEvent_Raid);
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", raider));
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION2", victim));
                return GrammarResolver.Resolve("FE_Sabotage", request, null, false, null);
            }
            if (fail)
            {
                request.Includes.Add(EndGameDefOf.FE_WarEvent_Raid);
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", raider));
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION2", victim));
                request.Rules.AddRange(RulesForTown("AffectedTown", town));
                return GrammarResolver.Resolve("FE_RaidFail", request, null, false, null);
            }
            request.Includes.Add(EndGameDefOf.FE_WarEvent_Raid);
            request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", raider));
            request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION2", victim));
            request.Rules.AddRange(RulesForTown("AffectedTown", town));
            return GrammarResolver.Resolve("FE_RaidSuccess", request, null, false, null);

        }

        public static string History(Faction subject = null, Faction victim = null, Settlement town = null, Pawn pawn = null, string towndestroyed = null)
        {
            GrammarRequest request = new GrammarRequest();
            request.Includes.Add(EndGameDefOf.FE_History);
            if (pawn !=null)
            {
                request.Rules.AddRange(GrammarUtility.RulesForPawn("Pawn", pawn));
                return GrammarResolver.Resolve("r_history_pawn", request, null, false, null);
            }
            if(town!= null)
            {
                request.Rules.AddRange(RulesForTown("AffectedTown", town));
                return GrammarResolver.Resolve("r_history_town", request, null, false, null);
            }
            if(subject!=null && victim != null)
            {
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", subject));
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION2", victim));
                return GrammarResolver.Resolve("r_history_faction", request, null, false, null);
            }
            if(subject!=null)
            {
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", subject));
                request.Rules.AddRange(RulesForString( "FACTION2", NameGenerator.GenerateName(subject.def.factionNameMaker, Find.FactionManager.AllFactionsVisible.Select(fac => fac.Name))));
                return GrammarResolver.Resolve("r_history_factiondead", request, null, false, null);
            }
            if(towndestroyed !=null)
            {
                request.Rules.AddRange(RulesForString("AffectedTown", towndestroyed));
                return GrammarResolver.Resolve("r_history_townDestroyed", request, null, false, null);
            }
            return GrammarResolver.Resolve("r_history_generic", request, null, false, null);
        }

        public static IEnumerable<Rule> RulesForTown(string prefix, Settlement town)
        {
            yield return new Rule_String(prefix + "_name", town.Name);
            
        }

        public static IEnumerable<Rule> RulesForString(string prefix, string text)
        {
            yield return new Rule_String(prefix + "_name", text);

        }
    }
}
