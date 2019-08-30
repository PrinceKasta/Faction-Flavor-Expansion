using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.AI.Group;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{
    #region FactionsWar
    public class FE_WorldComp_FactionsWar : WorldComponent
    {
        #region vars
        public const int SETTLEMENT_RESOURCE_VALUE = 500, TECHLEVEL_RESOURCE_VALUE = 1000, LARGE_EVENT_Cache_RESOURCE_VALUE = 1350, MEDIUM_EVENT_RESOURCE_VALUE = 675, MINOR_EVENT_RESOURCE_VALUE = 337;
        
        private static readonly IntRange daysToDeclareWar = new IntRange(14, 18);
        private static readonly IntRange settlementGroupCount = new IntRange(7, 13);
        private static readonly IntRange PeaceEventChance = new IntRange(1, 100000);
        private static readonly IntRange WarEventChance = new IntRange(1, 10000);
        private int currentDaysToDeclareWar = -1;
        private List<War> Wars = new List<War>();
        public List<LE_FactionInfo> factionInfo = new List<LE_FactionInfo>();

        #endregion vars

        public LE_FactionInfo GetByFaction(Faction f)
        {
            LE_FactionInfo info = factionInfo.FirstOrDefault(x => f == x.faction);
            if (info == null)
            {
                return null;
            }
            return info;
        }
        
        public FE_WorldComp_FactionsWar(World world) : base(world)
        {
            
        }

        public override void WorldComponentTick()
        {
            
            base.WorldComponentTick();
            UpdatefactionInfo();
            if (factionInfo.Count == 0)
            {
                //Log.ErrorOnce("ResourcePool empty",1); 
                return;
            }
            if (!EndGame_Settings.FactionWar)
                return;
            // resource regenaration every 600 ticks of 1
            if (Find.TickManager.TicksGame % 600 == 0)
            {
                foreach (LE_FactionInfo f in factionInfo.ToList())
                {
                    if (f.resources < Find.WorldObjects.Settlements.Where(x => x.Faction == f.faction).Count() * SETTLEMENT_RESOURCE_VALUE + (int)f.faction.def.techLevel * TECHLEVEL_RESOURCE_VALUE)
                        f.resources += 0.1f + f.SupplyDepots.Count/10 + Find.WorldObjects.AllWorldObjects.Count(x=> x.GetComponent<WorldObjectComp_SupplyDepot>() != null && x.GetComponent<WorldObjectComp_SupplyDepot>().IsActive())/10;

                    if (TryUseResourcesAtPeace(f.faction))
                        break;

                }
                foreach(War war in Wars)
                    WarUpdate(war);
                WarEnd();
                ManageHiddenFaction();
            }

            if (Find.TickManager.TicksGame % 300 == 0)//
            //if((Find.TickManager.TicksGame % Global.DayInTicks * currentDaysToDeclareWar == 0))
            {
                TryDeclareWar();
                currentDaysToDeclareWar = daysToDeclareWar.RandomInRange;
            }

        }

        public List<War> GetWars()
        {
            return this.Wars;
        }

        public float GetResouceAmount(Faction f, float value = -99999)
        {
            if (value == -99999)
                return factionInfo.Find(x => x.faction == f).resources;
            factionInfo.Find(x => x.faction == f).resources += value;
            return factionInfo.Find(x => x.faction == f).resources;
        }

        private void ManageHiddenFaction()
        {
            int chance = PeaceEventChance.RandomInRange;
            if (chance < 5)
            {
                Faction hidden = Rand.Chance(0.5f) ? Faction.OfInsects : Faction.OfMechanoids;

                Settlement set;
                if (!Find.WorldObjects.Settlements.Where(f => factionInfo.Exists(x=> x.faction == f.Faction)).TryRandomElement(out set))
                    return;
                Find.WorldObjects.Remove(set);

                Messages.Message("MessageFactionWarHiddenRaid".Translate(hidden, set, set.Faction), MessageTypeDefOf.NeutralEvent, false);
                GetByFaction(set.Faction).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarHiddenRaid".Translate(hidden, set, set.Faction) + "\n\n";
            }
        }
        private bool TryUseResourcesAtPeace(Faction f)
        {
            if (f == null)
                return false;
            int chance = PeaceEventChance.RandomInRange;
            // Roads
            if (!f.def.techLevel.IsNeolithicOrWorse() && chance < 50)//!Find.WorldObjects.AllWorldObjects.FindAll(x=> x.def == EndGameDefOf.Roads_Camp).Any())//
            {
                Settlement set1 = new Settlement(), set2 = new Settlement();
                foreach (Settlement s in Find.WorldObjects.Settlements.Where(x => x.Faction == f).InRandomOrder())
                {
                    if (Find.WorldObjects.Settlements.Where(x => x != s && x.Faction == f && Utilities.Reachable(x, s, 50)).TryRandomElement(out set2))
                    {
                        set1 = s;
                        break;
                    }
                }
                if (set1 == null || set2 == null)
                    return false;
                int tile;
                List<int> p = new List<int>();
                //WorldPathFinder
                using (WorldPath path = Find.World.pathFinder.FindPath(set1.Tile, set2.Tile, null))
                {
                    p = path.NodesReversed;

                    for (int i = 0; i < (p.Count() - 1); i++)
                    {
                        if (Find.WorldGrid[p[i]].potentialRoads == null)
                        {
                            continue;
                        }
                        if (Find.WorldGrid[p[i]].potentialRoads.Any(x => x.road == RoadDefOf.AncientAsphaltHighway) || Find.WorldGrid[p[i]].potentialRoads.Any(x => x.road == RoadDefOf.AncientAsphaltRoad) || Find.WorldGrid[p[i]].potentialRoads.Any(x => x.road == EndGameDefOf.StoneRoad))
                        {
                            if (Find.WorldGrid[p[i + 1]].potentialRoads != null && (Find.WorldGrid[p[i + 1]].potentialRoads.Any(x => x.road == RoadDefOf.AncientAsphaltHighway) || Find.WorldGrid[p[i + 1]].potentialRoads.Any(x => x.road == RoadDefOf.AncientAsphaltRoad) || Find.WorldGrid[p[i + 1]].potentialRoads.Any(x => x.road == EndGameDefOf.StoneRoad)))
                                p.Remove(p[i+1]);
                        }
                        if (i == (p.Count() - 1))
                            return false;
                    }
                    if (p.Count()==0)
                        return false;
                    tile = p.First();
                    WorldObject dispute = WorldObjectMaker.MakeWorldObject(EndGameDefOf.Roads_Camp);
                    dispute.GetComponent<WorldComp_DisputeRoads>().StartComp(set1.Tile, set2.Tile, p);
                    dispute.Tile = tile;
                    dispute.SetFaction(f);
                    Find.WorldObjects.Add(dispute);
                    Messages.Message("MessageFactionRoads".Translate(set1.Faction, set1, set2), dispute, MessageTypeDefOf.NeutralEvent);
                    GetByFaction(f).resources -= MEDIUM_EVENT_RESOURCE_VALUE;
                    GetByFaction(f).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionRoads".Translate(set1.Faction, set1, set2) + "\n\n";
                }
                return true;
            }
            // Goodwill randomizer
            if(chance<100)
            {
                if (!f.def.CanEverBeNonHostile)
                    return false;
                Faction faction;
                IntRange goodwillChange = new IntRange(-10, 10);
                if (!Find.FactionManager.AllFactionsListForReading.Where(x => !x.def.isPlayer && x.def.CanEverBeNonHostile).TryRandomElement(out faction))
                    return false;
                f.TryAffectGoodwillWith(faction,goodwillChange.RandomInRange,false,false);
                factionInfo.Find(x => x.faction == f).resources -= 200;
                return true;
            }
            // Settlement Expansion
            if(chance<300 && Find.WorldObjects.Settlements.Where(x=> x.Faction==f).Count() < 3 && factionInfo.Find(x => x.faction == f).resources > 1000)
            {
                Settlement origin;
                if (!Find.WorldObjects.Settlements.Where(x => x.Faction == f).TryRandomElement(out origin)) 
                    return false;
                Settlement expand = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                expand.SetFaction(f);
                expand.Tile = TileFinder.RandomSettlementTileFor(expand.Faction, false, x => Find.WorldGrid.ApproxDistanceInTiles(Find.AnyPlayerHomeMap.Tile, x) <
                    Find.WorldGrid.ApproxDistanceInTiles(Find.AnyPlayerHomeMap.Tile, origin.Tile) - 20 && Find.WorldGrid.ApproxDistanceInTiles(x, origin.Tile) < 40
                    && TileFinder.IsValidTileForNewSettlement(x, (StringBuilder)null));
                expand.Name = SettlementNameGenerator.GenerateSettlementName(expand);
                Find.WorldObjects.Add(expand);
                Messages.Message("MessageExpanded".Translate(origin, f, expand), expand, MessageTypeDefOf.NeutralEvent, false);
                GetByFaction(f).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageExpanded".Translate(origin, f, expand) + "\n\n";
                factionInfo.Find(x=> x.faction==f).resources -= LARGE_EVENT_Cache_RESOURCE_VALUE;
                return true;
            }

            return false;
        }
        
        /*
         * Updates the factionInfo list to make sure any valid factions are in there and any invalid are removed
         */
         
        private void UpdatefactionInfo()
        {
            foreach (Faction f in Find.FactionManager.AllFactions.Where(x => !x.def.hidden && !x.IsPlayer && factionInfo.Where(fac=> fac.faction==x).Count()==0))
            {
                factionInfo.Add( new LE_FactionInfo(f, Find.WorldObjects.Settlements.Where(x => x.Faction == f).Count() * SETTLEMENT_RESOURCE_VALUE + (int)f.def.techLevel * TECHLEVEL_RESOURCE_VALUE));
            }
            foreach (LE_FactionInfo info in factionInfo.ToList())
            {
                if (!Find.FactionManager.AllFactions.Contains(info.faction) || info.faction.defeated)
                    factionInfo.Remove(info);
                else if (info.resources < 0 || info.resources > Find.WorldObjects.Settlements.Where(x => x.Faction == info.faction).Count() * SETTLEMENT_RESOURCE_VALUE + (int)info.faction.def.techLevel * TECHLEVEL_RESOURCE_VALUE + info.disposition * 200)
                    info.resources = Mathf.Clamp(info.resources, 0 , Find.WorldObjects.Settlements.Where(x => x.Faction == info.faction).Count() * SETTLEMENT_RESOURCE_VALUE + (int)info.faction.def.techLevel * TECHLEVEL_RESOURCE_VALUE + info.disposition * 200);

                if (info.SupplyDepots == null)
                    info.SupplyDepots = new List<int>();
                for (int i=0; i< info.SupplyDepots.Count; i++)
                {
                    if (info.SupplyDepots[i] == 0)
                    {
                        info.SupplyDepots.Remove(info.SupplyDepots[i]);
                        i--;
                    }
                    else info.SupplyDepots[i]--;
                }

            }
        }

        #region WarMethods

        // Finding two hostile faction that can war but are at peace now and adding them the warringFactions
        private bool TryDeclareWar()
        {
            //if(Rand.Chance(0.1f))
            if (Wars.Count > 2)
                return false;
            foreach (LE_FactionInfo attacker in factionInfo.Where(f => !Wars.Where(war => (war.DefenderFaction() == f.faction)).Any()).InRandomOrder()) //!warringFactions.ContainsKey(f) && !warringFactions.ContainsValue(f)).InRandomOrder())
            {
                //if (!Rand.Chance(0.001f + (GetByFaction(attacker.faction).disposition * 0.005f)))
                   // continue;
                foreach (LE_FactionInfo defender in factionInfo.Where(x => x.faction != attacker.faction && x.faction.HostileTo(attacker.faction) && attacker.faction.HostileTo(x.faction) && !Wars.Where(war => war.Equal(attacker.faction, x.faction)).Any()).InRandomOrder())
                {
                    
                    int uniqueId = !this.Wars.Any<War>() ? 1 : this.Wars.Max<War>((Func<War, int>)(o => o.uniqueId)) + 1;
                    War war = new War(uniqueId, attacker.faction, defender.faction);
                    Log.Warning("add war, " + attacker.faction + " attacker, " + defender.faction + " defender," + attacker.faction.HostileTo(defender.faction) + " hostile,  " + defender.faction.HostileTo(defender.faction) +", " + attacker.faction.GoodwillWith(defender.faction));
                    
                    if(defender.vassalage!=0)
                    {
                        Find.LetterStack.ReceiveLetter("LetterLabelFactionWarSubject".Translate(), "FactionWarSubject".Translate(defender.faction,attacker.faction), LetterDefOf.NegativeEvent);
                    }

                    foreach (LE_FactionInfo ally in factionInfo.Where(ally => ally != attacker && ally != defender && !Wars.Where(wars => wars.DefenderFaction() == ally.faction).Any()))
                    {
                        if (ally.faction.RelationKindWith(attacker.faction) == FactionRelationKind.Ally && Rand.Chance(0.6f))
                            war.AddFaction(ally.faction);
                        else if (ally.faction.RelationKindWith(defender.faction) == FactionRelationKind.Ally && Rand.Chance(0.6f))
                            war.AddFaction(null, defender.faction);
                    }
                    Wars.Add(war);
                    return true;
                }
            }
            return false;
        }

        private void WarUpdate(War war)
        {
            Faction f1 = war.AttackerFaction();
            Faction f2 = war.DefenderFaction();
            if (f1 == null || f2 == null || f1.defeated || f2.defeated)
                return;
            // Settlement conqured
            int chance = WarEventChance.RandomInRange;
            // Event Chance - 0.00695% every 600 ticks

            // Settlement Conqoured 0.0005%
            if (chance<5)
            {
                Settlement settlement;
                // if f1 
                if (Rand.Chance(0.5f + GetByFaction(f2).resources == GetByFaction(f1).resources ? GetByFaction(f2).resources / GetByFaction(f1).resources < 1 ? (0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f) : -(0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f) : 0))
                {
                    settlement = Find.WorldObjects.Settlements.Where(x => x.Faction == f1).RandomElement();
                    GetByFaction(f1).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementConqueredWinner".Translate(settlement,f2);
                    GetByFaction(f2).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementConqueredLoser".Translate(settlement,f1);
                }
                // if f2
                else
                {
                    settlement = Find.WorldObjects.Settlements.Where(x => x.Faction == f2).RandomElement();
                }

                Settlement set = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                set.SetFaction(settlement.Faction == f1 ? f2 : f1);

                set.Tile = settlement.Tile;
                set.Name = settlement.Name;
                Find.WorldObjects.Remove(settlement);
                Find.WorldObjects.Add(set);

                GetByFaction(set.Faction).resources += SETTLEMENT_RESOURCE_VALUE;
                GetByFaction(settlement.Faction).resources -= SETTLEMENT_RESOURCE_VALUE * 5;
                Messages.Message("MessageFactionWarSettlementConquered".Translate(set.Faction, settlement, settlement.Faction), MessageTypeDefOf.NeutralEvent);
                war.warHistory+="HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementConquered".Translate(set.Faction, settlement, settlement.Faction) + "\n\n";
                
                return;
            }
            // Settlement raided 0.0025%
            if (chance < 30)
            {
                Settlement settlement;
                // if f1 
                if (Rand.Chance(0.5f + GetByFaction(f2).resources / GetByFaction(f1).resources < 1 ? (0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f) : -(0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f)))
                {
                    settlement = Find.WorldObjects.Settlements.Where(x => x.Faction == f1).RandomElement();
                }
                // if f2
                else
                {
                    settlement = Find.WorldObjects.Settlements.Where(x => x.Faction == f2).RandomElement();
                }
                Find.WorldObjects.Remove(settlement);

                GetByFaction(settlement.Faction == f1 ? f2 : f1).resources += SETTLEMENT_RESOURCE_VALUE / 2;
                GetByFaction(settlement.Faction).resources -= SETTLEMENT_RESOURCE_VALUE * 5;
                Messages.Message("MessageFactionWarSettlementRaid".Translate(settlement.Faction == f1 ? f2 : f1, settlement, settlement.Faction), MessageTypeDefOf.NeutralEvent);
                war.warHistory+=("HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementRaid".Translate(settlement.Faction == f1 ? f2 : f1, settlement, settlement.Faction) + "\n\n");
                return;
            }
            // Artifact cache - Background 0.007%
            if (chance < 100)
            {
                if (Rand.Chance(0.5f))
                {
                    GetByFaction(f1).resources += LARGE_EVENT_Cache_RESOURCE_VALUE;
                    //Messages.Message("MessageFactionWarArtifactCache".Translate(f1), MessageTypeDefOf.NeutralEvent);
                    war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarArtifactCache".Translate(f1) + "\n\n";
                }
                else
                {
                    GetByFaction(f2).resources += LARGE_EVENT_Cache_RESOURCE_VALUE;
                    //Messages.Message("MessageFactionWarArtifactCache".Translate(f2), MessageTypeDefOf.NeutralEvent);
                    war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarArtifactCache".Translate(f1) + "\n\n";
                }
                return;
            }
            // Farms burned - Background 0.01%
            if (chance < 200)
            {
                if (Rand.Chance(0.5f))
                {
                    GetByFaction(f2).resources -= MEDIUM_EVENT_RESOURCE_VALUE;
                   // Messages.Message("MessageFactionWarFarms".Translate(f1, f2), MessageTypeDefOf.NeutralEvent);
                    war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFarms".Translate(f1, f2) + "\n\n";
                }
                else
                {
                    GetByFaction(f1).resources -= MEDIUM_EVENT_RESOURCE_VALUE;
                    //Messages.Message("MessageFactionWarFarms".Translate(f2, f1), MessageTypeDefOf.NeutralEvent);
                    war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFarms".Translate(f2, f1) + "\n\n";
                }
                return;
            }
            // SupplyDepot 0.0025%
            if (chance < 225)
            {
                int tile = 0;
                Settlement settlement = Find.WorldObjects.Settlements.Where(x => x.Faction == f1 || x.Faction == f2).RandomElement();
                if (!TileFinder.TryFindPassableTileWithTraversalDistance(settlement.Tile, 5, 25, out tile))
                    return;
                if (settlement.Faction.HostileTo(Faction.OfPlayer))
                {

                    Site worldObject = (Site)SiteMaker.MakeSite(SiteCoreDefOf.Nothing, SitePartDefOf.Outpost, tile, settlement.Faction);
                    worldObject.GetComponent<TimeoutComp>().StartTimeout(Global.DayInTicks * 14);
                    WorldObjectComp_SupplyDepot.Type type = (Rand.Chance(0.5f) ? WorldObjectComp_SupplyDepot.Type.Food : WorldObjectComp_SupplyDepot.Type.Weapons);
                    worldObject.GetComponent<WorldObjectComp_SupplyDepot>().StartComp(type);
                    worldObject.customLabel = "Supply Depot: " + type;
                    Find.WorldObjects.Add(worldObject);
                    Messages.Message("MessageFactionWarSupply".Translate(settlement.Faction), worldObject, MessageTypeDefOf.NeutralEvent, false);
                    
                }
                GetByFaction(settlement.Faction).SupplyDepots.Add(Global.DayInTicks * 14);
                GetByFaction(settlement.Faction).resources -= MEDIUM_EVENT_RESOURCE_VALUE;
                Messages.Message("MessageFactionWarSupply".Translate(settlement.Faction),null, MessageTypeDefOf.NeutralEvent, false);
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSupply".Translate(settlement.Faction) + "\n\n";
                return;
            }
            // Caravan ambushed - Background 0.0125%
            if (chance < 375)
            {
                Faction ambusher = Rand.Chance(0.5f) ? f2 : f1;

                GetByFaction(ambusher == f1 ? f2 : f1).resources -= MINOR_EVENT_RESOURCE_VALUE;
                //Messages.Message("MessageFactionWarCaravanAmbush".Translate(ambusher, ambusher == f1 ? f2 : f1), MessageTypeDefOf.NeutralEvent);
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarCaravanAmbush".Translate(ambusher, ambusher == f1 ? f2 : f1) + "\n\n";
                return;
            }
            // Minor Outpost raided - Background 0.01%
            if (chance < 475)
            {
                Faction raider = Rand.Chance(0.5f) ? f2 : f1;

                GetByFaction(raider == f1 ? f2 : f1).resources -= MEDIUM_EVENT_RESOURCE_VALUE;
                GetByFaction(raider).resources += MEDIUM_EVENT_RESOURCE_VALUE / 2;
                //Messages.Message("MessageFactionWarMinorOutpostRaid".Translate(raider, raider == f1 ? f2 : f1), MessageTypeDefOf.NeutralEvent);
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarMinorOutpostRaid".Translate(raider, raider == f1 ? f2 : f1)+"\n\n";
                return;
            }
            // Failed Settlement raid - Background 0.01%
            if (chance<575)
            {
                Settlement settlement;
                // if f1 
                if (Rand.Chance(0.5f + GetByFaction(f2).resources / GetByFaction(f1).resources < 1 ? (0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f) : -(0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f)))
                {
                    settlement = Find.WorldObjects.Settlements.Where(x => x.Faction == f1).RandomElement();
                }
                // if f2
                else
                {
                    settlement = Find.WorldObjects.Settlements.Where(x => x.Faction == f2).RandomElement();
                }
                Find.WorldObjects.Remove(settlement);

                GetByFaction(settlement.Faction == f1 ? f2 : f1).resources -= LARGE_EVENT_Cache_RESOURCE_VALUE;//SETTLEMENT_RESOURCE_VALUE / 2;
                
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFailRaid".Translate(settlement.Faction == f1 ? f2 : f1, settlement, settlement.Faction) + "\n\n";
                return;
            }

            // settlement Nuked - toxic fallout 0.002%
            if (chance<595 && Find.TickManager.TicksGame > Global.DayInTicks * 20 && Find.Storyteller.difficulty.difficulty>=2 && !Find.AnyPlayerHomeMap.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
            {
                Settlement ruin;
                if (!(Rand.Chance(0.5f + GetByFaction(f2).resources == GetByFaction(f1).resources ? GetByFaction(f2).resources / GetByFaction(f1).resources < 1 ? (0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f) : -(0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f) : 0) && (f1.def.techLevel == TechLevel.Industrial || f1.def.techLevel == TechLevel.Spacer) &&
                    Find.WorldObjects.Settlements.Where(x => x.Faction == f2 && Utilities.Reachable(Find.AnyPlayerHomeMap.Tile, x.Tile, 30)).TryRandomElement(out ruin)))
                {
                    if (!((f2.def.techLevel == TechLevel.Industrial || f1.def.techLevel == TechLevel.Spacer) && Find.WorldObjects.Settlements.Where(x => x.Faction == f1 && Utilities.Reachable(Find.AnyPlayerHomeMap.Tile, x.Tile, 30)).TryRandomElement(out ruin)))
                    {
                        return;
                    }
                }
                Find.WorldObjects.Remove(ruin);
                
                GetByFaction(ruin.Faction).resources -= SETTLEMENT_RESOURCE_VALUE * 7;
                IncidentParms parms = new IncidentParms();
                parms.forced = true;
                parms.target = Find.AnyPlayerHomeMap;
                IncidentDefOf.ToxicFallout.Worker.TryExecute(parms);
                Messages.Message("MessageFactionWarSettlementNuked".Translate(ruin, ruin.Faction == f1 ? f2 : f1), MessageTypeDefOf.ThreatSmall);

                GetByFaction(ruin.Faction == f1 ? f2 : f1).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementWinner".Translate(ruin,ruin.Faction, ruin.Faction == f1 ? f2 : f1) + "\n\n";
                GetByFaction(ruin.Faction).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementNukedLoser".Translate(ruin,ruin.Faction == f1 ? f2 : f1) + "\n\n";
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementNukedHistory".Translate(ruin.Faction == f1 ? f2 : f1, ruin, ruin.Faction) + "\n\n";
                return;
            }
            // Factories sabotaged - background - 0.01%
            if(chance<695)
            {
                Faction spy = Rand.Chance(0.5f) ? f2 : f1;

                GetByFaction(spy == f1 ? f2 : f1).resources -= MINOR_EVENT_RESOURCE_VALUE;
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFactory".Translate(spy, spy == f1 ? f2 : f1) + "\n\n";
                return;
            }

        }
        private void WarEnd()
        {
            //foreach (KeyValuePair<Faction, Faction> f in warringFactions.ToList())
            foreach(War war in Wars.ToList())
            {
                float winnerTotal = 0;
                
                if (GetByFaction(war.AttackerFaction()).resources + war.GetAlliesAttackerrResources()/3 <= 0)
                {
                    winnerTotal = war.GetAlliesAttackerrResources();
                    Wars.Remove(war);
                    WarAftermath(war.AttackerFaction(), war.DefenderFaction(), winnerTotal);

                }
                if (GetByFaction(war.DefenderFaction()).resources + war.GetAlliesDefenderrResources() <= 0)
                {
                    winnerTotal = war.GetAlliesDefenderrResources();
                    Wars.Remove(war);
                    WarAftermath(war.DefenderFaction(), war.AttackerFaction(), winnerTotal);
                }
            }
        }
        // Balance High priority
        private void WarAftermath(Faction loser, Faction winner, float allyResources)
        {
            if (loser == null || winner == null)
                return;
            if (GetByFaction(winner).resources + allyResources < 100)
            {
                Messages.Message("MessageFactionWarWhitePeace".Translate(loser, winner), MessageTypeDefOf.SituationResolved);
                GetByFaction(loser).resources += (int)loser.def.techLevel * TECHLEVEL_RESOURCE_VALUE;

                GetByFaction(loser).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarWhitePeace".Translate(loser, winner) + "\n\n";
                GetByFaction(winner).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarWhitePeace".Translate(loser, winner) + "\n\n";
                return;
            }
            if (GetByFaction(winner).resources + allyResources < 4000)
            {
                Messages.Message("MessageFactionWarFactionDestoryed".Translate(loser, winner), MessageTypeDefOf.SituationResolved);
                GetByFaction(loser).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFactionDestoryed".Translate(loser, winner) + "\n\n";
                GetByFaction(winner).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFactionDestoryed".Translate(loser, winner) + "\n\n";

                IEnumerable<Settlement> settlements = Find.WorldObjects.Settlements.Where(x => x.Faction == loser);
                int groupSize = 0;
                Faction splinterFaction = new Faction();

                foreach (Settlement s in settlements.ToList())
                {
                    if (groupSize == 0)
                    {
                        groupSize = settlementGroupCount.RandomInRange;
                        splinterFaction = FactionGenerator.NewGeneratedFaction(loser.def);
                        splinterFaction.colorFromSpectrum = new FloatRange(0, 1).RandomInRange;
                        splinterFaction.centralMelanin = Rand.Value;

                        Find.WorldObjects.Remove(Find.WorldObjects.Settlements.Where(x => x.Faction == splinterFaction).First());
                    }

                    Settlement replace = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    replace.Tile = s.Tile;
                    replace.SetFaction(splinterFaction);
                    replace.Name = s.Name;

                    Find.WorldObjects.Remove(s);
                    Find.WorldObjects.Add(replace);
                    groupSize--;
                    if (groupSize == 0)
                    {
                        Find.FactionManager.Add(splinterFaction);
                    }
                }
                if (groupSize > 0)
                    Find.FactionManager.Add(splinterFaction);
                foreach (WorldObject ob in Find.WorldObjects.AllWorldObjects.Where(x => x.Faction == loser).ToList())
                {
                    Find.WorldObjects.Remove(ob);
                }
                loser.defeated = true;
                factionInfo.Remove(GetByFaction(loser));
                return;
            }

            //if(resourcePool[winner] + allyResources < 7000) if another case is added later.
            {
                Messages.Message("MessageFactionWarFactionConquered".Translate(loser, winner), MessageTypeDefOf.SituationResolved);
                GetByFaction(loser).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFactionConquered".Translate(loser, winner) + "\n\n";
                GetByFaction(winner).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFactionConquered".Translate(loser, winner) + "\n\n";

                foreach (Settlement s in Find.WorldObjects.Settlements.Where(x => x.Faction == loser).ToList())
                {
                    Settlement replace = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    replace.Tile = s.Tile;
                    replace.SetFaction(winner);
                    replace.Name = s.Name;
                    Find.WorldObjects.Remove(s);
                    Find.WorldObjects.Add(replace);
                }
                foreach (WorldObject ob in Find.WorldObjects.AllWorldObjects.Where(x => x.Faction == loser).ToList())
                {
                    Find.WorldObjects.Remove(ob);
                }
                GetByFaction(winner).resources = Find.WorldObjects.Settlements.Where(x => x.Faction == winner).Count() * SETTLEMENT_RESOURCE_VALUE + (int)winner.def.techLevel * TECHLEVEL_RESOURCE_VALUE;
                loser.defeated = true;
                factionInfo.Remove(GetByFaction(loser));
                return;
            }
        }
        #endregion WarMethod

        public override void ExposeData()
        {
            Scribe_Values.Look(ref currentDaysToDeclareWar, "currentDaysToDeclareWar");
            Scribe_Collections.Look(ref factionInfo, "factionInfo", LookMode.Deep);
            Scribe_Collections.Look(ref Wars, "Wars", LookMode.Deep);
            
        }
    }

    #endregion FactionsWar

    #region class LE_Faction
    public class LE_FactionInfo : IExposable
    {
        public Faction faction;
        public string history = "";
        public string ancientHistory = "";
        public float resources;
        public List<int> SupplyDepots = new List<int>();
        public int disposition = 0;
        // 0 - nothing, - 1 tribute, 2- vassal
        public int vassalage = 0;
        public int vassalageResourseCooldown = 0;

        public LE_FactionInfo()
        {

        }

        public LE_FactionInfo(Faction faction, int resources)
        {
            this.faction = faction;
            this.ancientHistory = HistoryDialogDataBase.GenerateHistory(faction, ref disposition);
            if (faction.def.permanentEnemy)
                disposition -= 4;
            this.resources = resources;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref history, "history");
            Scribe_Values.Look(ref ancientHistory, "ancientHistory");
            Scribe_Values.Look(ref resources, "resources");
            Scribe_Collections.Look(ref SupplyDepots, "SupplyDepots",LookMode.Value);
            Scribe_Values.Look(ref disposition, "disposition", defaultValue: 0);
            Scribe_Values.Look(ref vassalage, "vassalage", defaultValue : 0);
            Scribe_Values.Look(ref vassalageResourseCooldown, "vassalageResourseCooldown", defaultValue: 0);
        }
    }
    #endregion class LE_Faction

    #region War
    public class War : IExposable  , ILoadReferenceable
    {
        private Faction attackerFaction = new Faction();
        private Faction defenderFaction = new Faction();
        List<Faction> alliesAttacker = new List<Faction>();
        List<Faction> alliesDefender = new List<Faction>();

        public string warHistory = "";

        public int uniqueId = -1;

        public War()
        {

        }
        public Faction AttackerFaction()
        {
            return attackerFaction;
        }
        public Faction DefenderFaction()
        {
            return defenderFaction;
        }
        public War(int uniqueId, Faction attacker, Faction defender)
        {
            this.uniqueId = uniqueId;
            this.attackerFaction = attacker;
            this.defenderFaction = defender;

        }

        public bool Equal(Faction attacker, Faction defender)
        {
            if (this.attackerFaction == attacker && this.defenderFaction == defender)
                return true;
            return false;
        }

        public void AddFaction(Faction attacker=null, Faction defender=null)
        {
            
            if (attacker != null && attacker != attackerFaction && !alliesAttacker.Contains(attacker) && !alliesDefender.Contains(attacker))
                alliesAttacker.Add(attacker);
            if (defender != null && defender != defenderFaction && !alliesDefender.Contains(defender) && !alliesAttacker.Contains(defender))
                alliesDefender.Add(defender);
        }

        public bool TryFindAttackerAlly(Faction f)
        {
            if (alliesAttacker.Contains(f))
                return true;
            return false;
        }
        public bool TryFindDefenderAlly(Faction f)
        {
            if (alliesDefender.Contains(f))
                return true;
            return false;
        }

        public bool TryFindFactioninvolved(Faction f)
        {
            if (TryFindDefenderAlly(f) || TryFindAttackerAlly(f) || attackerFaction == f || defenderFaction == f)
                return true;
            return false;
        }

        public float GetAlliesAttackerrResources()
        {
            float total = 0;
            foreach(Faction ally in alliesAttacker)
            {
                total += Utilities.FactionsWar().GetResouceAmount(ally);
            }
            return total;
        }

        public float GetAlliesDefenderrResources()
        {
            float total = 0;
            foreach (Faction ally in alliesDefender)
            {
                total += Utilities.FactionsWar().GetResouceAmount(ally);
            }
            return total;
        }

        public string GetUniqueLoadID()
        {
            return "FactionWar_" + uniqueId;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref uniqueId, "uniqueid");
            
            Scribe_References.Look(ref attackerFaction, "attackerFaction");
            Scribe_References.Look(ref defenderFaction, "defenderFaction");
            Scribe_Collections.Look(ref alliesAttacker, "alliesAttacker", LookMode.Reference);
            Scribe_Collections.Look(ref alliesDefender, "alliesDefender", LookMode.Reference);
            Scribe_Values.Look(ref warHistory, "warHistory");

        }
    };

    #endregion War
}
