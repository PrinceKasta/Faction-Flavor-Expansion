using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;
using RimWorld;

namespace Flavor_Expansion
{
    class CaravanArrivalAction_VisitDispute : CaravanArrivalAction
    {
        private WorldObject_Dispute dispute;

        public CaravanArrivalAction_VisitDispute()
        {
        }

        public CaravanArrivalAction_VisitDispute(WorldObject_Dispute dispute) => this.dispute = dispute;

        public override string Label => "VisitDispute".Translate(dispute.Label);

        public override string ReportString => "CaravanVisiting".Translate(dispute.Label);

        public override void Arrived(Caravan caravan) => dispute.Notify_CaravanArrived(caravan);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref dispute, "dispute", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, WorldObject_Dispute dispute) => dispute != null && dispute.Spawned;

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, int destinationTile)
        {
            if (!(bool)base.StillValid(caravan, destinationTile))
                return base.StillValid(caravan, destinationTile);
            if (dispute != null && dispute.Tile != destinationTile)
                return false;
            return CanVisit(caravan, dispute);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldObject_Dispute dispute) => CaravanArrivalActionUtility.GetFloatMenuOptions(() => CanVisit(caravan, dispute), () => new CaravanArrivalAction_VisitDispute(dispute), "VisitDispute".Translate(dispute.Label), caravan, dispute.Tile, dispute);
    }

    //----------------------------------------------------------------------------------------------------------

    class CaravanArrivalAction_Defend : CaravanArrivalAction
    {
        private Settlement friendly;

        public CaravanArrivalAction_Defend()
        {
        }

        public CaravanArrivalAction_Defend(Settlement friendly) => this.friendly = friendly;

        public override string Label => ("DefendArrival".Translate(friendly.Label));

        public override string ReportString => "CaravanVisiting".Translate(friendly.Label);

        public override void Arrived(Caravan caravan)
        {
            if (!Find.WorldObjects.Settlements.Where(s=> s.Tile == caravan.Tile).TryRandomElement(out Settlement ally))
            {
                Log.Error("Ally null");
                return;
            }
            MapGenerator.GenerateMap(Find.AnyPlayerHomeMap.Size, ally, MapGeneratorDefOf.Base_Faction);
            CaravanEnterMapUtility.Enter(caravan, ally.Map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, true);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref friendly, "dispute", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, Settlement friendly) => friendly != null && friendly.Spawned;

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, int destinationTile)
        {
            FloatMenuAcceptanceReport acceptanceReport = base.StillValid(caravan, destinationTile);
            if (!(bool)acceptanceReport)
                return acceptanceReport;
            if (friendly != null && friendly.Tile != destinationTile)
                return false;
            return CanVisit(caravan, friendly);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, Settlement friendly) => CaravanArrivalActionUtility.GetFloatMenuOptions(() => CanVisit(caravan, friendly), () => new CaravanArrivalAction_Defend(friendly), "DefendArrival".Translate(friendly), caravan, friendly.Tile, friendly);
    }

    class CaravanArrivalAction_RoadCamp : CaravanArrivalAction
    {
        private WorldObject_RoadsCamp camp;

        public CaravanArrivalAction_RoadCamp()
        {

        }
        public CaravanArrivalAction_RoadCamp(WorldObject_RoadsCamp camp) => this.camp = camp;

        public override string Label => "RoadsCampRequestArrive".Translate(camp.Label);

        public override string ReportString => "CaravanVisiting".Translate(camp.Label);

        public override void Arrived(Caravan caravan) => camp.Notify_CaravanArrived(caravan);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref camp, "camp", false);
        }

        public static FloatMenuAcceptanceReport CanVisit( Caravan caravan, WorldObject_RoadsCamp camp) => camp != null && camp.Spawned;

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldObject_RoadsCamp camp) => CaravanArrivalActionUtility.GetFloatMenuOptions(() => CanVisit(caravan, camp), () => new CaravanArrivalAction_RoadCamp(camp), "RoadsCampRequestArrive".Translate(camp), caravan, camp.Tile, camp);
    }
}
