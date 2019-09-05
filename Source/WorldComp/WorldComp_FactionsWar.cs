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
        public const int SETTLEMENT_RESOURCE_VALUE = 500, TECHLEVEL_RESOURCE_VALUE = 1000, LARGE_EVENT_Cache_RESOURCE_VALUE = 750, MEDIUM_EVENT_RESOURCE_VALUE = 375, MINOR_EVENT_RESOURCE_VALUE = 160;
        
        private static readonly IntRange daysToDeclareWar = new IntRange(14, 18);
        private static readonly IntRange settlementGroupCount = new IntRange(7, 13);
        private static readonly IntRange PeaceEventChance = new IntRange(1, 100000);
        private static readonly IntRange WarEventChance = new IntRange(1, 100000);
        private int currentDaysToDeclareWar = -1;
        private List<War> Wars = new List<War>();
        public List<LE_FactionInfo> factionInfo = new List<LE_FactionInfo>();

        #endregion vars
        public static readonly SimpleCurve daysToExpansion = new SimpleCurve()
        {
            {
                new CurvePoint(5, Global.DayInTicks),
                true
            },
            {
                new CurvePoint(10f, Global.DayInTicks * 2),
                true
            },
            {
                new CurvePoint(15f, Global.DayInTicks * 4),
                true
            },
            {
                new CurvePoint(20f, Global.DayInTicks * 10),
                true
            },
            {
                new CurvePoint(25f, Global.DayInTicks * 15),
                true
            },
            {
                new CurvePoint(30f, Global.DayInTicks * 20),
                true
            }
        };
        
        
        public FE_WorldComp_FactionsWar(World world) : base(world)
        {
            
        }

        public override void WorldComponentTick()
        { 
            UpdatefactionInfo();
            NaturalSettlementExpansion();
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
                    if (f.resources < Find.WorldObjects.Settlements.Count(x => x.Faction == f.faction) * SETTLEMENT_RESOURCE_VALUE + (int)f.faction.def.techLevel * TECHLEVEL_RESOURCE_VALUE)
                        f.resources += 0.1f + f.SupplyDepots.Count/10 + Find.WorldObjects.AllWorldObjects.Count(x=> x.GetComponent<WorldObjectComp_SupplyDepot>() != null && x.GetComponent<WorldObjectComp_SupplyDepot>().IsActive())/10;

                    if (TryUseResourcesAtPeace(f.faction))
                        break;

                }
                foreach(War war in Wars)
                    WarUpdate(war);
               
                ManageHiddenFaction();
            }
            WarEnd();

            if (Find.TickManager.TicksGame>= currentDaysToDeclareWar || (Prefs.DevMode && Find.TickManager.TicksGame % 300 == 0))
            {
                TryDeclareWar();
                currentDaysToDeclareWar = Global.DayInTicks * daysToDeclareWar.RandomInRange + Find.TickManager.TicksGame;
            }

        }

        public List<War> GetWars()
        {
            return this.Wars;
        }
        public LE_FactionInfo GetByFaction(Faction f)
        {
            LE_FactionInfo info = factionInfo.FirstOrDefault(x => f == x.faction);
            if (info == null)
            {
                return null;
            }
            return info;
        }

        private void ManageHiddenFaction()
        {
            int chance = PeaceEventChance.RandomInRange;
            if (chance < 5)
            {
                Faction hidden = Rand.Chance(0.5f) ? Faction.OfInsects : Faction.OfMechanoids;
                
                if (!Find.WorldObjects.Settlements.Where(f => factionInfo.Exists(x=> x.faction == f.Faction)).TryRandomElement(out Settlement set))
                    return;
                Find.WorldObjects.Remove(set);

                Messages.Message("MessageFactionWarHiddenRaid".Translate(hidden, set, set.Faction), MessageTypeDefOf.NeutralEvent, false);
                GetByFaction(set.Faction).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarHiddenRaid".Translate(hidden, set, set.Faction) + "\n\n";
            }
        }

        /*
           * this method manages faction expansion.
           * Each faction has a timer, when it ends a new settlement is built
        */
        private void NaturalSettlementExpansion()
        {
            if (!EndGame_Settings.FactionExpansion)
                return;

            foreach (LE_FactionInfo info in factionInfo.ToList())
            {
                if (info.faction.defeated)
                {
                    factionInfo.Remove(info);
                    return;
                }
                
                if (info.expansionCoolddown > Find.TickManager.TicksGame)
                    return;
                
                info.expansionCoolddown = Find.TickManager.TicksGame + (int)daysToExpansion.Evaluate(Find.WorldObjects.Settlements.Count(s => s.Faction == info.faction));
                if (!Find.WorldObjects.Settlements.Where(x => x.Faction == info.faction).TryRandomElement(out Settlement origin))
                    continue;
                Settlement expand = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                expand.SetFaction(info.faction);
                expand.Tile = TileFinder.RandomSettlementTileFor(expand.Faction, false, x => Find.WorldGrid.ApproxDistanceInTiles(Find.AnyPlayerHomeMap.Tile, x) <
                    Find.WorldGrid.ApproxDistanceInTiles(Find.AnyPlayerHomeMap.Tile, origin.Tile) - 20 && Find.WorldGrid.ApproxDistanceInTiles(x, origin.Tile) < 40
                    && TileFinder.IsValidTileForNewSettlement(x, (StringBuilder)null));
                expand.Name = SettlementNameGenerator.GenerateSettlementName(expand,info.faction.def.settlementNameMaker);
                Find.WorldObjects.Add(expand);
                GetByFaction(info.faction).resources -= LARGE_EVENT_Cache_RESOURCE_VALUE;
                Messages.Message("MessageExpanded".Translate(origin, info.faction, expand), expand, MessageTypeDefOf.NeutralEvent, false);
                GetByFaction(info.faction).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageExpanded".Translate(origin, info.faction, expand) + "\n\n";


            }
        }

        private bool TryUseResourcesAtPeace(Faction f)
        {
            if (f == null)
                return false;
            int chance = PeaceEventChance.RandomInRange;
            // Roads
            if (!f.def.techLevel.IsNeolithicOrWorse() && chance < 50)
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
                IntRange goodwillChange = new IntRange(-10, 10);
                if (!Find.FactionManager.AllFactionsListForReading.Where(x => !x.def.isPlayer && x.def.CanEverBeNonHostile).TryRandomElement(out Faction faction))
                    return false;
                f.TryAffectGoodwillWith(faction,goodwillChange.RandomInRange,false,false);
                factionInfo.Find(x => x.faction == f).resources -= 200;
                return true;
            }
            // Settlement Expansion
            if(chance<300 && Find.WorldObjects.Settlements.Count(x=> x.Faction==f) < 3 && factionInfo.Find(x => x.faction == f).resources > 1000)
            {
                if (!Find.WorldObjects.Settlements.Where(x => x.Faction == f).TryRandomElement(out Settlement origin)) 
                    return false;
                Settlement expand = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                expand.SetFaction(f);
                expand.Tile = TileFinder.RandomSettlementTileFor(expand.Faction, false, x => Find.WorldGrid.ApproxDistanceInTiles(Find.AnyPlayerHomeMap.Tile, x) <
                    Find.WorldGrid.ApproxDistanceInTiles(Find.AnyPlayerHomeMap.Tile, origin.Tile) - 20 && Find.WorldGrid.ApproxDistanceInTiles(x, origin.Tile) < 40
                    && TileFinder.IsValidTileForNewSettlement(x, (StringBuilder)null));
                expand.Name = SettlementNameGenerator.GenerateSettlementName(expand,f.def.settlementNameMaker);
                Find.WorldObjects.Add(expand);
                Messages.Message("MessageExpanded".Translate(origin, f, expand), expand, MessageTypeDefOf.NeutralEvent, false);
                GetByFaction(f).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageExpanded".Translate(origin, f, expand) + "\n\n";
                factionInfo.Find(x=> x.faction==f).resources -= LARGE_EVENT_Cache_RESOURCE_VALUE;
                return true;
            }

            return false;
        }
        
        /*
         * Updates the factionInfo list to make sure any valid factions are in there and any invalid ones are removed
         */
        private void UpdatefactionInfo()
        {
            foreach (Faction f in Find.FactionManager.AllFactions.Where(x => !x.def.hidden && !x.defeated && !x.IsPlayer && factionInfo.Count(fac=> fac.faction==x) == 0))
            {
                factionInfo.Add( new LE_FactionInfo(f, Math.Max(Find.WorldObjects.Settlements.Count(x => x.Faction == f) , 1) * SETTLEMENT_RESOURCE_VALUE + (int)f.def.techLevel * TECHLEVEL_RESOURCE_VALUE));
            }
            foreach (LE_FactionInfo info in factionInfo.ToList())
            {
                if (!Find.FactionManager.AllFactions.Contains(info.faction) || info.faction.defeated)
                    factionInfo.Remove(info);
                else if (info.resources < 0 || info.resources > MaxResourcesForFaction(info.faction))
                    info.resources = Mathf.Clamp(info.resources, 0 , MaxResourcesForFaction(info.faction));

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

        public float MaxResourcesForFaction(Faction faction)
        {
            LE_FactionInfo info = GetByFaction(faction);
            return Find.WorldObjects.Settlements.Count(x => x.Faction == info.faction) * SETTLEMENT_RESOURCE_VALUE + (int)info.faction.def.techLevel * TECHLEVEL_RESOURCE_VALUE + info.disposition * 200;
        }

        #region WarMethods

        // Finding two hostile faction that can war but are at peace now and adding them the warringFactions
        private bool TryDeclareWar()
        {
            //if(Rand.Chance(0.1f))
            if (Wars.Count > 3)
                return false;
            foreach (LE_FactionInfo attacker in factionInfo.Where(f => !Wars.Where(war => (war.DefenderFaction() == f.faction)).Any()).InRandomOrder())
            {
                if (!Rand.Chance(0.001f + (GetByFaction(attacker.faction).disposition * 0.005f)))
                    continue;
                foreach (LE_FactionInfo defender in factionInfo.Where(x => x.faction != attacker.faction && x.faction.HostileTo(attacker.faction) && attacker.faction.HostileTo(x.faction) && !Wars.Where(war => war.Equal(attacker.faction, x.faction)).Any()).InRandomOrder())
                {
                    
                    int uniqueId = !this.Wars.Any<War>() ? 1 : this.Wars.Max<War>((Func<War, int>)(o => o.uniqueId)) + 1;
                    War war = new War(uniqueId, attacker.faction, defender.faction);
                    if(Prefs.DevMode)
                        Log.Warning("add war, " + attacker.faction + " attacker, " + defender.faction + " defender," + attacker.faction.HostileTo(defender.faction) + " hostile,  " + defender.faction.HostileTo(defender.faction) +", " + attacker.faction.GoodwillWith(defender.faction));
                    
                    if(defender.vassalage!=0)
                    {
                        Find.LetterStack.ReceiveLetter("LetterLabelFactionWarSubject".Translate(), "FactionWarSubject".Translate(defender.faction,attacker.faction), LetterDefOf.NegativeEvent);
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

            // Event Chance - 0.000675% every 600 ticks

            // Settlement Conqoured 0.00005%
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
            // Settlement raided 0.00005%
            if (chance < 10)
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
            // Artifact cache - Background 0.0007%
            if (chance < 80)
            {
                if (Rand.Chance(0.5f))
                {
                    GetByFaction(f1).resources += LARGE_EVENT_Cache_RESOURCE_VALUE;
                    war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarArtifactCache".Translate(f1) + "\n\n";
                }
                else
                {
                    GetByFaction(f2).resources += LARGE_EVENT_Cache_RESOURCE_VALUE;
                    war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarArtifactCache".Translate(f1) + "\n\n";
                }
                return;
            }
            // Farms burned - Background 0.001%
            if (chance < 180)
            {
                if (Rand.Chance(0.5f))
                {
                    GetByFaction(f2).resources -= MEDIUM_EVENT_RESOURCE_VALUE;
                    war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFarms".Translate(f1, f2) + "\n\n";
                }
                else
                {
                    GetByFaction(f1).resources -= MEDIUM_EVENT_RESOURCE_VALUE;
                    war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFarms".Translate(f2, f1) + "\n\n";
                }
                return;
            }
            // SupplyDepot 0.00025%
            if (chance < 205)
            {
                if (!Find.WorldObjects.Settlements.Where(x => (x.Faction == f1 && GetByFaction(f1).resources >= MEDIUM_EVENT_RESOURCE_VALUE * 2) || (x.Faction == f2 && GetByFaction(f1).resources >= MEDIUM_EVENT_RESOURCE_VALUE * 2)).TryRandomElement(out Settlement settlement))
                    return;
                if (!TileFinder.TryFindPassableTileWithTraversalDistance(settlement.Tile, 5, 25, out int tile))
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
                else
                {
                    GetByFaction(settlement.Faction).SupplyDepots.Add(Global.DayInTicks * 14);
                    Messages.Message("MessageFactionWarSupply".Translate(settlement.Faction), null, MessageTypeDefOf.NeutralEvent, false);
                }
                GetByFaction(settlement.Faction).resources -= MEDIUM_EVENT_RESOURCE_VALUE;
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSupply".Translate(settlement.Faction) + "\n\n";
                return;
            }
            // Caravan ambushed - Background 0.00125%
            if (chance < 355)
            {
                Faction ambusher = Rand.Chance(0.5f) ? f2 : f1;

                GetByFaction(ambusher == f1 ? f2 : f1).resources -= MINOR_EVENT_RESOURCE_VALUE;
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarCaravanAmbush".Translate(ambusher, ambusher == f1 ? f2 : f1) + "\n\n";
                return;
            }
            // Minor Outpost raided - Background 0.001%
            if (chance < 455)
            {
                Faction raider = Rand.Chance(0.5f) ? f2 : f1;

                GetByFaction(raider == f1 ? f2 : f1).resources -= MEDIUM_EVENT_RESOURCE_VALUE;
                GetByFaction(raider).resources += MEDIUM_EVENT_RESOURCE_VALUE / 2;
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarMinorOutpostRaid".Translate(raider, raider == f1 ? f2 : f1)+"\n\n";
                return;
            }
            // Failed Settlement raid - Background 0.001%
            if (chance<555)
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

                GetByFaction(settlement.Faction == f1 ? f2 : f1).resources -= LARGE_EVENT_Cache_RESOURCE_VALUE;
                
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFailRaid".Translate(settlement.Faction == f1 ? f2 : f1, settlement, settlement.Faction) + "\n\n";
                return;
            }

            // settlement Nuked - toxic fallout 0.0002%
            if (chance < 575 && Find.TickManager.TicksGame > Global.DayInTicks * 20 && Find.Storyteller.difficulty.difficulty >= 2 && !Find.AnyPlayerHomeMap.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
            {
                if (!(Rand.Chance(0.5f + GetByFaction(f2).resources == GetByFaction(f1).resources ? GetByFaction(f2).resources / GetByFaction(f1).resources < 1 ? (0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f) : -(0.5f - (GetByFaction(f2).resources / GetByFaction(f1).resources) / 2f) : 0) && (f1.def.techLevel == TechLevel.Industrial || f1.def.techLevel == TechLevel.Spacer) &&
                    Find.WorldObjects.Settlements.Where(x => x.Faction == f2 && Utilities.Reachable(Find.AnyPlayerHomeMap.Tile, x.Tile, 30)).TryRandomElement(out Settlement ruin)))
                {
                    if (!((f2.def.techLevel == TechLevel.Industrial || f1.def.techLevel == TechLevel.Spacer) && Find.WorldObjects.Settlements.Where(x => x.Faction == f1 && Utilities.Reachable(Find.AnyPlayerHomeMap.Tile, x.Tile, 30)).TryRandomElement(out ruin)))
                    {
                        return;
                    }
                }
                Find.WorldObjects.Remove(ruin);

                GetByFaction(ruin.Faction).resources -= SETTLEMENT_RESOURCE_VALUE * 7;
                IncidentParms parms = new IncidentParms()
                {
                    forced = true,
                    target = Find.AnyPlayerHomeMap
                };
                IncidentDefOf.ToxicFallout.Worker.TryExecute(parms);
                Messages.Message("MessageFactionWarSettlementNuked".Translate(ruin, ruin.Faction == f1 ? f2 : f1), MessageTypeDefOf.ThreatSmall);

                GetByFaction(ruin.Faction == f1 ? f2 : f1).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementWinner".Translate(ruin,ruin.Faction, ruin.Faction == f1 ? f2 : f1) + "\n\n";
                GetByFaction(ruin.Faction).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementNukedLoser".Translate(ruin,ruin.Faction == f1 ? f2 : f1) + "\n\n";
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarSettlementNukedHistory".Translate(ruin.Faction == f1 ? f2 : f1, ruin, ruin.Faction) + "\n\n";
                return;
            }
            // Factories sabotaged - background - 0.001%
            if(chance<675)
            {
                Faction spy = Rand.Chance(0.5f) ? f2 : f1;

                GetByFaction(spy == f1 ? f2 : f1).resources -= MINOR_EVENT_RESOURCE_VALUE;
                war.warHistory += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFactory".Translate(spy, spy == f1 ? f2 : f1) + "\n\n";
                return;
            }

        }

        private void WarEnd()
        {
            foreach(War war in Wars.ToList())
            {
                if (war.DefenderFaction().defeated || war.AttackerFaction().defeated)
                {
                    Wars.Remove(war);
                    continue;
                }
                if (GetByFaction(war.AttackerFaction()).resources<= 1)
                {
                    Wars.Remove(war);
                    WarAftermath(war.AttackerFaction(), war.DefenderFaction());
                    return;
                }
                if (GetByFaction(war.DefenderFaction()).resources<= 1)
                {
                    Wars.Remove(war);
                    WarAftermath(war.DefenderFaction(), war.AttackerFaction());
                }
            }
        }
        // Balance High priority
        private void WarAftermath(Faction loser, Faction winner)
        {
            if (loser == null || winner == null)
                return;
            if (GetByFaction(winner)?.resources < 100)
            {
                Messages.Message("MessageFactionWarWhitePeace".Translate(loser, winner), MessageTypeDefOf.SituationResolved);
                GetByFaction(loser).resources += (int)loser.def.techLevel * TECHLEVEL_RESOURCE_VALUE;

                GetByFaction(loser).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarWhitePeace".Translate(loser, winner) + "\n\n";
                GetByFaction(winner).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarWhitePeace".Translate(loser, winner) + "\n\n";
                return;
            }
            if (GetByFaction(winner).resources < 4000)
            {
                Messages.Message("MessageFactionWarFactionDestoryed".Translate(loser, winner), MessageTypeDefOf.SituationResolved);
                GetByFaction(loser).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFactionDestoryed".Translate(loser, winner) + "\n\n";
                GetByFaction(winner).history += "HistoryDate".Translate(5500 + Find.TickManager.TicksGame / Global.YearInTicks) + "MessageFactionWarFactionDestoryed".Translate(loser, winner) + "\n\n";

                IEnumerable<Settlement> settlements = Find.WorldObjects.Settlements.Where(x => x.Faction == loser);
                int groupSize = 0;
                int groupMax = settlements.Count()/2+1;
                Faction splinterFaction = new Faction();

                foreach (Settlement s in settlements.ToList())
                {
                    if (groupSize == 0)
                    {
                        splinterFaction = FactionGenerator.NewGeneratedFaction(loser.def);
                        splinterFaction.colorFromSpectrum =FactionGenerator.NewRandomColorFromSpectrum(splinterFaction);
                        
                        Find.WorldObjects.Remove(Find.WorldObjects.Settlements.Where(x=> x.Faction == splinterFaction).RandomElement());
                    }

                    Settlement replace = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    replace.Tile = s.Tile;
                    replace.SetFaction(splinterFaction);
                    replace.Name = s.Name;
                    Find.WorldObjects.Remove(s);
                    Find.WorldObjects.Add(replace);
                    groupSize++;
                    if (groupSize == groupMax)
                    {
                        Find.FactionManager.Add(splinterFaction);
                        Find.Maps.ForEach((x) => { x.pawnDestinationReservationManager.RegisterFaction(splinterFaction); });
                        groupSize = 0;
                    }
                }
                if (groupSize < groupMax)
                {
                    
                    Find.FactionManager.Add(splinterFaction);
                    Find.CurrentMap.pawnDestinationReservationManager.RegisterFaction(splinterFaction);
                }

                foreach (WorldObject ob in Find.WorldObjects.AllWorldObjects.Where(x => x.Faction == loser).ToList())
                {
                    Find.WorldObjects.Remove(ob);
                }
                loser.defeated = true;
                Wars.RemoveAll(war => war.DefenderFaction() == loser || war.AttackerFaction() == loser);
                factionInfo.Remove(GetByFaction(loser));
                return;
            }

            //if(GetByFaction(winner).resources < 7000) //if another case is added later.
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
                GetByFaction(winner).resources = Find.WorldObjects.Settlements.Count(x => x.Faction == winner) * SETTLEMENT_RESOURCE_VALUE + (int)winner.def.techLevel * TECHLEVEL_RESOURCE_VALUE;
                loser.defeated = true;
                Wars.RemoveAll(war => war.DefenderFaction() == loser || war.AttackerFaction() == loser);
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
        public Investments investments = new Investments();
        public int vassalage = 0; // 0 - nothing, - 1 tribute, 2- vassal
        public int vassalageResourseCooldown = 0;
        public int expansionCoolddown = 0;

        public LE_FactionInfo()
        {

        }

        public LE_FactionInfo(Faction faction, int resources)
        {
            vassalage = 0;
            this.faction = faction;
            disposition = 0;
            if (faction.def.permanentEnemy)
                disposition += 8;
            this.ancientHistory = HistoryDialogDataBase.GenerateHistory(faction, ref disposition);
            
            this.resources = resources;
            expansionCoolddown = Find.TickManager.TicksGame + (int)FE_WorldComp_FactionsWar.daysToExpansion.Evaluate(Find.WorldObjects.Settlements.Count(x => x.Faction == faction));
            vassalageResourseCooldown = 0;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref investments, "investments");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref history, "history");
            Scribe_Values.Look(ref ancientHistory, "ancientHistory");
            Scribe_Values.Look(ref resources, "resources", defaultValue : 0);
            Scribe_Collections.Look(ref SupplyDepots, "SupplyDepots",LookMode.Value);
            Scribe_Values.Look(ref disposition, "disposition", defaultValue: 0);
            Scribe_Values.Look(ref vassalage, "vassalage", defaultValue : 0);
            Scribe_Values.Look(ref vassalageResourseCooldown, "vassalageResourseCooldown", defaultValue: 0);
            Scribe_Values.Look(ref expansionCoolddown, "expansionCoolddown", defaultValue: 0);
        }
    }
    #endregion class LE_Faction

    public class Investments : IExposable
    {
        public int cooldown;
        public int Armory;
        public int Weaponry;
        public int Mining;
        public int Medicine;
        public int Druglabs;
        public int Prosthetics;
        public int Food;
        public int Components;
        public int Trade;
        public int Relations;

        public Investments()
        {
            cooldown = 0;
            Armory = 0;
            Weaponry = 0;
            Mining = 0;
            Medicine = 0;
            Druglabs = 0;
            Prosthetics = 0;
            Food = 0;
            Components = 0;
            Trade = 0;
            Relations = 0;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref cooldown, "cooldown", defaultValue: 0);
            Scribe_Values.Look(ref Armory, "Armory", defaultValue: 0);
            Scribe_Values.Look(ref Weaponry, "Weaponry", defaultValue: 0);
            Scribe_Values.Look(ref Mining, "Mining", defaultValue: 0);
            Scribe_Values.Look(ref Medicine, "Medicine", defaultValue: 0);
            Scribe_Values.Look(ref Druglabs, "Druglabs", defaultValue: 0);
            Scribe_Values.Look(ref Prosthetics, "Prosthetics", defaultValue: 0);
            Scribe_Values.Look(ref Food, "Food", defaultValue: 0);
            Scribe_Values.Look(ref Components, "Components", defaultValue: 0);
            Scribe_Values.Look(ref Trade, "Trade", defaultValue: 0);
            Scribe_Values.Look(ref Relations, "Relations", defaultValue: 0);
        }
    }

    #region War
    public class War : IExposable  , ILoadReferenceable
    {
        private Faction attackerFaction = new Faction();
        private Faction defenderFaction = new Faction();

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

        public bool TryFindFactioninvolved(Faction f)
        {
            if (attackerFaction == f || defenderFaction == f)
                return true;
            return false;
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

            Scribe_Values.Look(ref warHistory, "warHistory");

        }
    };

    #endregion War
}
