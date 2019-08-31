using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using System.Reflection;
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
        private const float BaseWeight_Disaster = -1.00f;
        private const float BaseWeight_Backfire = -1.0f;
        private const float BaseWeight_TalksFlounder = -1.0f;
        private const float BaseWeight_Success = 1.0f;
        private const float BaseWeight_Triumph = -1.0f;
        public Settlement set1 { get; set; }
        public Settlement set2 { get; set; }

        public override Material Material
        {
            get
            {
                if ((UnityEngine.Object)this.cachedMat == (UnityEngine.Object)null)
                {
                    Color color = this.Faction == null ? Color.white : this.Faction.Color;
                    this.cachedMat = MaterialPool.MatFrom(this.def.texture, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.WorldObjectRenderQueue);
                }
                return this.cachedMat;
            }
        }

        public void Notify_CaravanArrived(Caravan caravan)
        {
            Pawn bestDiplomat = BestCaravanPawnUtility.FindBestDiplomat(caravan);
            if (bestDiplomat == null)
            {
                Messages.Message("MessagePeaceTalksNoDiplomat".Translate(), (LookTargets)((WorldObject)caravan), MessageTypeDefOf.NegativeEvent, false);
            }
            else
            {
                float outcomeWeightFactor = WorldObject_Dispute.GetBadOutcomeWeightFactor(bestDiplomat);
                float num = 1f / outcomeWeightFactor;
                WorldObject_Dispute.tmpPossibleOutcomes.Clear();
                WorldObject_Dispute.tmpPossibleOutcomes.Add(new Pair<Action, float>((Action)(() => this.Outcome_Disaster(caravan)), 0.05f * outcomeWeightFactor));
                WorldObject_Dispute.tmpPossibleOutcomes.Add(new Pair<Action, float>((Action)(() => this.Outcome_Backfire(caravan)), 0.15f * outcomeWeightFactor));
                WorldObject_Dispute.tmpPossibleOutcomes.Add(new Pair<Action, float>((Action)(() => this.Outcome_Fail(caravan)), 0.35f));
                WorldObject_Dispute.tmpPossibleOutcomes.Add(new Pair<Action, float>((Action)(() => this.Outcome_MildSuccess(caravan)), 0.35f * num));
                WorldObject_Dispute.tmpPossibleOutcomes.Add(new Pair<Action, float>((Action)(() => this.Outcome_Success(caravan)), 0.15f * num));
                WorldObject_Dispute.tmpPossibleOutcomes.Add(new Pair<Action, float>((Action)(() => this.Outcome_Triumph(caravan)), 0.05f * num));
                WorldObject_Dispute.tmpPossibleOutcomes.RandomElementByWeight<Pair<Action, float>>((Func<Pair<Action, float>, float>)(x => x.Second)).First();
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
            Settlement turncoat, friendly;
            Site ambush;
            this.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -40);

            if (Rand.Chance(0.5f))
            {
                
                turncoat = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                turncoat.SetFaction((from f in Find.FactionManager.AllFactions
                                     where f.HostileTo(Faction.OfPlayer) && !f.def.hidden
                                     select f).RandomElementWithFallback(null));
                turncoat.Tile = set2.Tile;
                turncoat.Name = set2.Name;
                if (turncoat.Faction == null)
                {
                    Log.Warning("Disaster null");
                    Outcome_Fail(caravan);
                    return;
                }
                ambush = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, SitePartDefOf.AmbushEdge, this.Tile, turncoat.Faction, true, StorytellerUtility.DefaultSiteThreatPointsNow());
                friendly = set1;
                Find.WorldObjects.Remove(set1);
                Find.WorldObjects.Remove(set2);
                Find.WorldObjects.Add(turncoat);
            }
            else
            {
                
                turncoat = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                turncoat.SetFaction((from f in Find.FactionManager.AllFactions
                            where f.HostileTo(Faction.OfPlayer) && !f.def.hidden
                            select f).RandomElementWithFallback(null));
                turncoat.Tile = set1.Tile;
                turncoat.Name = set1.Name;
                if (turncoat.Faction == null)
                {
                    Log.Warning("Disaster null");
                    Outcome_Fail(caravan);
                    return;
                }
                
                friendly = set2;
                Find.WorldObjects.Remove(set1);
                Find.WorldObjects.Remove(set2);
                Find.WorldObjects.Add(turncoat);
                ambush = SiteMaker.MakeSite(SiteCoreDefOf.Nothing, SitePartDefOf.AmbushEdge, this.Tile, turncoat.Faction, true, StorytellerUtility.DefaultSiteThreatPointsNow());
            }
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeDisaster".Translate(), "DisputeDisaster".Translate(turncoat, turncoat.Faction, friendly)
                    , LetterDefOf.ThreatBig, turncoat, null, "Disaster");

            LongEventHandler.QueueLongEvent(new Action(() => {
                MapGenerator.GenerateMap(new IntVec3(110,1,110), ambush, MapGeneratorDefOf.Encounter);
                // Balance
                float point = StorytellerUtility.DefaultSiteThreatPointsNow() + 300;

                List<PawnKindDef> kindDefs = new List<PawnKindDef>();
                for (int i = 0; i < 2; i++)
                {
                    kindDefs.Clear();
                    Lord lord = LordMaker.MakeNewLord(i == 0 ? turncoat.Faction : this.Faction, new LordJob_AssaultColony(i == 0 ? turncoat.Faction : this.Faction), ambush.Map);
                    IntVec3 vec = CellFinder.RandomClosewalkCellNear(new IntVec3(ambush.Map.Center.x - 30 + (i * 60), ambush.Map.Center.y, ambush.Map.Center.z), ambush.Map, 10);
                    
                    kindDefs=Utilities.GeneratePawnKindDef(45, i == 0 ? turncoat.Faction : this.Faction);
                    Utilities.GenerateFighter(point, lord, kindDefs, ambush.Map, i == 0 ? turncoat.Faction : this.Faction, vec);
                }

                CaravanEnterMapUtility.Enter(caravan, ambush.Map,CaravanEnterMode.Center, CaravanDropInventoryMode.DoNotDrop, true);

            }), "GeneratingMapForNewEncounter",false,null);


            Utilities.FactionsWar().GetByFaction(turncoat.Faction).resources += FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE * 2;
            Utilities.FactionsWar().GetByFaction(friendly.Faction).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE * 2;
            Find.WorldObjects.Remove(this);
        }

        private void Outcome_Backfire(Caravan caravan)
        {
            this.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -25);
            Settlement turncoat, friendly;
            bool chance = Rand.Chance(0.5f);
            if (chance)
            {
                turncoat = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                turncoat.SetFaction((from f in Find.FactionManager.AllFactions
                                     where f.HostileTo(Faction.OfPlayer) && !f.def.hidden
                                     select f).RandomElementWithFallback(null));
                turncoat.Tile = set2.Tile;
                turncoat.Name = set2.Name;
                if (turncoat.Faction == null)
                {
                    Log.Warning("Disaster null");
                    Outcome_Fail(caravan);
                    return;
                }
                List<Thing> rewards = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
                {
                    totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow()))
                });
                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = (int)FE_IncidentWorker_Jointraid.SilverBonusRewardCurve.Evaluate(set1.Faction.PlayerGoodwill);
                turncoat.GetComponent<WorldComp_JointRaid>().StartComp(600,set1.Faction, rewards, silver);
                Find.WorldObjects.Remove(set2);
                Find.WorldObjects.Add(turncoat);
                friendly = set1;
            }
            else
            {
                turncoat = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                turncoat.SetFaction((from f in Find.FactionManager.AllFactions
                                     where f.HostileTo(Faction.OfPlayer) && !f.def.hidden
                                     select f).RandomElementWithFallback(null));
                turncoat.Tile = set1.Tile;
                turncoat.Name = set1.Name;
                if (turncoat.Faction == null)
                {
                    Log.Warning("Disaster null");
                    Outcome_Fail(caravan);
                    return;
                }
                List<Thing> rewards = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
                {
                    totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow()))
                });
                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = (int)FE_IncidentWorker_Jointraid.SilverBonusRewardCurve.Evaluate(set2.Faction.PlayerGoodwill);
                turncoat.GetComponent<WorldComp_JointRaid>().StartComp(600, set1.Faction,rewards, silver);
                Find.WorldObjects.Remove(set1);
                Find.WorldObjects.Add(turncoat);
                friendly = set2;
            }
            Utilities.FactionsWar().GetByFaction(turncoat.Faction).resources += FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE * 2;
            Utilities.FactionsWar().GetByFaction(friendly.Faction).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE * 2;
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeBackfire".Translate(), "DisputeBackfire".Translate(turncoat, turncoat.Faction, friendly)
                   , LetterDefOf.ThreatBig, turncoat, Faction, (string)null);
            Find.WorldObjects.Remove(this);
        }
        private void Outcome_Fail(Caravan caravan)
        {
            Pawn bestDiplomat = BestCaravanPawnUtility.FindBestDiplomat(caravan);
            set1.Faction.TryAffectGoodwillWith(Faction.OfPlayer,-10);
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeFail".Translate(), "DisputeFail".Translate(bestDiplomat, set1, set2)
                   , LetterDefOf.NegativeEvent, null, Faction, (string)null);
            Find.WorldObjects.Remove(this);
        }

        private void Outcome_MildSuccess(Caravan caravan)
        {
            Pawn bestDiplomat = BestCaravanPawnUtility.FindBestDiplomat(caravan);
            set1.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 10);
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeMildSuccess".Translate(), "DisputeMildSuccess".Translate(bestDiplomat, set1, set2)
                   , LetterDefOf.PositiveEvent, null, null, (string)null);
            Find.WorldObjects.Remove(this);
        }
        private void Outcome_Success(Caravan caravan)
        {
            set1.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 25);
            
            using ( WorldPath path = Find.WorldPathFinder.FindPath(set1.Tile, Find.AnyPlayerHomeMap.Tile, (Caravan)null))
            {
                Pawn bestDiplomat = BestCaravanPawnUtility.FindBestDiplomat(caravan);
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
                int tile = p.First();

                WorldObject dispute = WorldObjectMaker.MakeWorldObject(EndGameDefOf.Roads_Camp);
                dispute.GetComponent<WorldComp_DisputeRoads>().StartComp(set1.Tile, set2.Tile, p);
                dispute.Tile = tile;
                dispute.SetFaction(Faction);
                Utilities.FactionsWar().GetByFaction(Faction).resources += FE_WorldComp_FactionsWar.MEDIUM_EVENT_RESOURCE_VALUE;
                Find.WorldObjects.Add(dispute);
                Find.LetterStack.ReceiveLetter("LetterLabelDisputeSuccess".Translate(), "DisputeSuccess".Translate(bestDiplomat, set1, set2)
                   , LetterDefOf.PositiveEvent, set1, null, (string)null);

                Find.WorldObjects.Remove(this);
                
            }
        }
        private void Outcome_Triumph(Caravan caravan)
        {
            Pawn bestDiplomat = BestCaravanPawnUtility.FindBestDiplomat(caravan);
            set1.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 45);

            WorldObject site = WorldObjectMaker.MakeWorldObject(EndGameDefOf.Dispute_FOB);
            site.SetFaction(set1.Faction);
            Utilities.FactionsWar().GetByFaction(site.Faction).resources += FE_WorldComp_FactionsWar.LARGE_EVENT_Cache_RESOURCE_VALUE;
            IntRange siteDistanceRange = SiteTuning.PeaceTalksQuestSiteDistanceRange;
            site.Tile=TileFinder.RandomSettlementTileFor(this.Faction, true, x => Utilities.Reachable(x, set1.Tile, 25));
            site.GetComponent<WorldComp_DisputeFOB>().StartComp(set1, set2);
            Find.WorldObjects.Add(site);
            Find.WorldObjects.Remove(this);
            Find.LetterStack.ReceiveLetter("LetterLabelDisputeTriumph".Translate(), "DisputeTriumph".Translate(bestDiplomat, set1, set2)
                   , LetterDefOf.PositiveEvent, site, null, (string)null);
        }

        private static float GetBadOutcomeWeightFactor(Pawn diplomat)
        {
            return WorldObject_Dispute.GetBadOutcomeWeightFactor(diplomat.GetStatValue(StatDefOf.NegotiationAbility, true));
        }

        private static float GetBadOutcomeWeightFactor(float negotationAbility)
        {
            return WorldObject_Dispute.BadOutcomeChanceFactorByNegotiationAbility.Evaluate(negotationAbility);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Settlement set1=this.set1;
            Scribe_References.Look(ref set1, "set1");
            this.set1 = set1;
            Settlement set2 = this.set2;
            Scribe_References.Look(ref set2, "set2");
            this.set2 = set2;
        }
    }
}
