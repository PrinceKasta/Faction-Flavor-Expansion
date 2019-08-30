using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public CaravanArrivalAction_VisitDispute(WorldObject_Dispute dispute)
        {
            this.dispute = dispute;
        }

        public override string Label
        {
            get
            {
                return "VisitDispute".Translate((NamedArgument)this.dispute.Label);
            }
        }

        public override string ReportString
        {
            get
            {
                return "CaravanVisiting".Translate((NamedArgument)this.dispute.Label);
            }
        }
        public override void Arrived(Caravan caravan)
        {
            this.dispute.Notify_CaravanArrived(caravan);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject_Dispute>(ref this.dispute, "dispute", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(
        Caravan caravan,
        WorldObject_Dispute dispute)
        {
            return (FloatMenuAcceptanceReport)(dispute != null && dispute.Spawned);
        }

        public override FloatMenuAcceptanceReport StillValid(
        Caravan caravan,
        int destinationTile)
        {
            FloatMenuAcceptanceReport acceptanceReport = base.StillValid(caravan, destinationTile);
            if (!(bool)acceptanceReport)
                return acceptanceReport;
            if (this.dispute != null && this.dispute.Tile != destinationTile)
                return (FloatMenuAcceptanceReport)false;
            return CaravanArrivalAction_VisitDispute.CanVisit(caravan, this.dispute);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(
        Caravan caravan,
        WorldObject_Dispute dispute)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitDispute>((Func<FloatMenuAcceptanceReport>)(() => CaravanArrivalAction_VisitDispute.CanVisit(caravan, dispute)), (Func<CaravanArrivalAction_VisitDispute>)(() => new CaravanArrivalAction_VisitDispute(dispute)), "VisitDispute".Translate((NamedArgument)dispute.Label), caravan, dispute.Tile, (WorldObject)dispute);
        }
    }


    //----------------------------------------------------------------------------------------------------------


    class CaravanArrivalAction_Defend : CaravanArrivalAction
    {
        private Settlement friendly;

        public CaravanArrivalAction_Defend()
        {
        }

        public CaravanArrivalAction_Defend(Settlement friendly)
        {
            this.friendly = friendly;
        }

        public override string Label
        {
            get
            {
                return ("DefendArrival".Translate((NamedArgument)this.friendly.Label));
            }
        }

        public override string ReportString
        {
            get
            {
                return "CaravanVisiting".Translate((NamedArgument)this.friendly.Label);
            }
        }
        public override void Arrived(Caravan caravan)
        {
            Settlement ally;
            if (!(from s in Find.WorldObjects.Settlements
                  where s.Tile == caravan.Tile
                  select s).TryRandomElement(out ally))
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
            Scribe_References.Look<Settlement>(ref this.friendly, "dispute", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(
        Caravan caravan,
        Settlement friendly)
        {
            return (FloatMenuAcceptanceReport)(friendly != null && friendly.Spawned);
        }

        public override FloatMenuAcceptanceReport StillValid(
        Caravan caravan,
        int destinationTile)
        {
            FloatMenuAcceptanceReport acceptanceReport = base.StillValid(caravan, destinationTile);
            if (!(bool)acceptanceReport)
                return acceptanceReport;
            if (this.friendly != null && this.friendly.Tile != destinationTile)
                return (FloatMenuAcceptanceReport)false;
            return CaravanArrivalAction_Defend.CanVisit(caravan, this.friendly);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(
        Caravan caravan,
        Settlement friendly)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions((Func<FloatMenuAcceptanceReport>)(() => CaravanArrivalAction_Defend.CanVisit(caravan, friendly)), (Func<CaravanArrivalAction_Defend>)(() => new CaravanArrivalAction_Defend(friendly)),"DefendArrival".Translate((NamedArgument)friendly), caravan, friendly.Tile, friendly);
        }
    }

    class CaravanArrivalAction_RoadCamp : CaravanArrivalAction
    {
        private WorldObject_RoadsCamp camp;
        public CaravanArrivalAction_RoadCamp()
        {

        }
        public CaravanArrivalAction_RoadCamp(WorldObject_RoadsCamp camp)
        {
            this.camp = camp;
        }
        public override string Label
        {
            get
            {
                return ("RoadsCampRequestArrive".Translate((NamedArgument)this.camp.Label));
            }
        }

        public override string ReportString
        {
            get
            {
                return "CaravanVisiting".Translate((NamedArgument)this.camp.Label);
            }
        }
        public override void Arrived(Caravan caravan)
        {
            this.camp.Notify_CaravanArrived(caravan);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject_RoadsCamp>(ref this.camp, "camp", false);
        }

        public static FloatMenuAcceptanceReport CanVisit(
        Caravan caravan,
        WorldObject_RoadsCamp camp)
        {
            return (FloatMenuAcceptanceReport)(camp != null && camp.Spawned);
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(
        Caravan caravan,
        WorldObject_RoadsCamp camp)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions((Func<FloatMenuAcceptanceReport>)(() => CaravanArrivalAction_RoadCamp.CanVisit(caravan, camp)), (Func<CaravanArrivalAction_RoadCamp>)(() => new CaravanArrivalAction_RoadCamp(camp)), "RoadsCampRequestArrive".Translate((NamedArgument)camp), caravan, camp.Tile, camp);
        }
    }
}
