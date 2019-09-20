using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;


namespace Flavor_Expansion
{
    class FE_IncidentWorker_War : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && Utilities.FactionsWar().GetWars().Count <=3 && PotentialWars();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (Utilities.FactionsWar().GetWars().Count > 3 || !PotentialWars())
                return false;

            Utilities.FactionsWar().TryDeclareWar();
            return true;
        }

        private static bool PotentialWars()
        {
            List<LE_FactionInfo> info = Utilities.FactionsWar().factionInfo;
            List<War> wars = Utilities.FactionsWar().GetWars();

            foreach(LE_FactionInfo factionInfo in info)
            {
                if (info.Any(x=> x.faction!= factionInfo.faction && x.faction.HostileTo(factionInfo.faction) && !wars.Any(war => war.Equal(factionInfo.faction, x.faction))))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
