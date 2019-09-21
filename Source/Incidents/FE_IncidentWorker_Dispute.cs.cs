using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class FE_IncidentWorker_Dispute : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && FindSettlements(out Settlement set1,out Settlement set2);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!FindSettlements(out Settlement set1, out Settlement set2))
                return false;
            int tile;

            using (WorldPath path = Find.World.pathFinder.FindPath(set1.Tile, set2.Tile, null))
            {
                List<int> p = path.NodesReversed;
                tile = p[p.Count() / 2];
            }

            WorldObject_Dispute dispute = (WorldObject_Dispute)WorldObjectMaker.MakeWorldObject(EndGameDefOf.Dispute_Camp);
            dispute.GetComponent<TimeoutComp>().StartTimeout(Global.DayInTicks);
            dispute.Tile = tile;
            dispute.Set1 = set1;
            dispute.Set2 = set2;
            dispute.SetFaction(set1.Faction);
            Find.WorldObjects.Add(dispute);
            Find.LetterStack.ReceiveLetter("LetterLabelDispute".Translate(), "Dispute".Translate(set1,set2, set1.Faction)
                    , LetterDefOf.PositiveEvent, dispute, null, null);
            return true;

        }

        private bool FindSettlements(out Settlement set1, out Settlement set2)
        {
            List<Settlement> list = Find.WorldObjects.Settlements.Where(s => !s.Faction.IsPlayer && s.Faction.PlayerRelationKind == FactionRelationKind.Ally && Utilities.Reachable(s.Tile, Find.AnyPlayerHomeMap.Tile, 75)).ToList();
                                     
            if (list.NullOrEmpty())
            {
                set1 = null;
                set2 = null;
                return false;
            }
            
            foreach(Settlement s in list.InRandomOrder())
            {
                set1= list.Find(x => x != s && Utilities.Reachable(x, s, 100) && !RoadAlreadyExists(x, s));
                if (set1 != null)
                {
                    set2 = s;
                    return true;
                }
            }
            set1 = null;
            set2 = null;
            return false;

        }

        private bool RoadAlreadyExists(Settlement set1, Settlement set2)
        {
            using (WorldPath p = Find.World.pathFinder.FindPath(set1.Tile, set2.Tile, null))
            {
                List<int> path = p.NodesReversed;
                foreach (int i in path)
                {
                    if (Find.WorldGrid[i].Roads == null)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
