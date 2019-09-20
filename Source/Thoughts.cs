using System.Linq;
using RimWorld;
using Verse;

namespace Flavor_Expansion
{
    class ThoughtWorker_Security : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.Faction != Faction.OfPlayer)
                return ThoughtState.Inactive;
            byte count = 0;
            foreach (Faction f in Find.FactionManager.AllFactionsVisible.Where(x=> !x.defeated && !x.IsPlayer && x.PlayerRelationKind == FactionRelationKind.Ally))
            {
               count++;
            }
            if (count == 0)
                return ThoughtState.Inactive;
            if (count >= 6)
                return ThoughtState.ActiveAtStage(6);
            return ThoughtState.ActiveAtStage(count);
        }
    }
}
