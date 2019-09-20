using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System.Diagnostics;
using Verse.AI.Group;


namespace Flavor_Expansion
{

    class WorldObject_Dispute : WorldObject
    {
        private static readonly SimpleCurve BadOutcomeChanceFactorByNegotiationAbility = new SimpleCurve()
    {
      {
        new CurvePoint(0.0f, 4f),
        true
      },
      {
        new CurvePoint(1f, 1f),
        true
      },
      {
        new CurvePoint(1.5f, 0.4f),
        true
      }
    };
        private static List<Pair<Action, float>> tmpPossibleOutcomes = new List<Pair<Action, float>>();
        private Material cachedMat;
        public Settlement Set1 { get; set; }
        public Settlement Set2 { get; set; }

        public override Material Material
        {
            get
            {
                if (cachedMat == null)
                {
                    Color color = Faction == null ? Color.white : Faction.Color;
                    cachedMat = MaterialPool.MatFrom(def.texture, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.WorldObjectRenderQueue);
                }
                return cachedMat;
            }
        }

        public void Notify_CaravanArrived(Caravan caravan)
        {
            Pawn bestDiplomat = BestCaravanPawnUtility.FindBestDiplomat(caravan);
            if (bestDiplomat == null)
            {
                Messages.Message("MessagePeaceTalksNoDiplomat".Translate(), caravan, MessageTypeDefOf.NegativeEvent, false);
            }
            else
            {
                float outcomeWeightFactor = WorldObject_Dispute.GetBadOutcomeWeightFactor(bestDiplomat);
                float num = 1f / outcomeWeightFactor;
                tmpPossibleOutcomes.Clear();
                tmpPossibleOutcomes.Add(new Pair<Action, float>(() => Outcome_Disaster(caravan), 0.05f * outcomeWeightFactor));
                tmpPossibleOutcomes.Add(new Pair<Action, float>(() => Outcome_Backfire(caravan), 0.15f * outcomeWeightFactor));
                tmpPossibleOutcomes.Add(new Pair<Action, float>(() => Outcome_Fail(caravan), 0.35f));
                tmpPossibleOutcomes.Add(new Pair<Action, float>(() => Outcome_MildSuccess(caravan), 0.35f * num));
                tmpPossibleOutcomes.Add(new Pair<Action, float>(() => Outcome_Success(caravan), 0.15f * num));
                tmpPossibleOutcomes.Add(new Pair<Action, float>(() => Outcome_Triumph(caravan), 0.05f * num));
                tmpPossibleOutcomes.RandomElementByWeight(x => x.Second).First();
                bestDiplomat.skills.Learn(SkillDefOf.Social, 6000f, true);
            }
        }

        [DebuggerHidden]
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(caravan))
            {
                yield return o;
            }
            foreach (FloatMenuOption f in CaravanArrivalAction_VisitDispute.GetFloatMenuOptions(caravan, this))
            {
                yield return f;
            }
        }

        private void Outcome_Disaster(Caravan caravan)
        {
            Faction.TryAffectGoodwillWith(Faction.OfPlayer, -40);
            bool chance = Rand.Chance(0.5f);
            Settlement s = chance ? Set1 : Set2;
            Settlement turncoat = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            turncoat.SetFaction(Find.FactionManager.AllFactionsVisible.Where(f => !f.IsPlayer && f.HostileTo(Faction.OfPlayer)).RandomElementWithFallback(null));
            turncoat.Tile = s.Tile;
            turncoat.Name = s.Name;
            if (turncoat.Faction == null)
            {
                Outcome_Fail(caravan);
                return;
            }

            Settlement friendly = chance ? Set2 : Set1;
            Find.WorldObjects.Remove(s);
            Find.WorldObjects.Remove(chance ? Set2 : Set1);
            Find.WorldObjects.Add(turncoat);
            Site ambush = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, SitePartDefOf.AmbushEdge, Tile, turncoat.Faction, true, StorytellerUtility.DefaultSiteThreatPointsNow());
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeDisaster".Translate(), "DisputeDisaster".Translate(turncoat, turncoat.Faction, friendly)
                    , LetterDefOf.ThreatBig, turncoat, null, "Disaster");

            LongEventHandler.QueueLongEvent(new Action(() => 
            {
                MapGenerator.GenerateMap(new IntVec3(110,1,110), ambush, MapGeneratorDefOf.Encounter);
                List<PawnKindDef> kindDefs = new List<PawnKindDef>();
                for (int i = 0; i < 2; i++)
                {
                    kindDefs.Clear();
                    Lord lord = LordMaker.MakeNewLord(i == 0 ? turncoat.Faction : Faction, new LordJob_AssaultColony(i == 0 ? turncoat.Faction : Faction), ambush.Map);
                    kindDefs = Utilities.GeneratePawnKindDef(45, i == 0 ? turncoat.Faction : Faction);
                    // Balance
                    IntVec3 vec = CellFinder.RandomClosewalkCellNear(new IntVec3(ambush.Map.Center.x - 30 + (i * 60), ambush.Map.Center.y, ambush.Map.Center.z), ambush.Map, 10);
                    Utilities.GenerateFighter(StorytellerUtility.DefaultSiteThreatPointsNow() + 300, lord, kindDefs, ambush.Map, i == 0 ? turncoat.Faction : Faction, vec);
                }
                CaravanEnterMapUtility.Enter(caravan, ambush.Map, CaravanEnterMode.Center, CaravanDropInventoryMode.DoNotDrop, true);
            }), "GeneratingMapForNewEncounter",false,null);
            Utilities.FactionsWar().GetByFaction(turncoat.Faction).resources += FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE * 2;
            Utilities.FactionsWar().GetByFaction(friendly.Faction).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE * 2;
            Find.WorldObjects.Remove(this);
        }

        private void Outcome_Backfire(Caravan caravan)
        {
            Faction.TryAffectGoodwillWith(Faction.OfPlayer, -25);
            Settlement turncoat, friendly;
            bool chance = Rand.Chance(0.5f);
            
            Settlement s = chance ? Set1 : Set2;
            turncoat = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            turncoat.SetFaction(Find.FactionManager.AllFactionsVisible.Where(f => !f.IsPlayer && f.HostileTo(Faction.OfPlayer)).RandomElementWithFallback(null));
            turncoat.Tile = s.Tile;
            turncoat.Name = s.Name;
            if (turncoat.Faction == null)
            {
                Outcome_Fail(caravan);
                return;
            }

            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = (int)FE_IncidentWorker_Jointraid.SilverBonusRewardCurve.Evaluate(Set2.Faction.PlayerGoodwill);
            List<Thing> rewards = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
            {
                totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow()))
            });
            friendly = chance ? Set2 : Set1;
            turncoat.GetComponent<WorldComp_JointRaid>().StartComp(new IntRange(Global.DayInTicks * 15, Global.DayInTicks * 25).RandomInRange, friendly.Faction, rewards, silver);
            Find.WorldObjects.Remove(s);
            Find.WorldObjects.Add(turncoat);
            Utilities.FactionsWar().GetByFaction(turncoat.Faction).resources += FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE * 2;
            Utilities.FactionsWar().GetByFaction(friendly.Faction).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE * 2;
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeBackfire".Translate(), "DisputeBackfire".Translate(turncoat, turncoat.Faction, friendly)
                   , LetterDefOf.ThreatBig, turncoat, Faction, null);
            Find.WorldObjects.Remove(this);
        }
        private void Outcome_Fail(Caravan caravan)
        {
            Set1.Faction.TryAffectGoodwillWith(Faction.OfPlayer,-10);
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeFail".Translate(), "DisputeFail".Translate(BestCaravanPawnUtility.FindBestDiplomat(caravan), Set1, Set2)
                   , LetterDefOf.NegativeEvent, null, Faction, null);
            Find.WorldObjects.Remove(this);
        }

        private void Outcome_MildSuccess(Caravan caravan)
        {
            Set1.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 10);
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeMildSuccess".Translate(), "DisputeMildSuccess".Translate(BestCaravanPawnUtility.FindBestDiplomat(caravan), Set1, Set2)
                   , LetterDefOf.PositiveEvent, null, null, null);
            Find.WorldObjects.Remove(this);
        }
        private void Outcome_Success(Caravan caravan)
        {
            Set1.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 25);
            using ( WorldPath path = Find.WorldPathFinder.FindPath(Find.AnyPlayerHomeMap.Tile, Set1.Tile, null))
            {
                List<int> p = path.NodesReversed;

                for (int i = 0; i < (p.Count() - 1); i++)
                {
                    if (Find.WorldGrid[p[i]].potentialRoads == null)
                    {
                        continue;
                    }
                    if (Find.WorldGrid[p[i]].potentialRoads.Any(x => x.road == RoadDefOf.AncientAsphaltHighway) || Find.WorldGrid[p[i]].potentialRoads.Any(x => x.road == RoadDefOf.AncientAsphaltRoad) || Find.WorldGrid[p[i]].potentialRoads.Any(x => x.road == EndGameDefOf.StoneRoad))
                    {
                        if (Find.WorldGrid[p[i+1]].potentialRoads != null && (Find.WorldGrid[p[i+1]].potentialRoads.Any(x => x.road == RoadDefOf.AncientAsphaltHighway) || Find.WorldGrid[p[i+1]].potentialRoads.Any(x => x.road == RoadDefOf.AncientAsphaltRoad) || Find.WorldGrid[p[i+1]].potentialRoads.Any(x => x.road == EndGameDefOf.StoneRoad)))
                            p.Remove(p[i+1]);
                    }
                    if (i == (p.Count() - 1))
                        return;
                }
                if (p.Count() == 0)
                    return;

                WorldObject dispute = WorldObjectMaker.MakeWorldObject(EndGameDefOf.Roads_Camp);
                dispute.GetComponent<WorldComp_DisputeRoads>().StartComp(Set1.Tile, Set2.Tile, p);
                dispute.Tile = p.First();
                dispute.SetFaction(Faction);
                Utilities.FactionsWar().GetByFaction(Faction).resources += FE_WorldComp_FactionsWar.MEDIUM_EVENT_RESOURCE_VALUE;
                Find.WorldObjects.Add(dispute);
                Find.LetterStack.ReceiveLetter("LetterLabelDisputeSuccess".Translate(), "DisputeSuccess".Translate(BestCaravanPawnUtility.FindBestDiplomat(caravan), Set1, Set2)
                   , LetterDefOf.PositiveEvent, Set1, null, null);
                Find.WorldObjects.Remove(this);
                
            }
        }
        private void Outcome_Triumph(Caravan caravan)
        {
            Set1.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 45);
            WorldObject site = WorldObjectMaker.MakeWorldObject(EndGameDefOf.Dispute_FOB);
            site.SetFaction(Set1.Faction);
            Utilities.FactionsWar().GetByFaction(site.Faction).resources += FE_WorldComp_FactionsWar.LARGE_EVENT_Cache_RESOURCE_VALUE;
            site.Tile= Tile;
            site.GetComponent<WorldComp_DisputeFOB>().StartComp(Set1, Set2);
            Find.WorldObjects.Add(site);
            Find.WorldObjects.Remove(this);
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeTriumph".Translate(), "DisputeTriumph".Translate(BestCaravanPawnUtility.FindBestDiplomat(caravan), Set1, Set2)
                   , LetterDefOf.PositiveEvent, site, null, null);
        }

        private static float GetBadOutcomeWeightFactor(Pawn diplomat) => GetBadOutcomeWeightFactor(diplomat.GetStatValue(StatDefOf.NegotiationAbility, true));

        private static float GetBadOutcomeWeightFactor(float negotationAbility) => BadOutcomeChanceFactorByNegotiationAbility.Evaluate(negotationAbility);

        public override void ExposeData()
        {
            base.ExposeData();
            Settlement Set1=this.Set1;
            Scribe_References.Look(ref Set1, "Set1");
            this.Set1 = Set1;
            Settlement Set2 = this.Set2;
            Scribe_References.Look(ref Set2, "Set2");
            this.Set2 = Set2;
        }
    }
}
