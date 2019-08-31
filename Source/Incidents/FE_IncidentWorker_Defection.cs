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
    /*class FE_IncidentWorker_Defection : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Settlement defector;
            Faction faction;
            return base.CanFireNowSub(parms) && TryFindFactions(out faction) && TryFindDefector(out defector, faction);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Faction faction;
            if (!TryFindFactions(out faction))
                return false;
            Settlement defector;
            if (!TryFindDefector(out defector, faction))
               return false;
            if (defector==null)
            {
                Log.Error(defector.Label+", faction is "+faction.Name);
            }
            Pawn leader = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Villager, faction, PawnGenerationContext.NonPlayer, -1, false, false, false, false, false));
            string text = "DefectionRequest".Translate(faction, defector, leader.LabelShort);
            DiaNode nodeRoot = new DiaNode(text);
            nodeRoot.options.Add(new DiaOption("DefectionRequest_Accept".Translate())
            {

                action = (Action)(() =>
                {
                    Settlement join = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    join.SetFaction(FlavorExpansion.factionManager.SisterFaction());
                    join.Name = defector.Name;
                    join.Tile = defector.Tile;
                    defector.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -40, false);
                    Find.WorldObjects.Remove(defector);
                    
                    Find.WorldObjects.Add(join);
                }),
                link = new DiaNode("DefectionRequestAccept".Translate(leader.LabelShort, faction, defector))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            nodeRoot.options.Add(new DiaOption("DefectionRequest_Reject".Translate())
            {
                action = (Action)(() =>
                {
                    Faction hostileFac = Find.FactionManager.RandomEnemyFaction();
                    defector.SetFaction(hostileFac);
                    Find.LetterStack.ReceiveLetter("LetterLabelDefectionReject".Translate(), "DefectionRequestReject_result".Translate(defector, faction, hostileFac)
                    , LetterDefOf.NegativeEvent, null, faction, (string)null);
                }),
                link = new DiaNode("DefectionRequestReject".Translate(leader.LabelShort))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            string title = "DefectionRequestTitle".Translate(defector);
            Find.WindowStack.Add((Window)new Dialog_NodeTreeWithFactionInfo(nodeRoot, defector.Faction, true, true, title));
            Find.Archive.Add((IArchivable)new ArchivedDialog(nodeRoot.text, title, defector.Faction));
            return true;
        }
        private bool TryFindDefector(out Settlement defector, Faction ally)
        {
            if ((from x in Find.WorldObjects.Settlements
                 where x.Faction == ally && Utilities.Reachable(Find.AnyPlayerHomeMap.Tile,x.Tile,75)
                 select x).TryRandomElement(out defector))
            {

                return true;
            }
            defector = null;
            return false;
        }
        private bool TryFindFactions(out Faction alliedFaction)
        {
            if ((from x in Find.FactionManager.AllFactions
                 where !x.IsPlayer && !x.def.hidden && x.GoodwillWith(Faction.OfPlayer) > 0 && x.def != EndGameDefOf.Sister_Faction
                 select x).TryRandomElement(out alliedFaction))
            {

                return true;
            }
            alliedFaction = null;
            return false;
        }
    }*/
}
