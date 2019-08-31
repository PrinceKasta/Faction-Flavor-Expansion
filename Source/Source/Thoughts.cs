using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Flavor_Expansion
{
    class ThoughtWorker_Security : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.Faction != Faction.OfPlayer)
                return ThoughtState.Inactive;
            int count = 0;
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
