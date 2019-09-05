﻿using RimWorld;
using RimWorld.Planet;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Verse;


namespace Flavor_Expansion
{
    
    class FactionHistoryDialog
    {
        public static DiaOption RequestFactionInfoOption(Faction faction, Pawn negotiator)
        {
            
            string text = "FactionInfo".Translate();
            DiaOption diaOption1 = new DiaOption(text);

            int disposition = Utilities.FactionsWar().GetByFaction(faction).disposition;
            DiaNode diaNode1 = new DiaNode("FactionChose".Translate(disposition == 0 ? "FactionNeutral".Translate() : disposition > 0 ? (disposition> 4 ? "FactionGenocidal".Translate() : "FactionWarlike".Translate()) : disposition<-4 ? "FactionPacifistic".Translate() : "FactionPeacelike".Translate()) + (Prefs.DevMode ? " (Debug): (" + disposition + ")\n" : "\n") + "FactionDispositionInfo".Translate()+ "FactionResourcesInfo".Translate(faction,Math.Floor(Utilities.FactionsWar().GetByFaction(faction).resources/Utilities.FactionsWar().MaxResourcesForFaction(faction) * 100))+(Prefs.DevMode ? ("(DevMode) resources:"+ Utilities.FactionsWar().GetByFaction(faction).resources)+", Total Capacity: "+ Utilities.FactionsWar().MaxResourcesForFaction(faction) : ""));
            
            #region History
            // Faction History
            if (EndGame_Settings.FactionHistory)
            {
                DiaNode diaNode2 = new DiaNode((Utilities.FactionsWar().GetByFaction(faction).ancientHistory).CapitalizeFirst() + (Utilities.FactionsWar().GetByFaction(faction).history).CapitalizeFirst());

                diaNode1.options.Add(new DiaOption("FactionHistory".Translate())
                {
                    link = diaNode2

                });

                diaNode2.options.Add(new DiaOption("GoBack".Translate())
                {
                    link = diaNode1
                });
            }
            #endregion History

            #region War
            
            if (EndGame_Settings.FactionWar)
            {
                foreach (War war in Find.World.GetComponent<FE_WorldComp_FactionsWar>().GetWars().Where(x=> x.AttackerFaction()==faction || x.DefenderFaction()==faction))
                {
                    // Faction Wars
                    DiaNode diaNode3 = new DiaNode(HistoryDialogDataBase.GetWarInfo(faction,war));

                    diaNode1.options.Add(new DiaOption("FactionWar".Translate(war.AttackerFaction() == faction ? war.DefenderFaction() : war.AttackerFaction()))
                    {
                        link = diaNode3

                    });
                    diaNode3.options.Add(new DiaOption("GoBack".Translate())
                    {
                        link = diaNode1
                    });
                }
            }
            #endregion War



            #region Vassal


            RequestVassalOptions(faction, diaNode1);
            
            #endregion Vassal

            diaNode1.options.Add(new DiaOption("GoBack".Translate())
            {
                linkLateBind = (Func<DiaNode>)(() => FactionDialogMaker.FactionDialogFor(negotiator, faction))

            });

            diaOption1.link = diaNode1;
            return diaOption1;
        }

        public static void RequestVassalOptions(Faction faction, DiaNode diaNode1)
        {
            DiaNode vassalInfo = new DiaNode("FactionVasalageInfo".Translate(faction));

            vassalInfo.options.Add(new DiaOption("Disconnect".Translate())
            {
                resolveTree = true
            });

            DiaNode tributaryInfo = new DiaNode("FactionTributaryInfo".Translate(faction));

            tributaryInfo.options.Add(new DiaOption("\"" + "Disconnect".Translate() + "\"")
            {
                resolveTree = true
            });

            if (Utilities.FactionsWar().GetByFaction(faction).vassalage == 0)
            {
                DiaOption vassalage = new DiaOption("FactionVassalage".Translate())
                {
                    link = vassalInfo,

                    action = new Action(() =>
                    {
                        Utilities.FactionsWar().GetByFaction(faction).vassalage = 2;
                        faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Ally);
                    })
                };

                if (!Prefs.DevMode || faction.def.permanentEnemy || (Utilities.FactionsWar().GetByFaction(faction).resources < 2000 && Utilities.FactionsWar().GetByFaction(faction).resources >= Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal || Utilities.FactionsWar().GetByFaction(faction).resources / Math.Min(Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal, 10000) > 0.25f - Utilities.FactionsWar().GetByFaction(faction).disposition / 100))
                {
                    DiaOption vassalagediaOption = new DiaOption("FactionVassalage".Translate());
                    if (faction.def.permanentEnemy)
                        vassalagediaOption.Disable("FactionVassalageDisabledEnemy".Translate());
                    else vassalagediaOption.Disable("FactionVassalageDisabled".Translate());
                    diaNode1.options.Add(vassalagediaOption);
                }
                else
                {
                    diaNode1.options.Add(vassalage);
                }

                DiaOption tribute = new DiaOption("FactionTributary".Translate())
                {
                    link = tributaryInfo,

                    action = new Action(() =>
                    {
                        Utilities.FactionsWar().GetByFaction(faction).vassalage = 1;
                        faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Neutral);                    })
                };

                if (!Prefs.DevMode || faction.def.permanentEnemy || (Utilities.FactionsWar().GetByFaction(faction).resources < 3000 && Utilities.FactionsWar().GetByFaction(faction).resources >= Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal || Utilities.FactionsWar().GetByFaction(faction).resources / Math.Min(Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal, 10000) > 0.5f - Utilities.FactionsWar().GetByFaction(faction).disposition / 100))
                {
                    DiaOption tributarydiaOption = new DiaOption("FactionTributary".Translate());
                    if (faction.def.permanentEnemy)
                        tributarydiaOption.Disable("FactionTributaryDisabledEnemy".Translate());
                    else tributarydiaOption.Disable("FactionTributaryDisabled".Translate());
                    diaNode1.options.Add(tributarydiaOption);
                }
                else
                {
                    diaNode1.options.Add(tribute);
                }

            }
            else if (Utilities.FactionsWar().GetByFaction(faction).vassalage == 2)
            {
                if (Find.TickManager.TicksGame > Utilities.FactionsWar().GetByFaction(faction).vassalageResourseCooldown)
                {
                    DiaOption vassalage = new DiaOption("FactionVassalDemandResources".Translate())
                    {
                        resolveTree = true,

                        action = new Action(() =>
                        {
                            if (!DefDatabase<ThingDef>.AllDefs.Where(x => x.CountAsResource && x.IsStuff && x.BaseMarketValue >= 5 && x.PlayerAcquirable && !x.CanHaveFaction).TryRandomElement(out ThingDef def))
                            {
                                Log.Error("No def found");
                                return;
                            }

                            Thing thing = ThingMaker.MakeThing(def);
                            thing.stackCount = new IntRange(75, 100).RandomInRange;
                            IntVec3 intVec3 = DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap);
                            DropPodUtility.DropThingsNear(intVec3, Find.AnyPlayerHomeMap, new List<Thing>() { thing }, 110, false, false, false);
                            Utilities.FactionsWar().GetByFaction(faction).vassalageResourseCooldown = Find.TickManager.TicksGame + Global.DayInTicks * 3;
                        })
                    };
                    diaNode1.options.Add(vassalage);
                }
                else
                {
                    DiaOption vassalage = new DiaOption("FactionVassalDemandResources".Translate());
                    vassalage.Disable("FactionVassalDemandResourcesDisabled".Translate());
                    diaNode1.options.Add(vassalage);
                }
                RequestInvestmentsNode(faction, diaNode1);
            }
        }

        public static void RequestInvestmentsNode(Faction faction, DiaNode diaNode1)
        {
            int cooldown = Global.DayInTicks * 3;
            DiaNode mainNode = new DiaNode("FactionInvestmentInfo".Translate());
            List<Thing> requirements = new List<Thing>();
            bool canUpgrade =FactionDialogUtilities.CanUpgradeRawMaterials(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Mining);

            #region Raw Materials

            DiaNode rawMaterialsNode = new DiaNode(FactionDialogUtilities.RawMaterialsTextPerLevel(faction, requirements));

            if (Utilities.FactionsWar().GetByFaction(faction).investments.Mining >= 4)
            {
                rawMaterialsNode.options.Add(new DiaOption("FactionInvestmentRawMaterialUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentMaxed".Translate()
                });
            } else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
            {
                rawMaterialsNode.options.Add(new DiaOption("FactionInvestmentRawMaterialUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentCoolDown".Translate()
                });
            }
            else if (!canUpgrade)
            {
                rawMaterialsNode.options.Add(new DiaOption("FactionInvestmentRawMaterialUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentNoPrice".Translate()
                });
            } else
            {
                rawMaterialsNode.options.Add(new DiaOption("FactionInvestmentRawMaterialUpgrade".Translate())
                {
                    action = () =>
                    {
                        if (Utilities.FactionsWar().GetByFaction(faction).investments.Mining < 4)
                        {
                            Utilities.FactionsWar().GetByFaction(faction).investments.Mining++;
                            rawMaterialsNode.text = FactionDialogUtilities.RawMaterialsTextPerLevel(faction, requirements);
                            Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                        }
                    },
                    resolveTree = true
                });
            }

            rawMaterialsNode.options.Add(new DiaOption("GoBack".Translate())
            {
                link = mainNode
            });

            mainNode.options.Add(new DiaOption("FactionInvestmentRawMaterial".Translate())
            {
                link = rawMaterialsNode
            });

            #endregion Raw Materials

            #region Medicine

            canUpgrade = FactionDialogUtilities.CanUpgradeRawMaterials(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Medicine);
            DiaNode medicineNode = new DiaNode(FactionDialogUtilities.MedicineTextPerLevel(faction, requirements));

            if(Utilities.FactionsWar().GetByFaction(faction).investments.Medicine >= 3)
            {
                medicineNode.options.Add(new DiaOption("FactionInvestmentMedicineUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentMaxed".Translate()
                });
            }
            else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
            {
                medicineNode.options.Add(new DiaOption("FactionInvestmentMedicineUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentCoolDown".Translate()
                });
            }
            else if(!canUpgrade)
            {
                medicineNode.options.Add(new DiaOption("FactionInvestmentMedicineUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentNoPrice".Translate()
                });
            }
            else
            {
                medicineNode.options.Add(new DiaOption("FactionInvestmentMedicineUpgrade".Translate())
                {
                    action = () =>
                    {
                        if (Utilities.FactionsWar().GetByFaction(faction).investments.Medicine < 3)
                        {
                            Utilities.FactionsWar().GetByFaction(faction).investments.Medicine++;
                            medicineNode.text = FactionDialogUtilities.MedicineTextPerLevel(faction, requirements);
                            Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                        }
                    },
                    resolveTree = true
                });
            }

            medicineNode.options.Add(new DiaOption("GoBack".Translate())
            {
                link = mainNode
            });

            mainNode.options.Add(new DiaOption("FactionInvestmentMedicine".Translate())
            {
                link = medicineNode
            });

            #endregion Medicine

            if (Utilities.FactionsWar().GetByFaction(faction).investments.Medicine == 3)
            {
                #region Drug labs

                canUpgrade = FactionDialogUtilities.CanUpgradeDruglabs(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Druglabs);
                DiaNode druglabsNode = new DiaNode(FactionDialogUtilities.DruglabsTextPerLevel(faction, requirements));

                if (Utilities.FactionsWar().GetByFaction(faction).investments.Druglabs >= 4)
                {
                    druglabsNode.options.Add(new DiaOption("FactionInvestmentDruglabsUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentMaxed".Translate()
                    });
                }
                else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
                {
                    druglabsNode.options.Add(new DiaOption("FactionInvestmentDruglabsUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentCoolDown".Translate()
                    });
                }
                else  if (!canUpgrade)
                {
                    druglabsNode.options.Add(new DiaOption("FactionInvestmentDruglabsUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentNoPrice".Translate()
                    });
                } else
                { 
                    druglabsNode.options.Add(new DiaOption("FactionInvestmentDruglabsUpgrade".Translate())
                    {
                        action = () =>
                        {
                            if (Utilities.FactionsWar().GetByFaction(faction).investments.Druglabs < 4)
                            {
                                Utilities.FactionsWar().GetByFaction(faction).investments.Druglabs++;
                                druglabsNode.text = FactionDialogUtilities.DruglabsTextPerLevel(faction, requirements);
                                Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                            }
                        },
                        resolveTree = true
                    });
                }

                druglabsNode.options.Add(new DiaOption("GoBack".Translate())
                {
                    link = mainNode
                });

                mainNode.options.Add(new DiaOption("FactionInvestmentDruglabs".Translate())
                {
                    link = druglabsNode
                });


                #endregion Drug labs
            }

            //Need Components
            if (Utilities.FactionsWar().GetByFaction(faction).investments.Components >= 1)
            {
                #region Prosthetics labs

                canUpgrade = FactionDialogUtilities.CanUpgradeProstheticslabs(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Prosthetics);
                DiaNode prostheticslabsNode = new DiaNode(FactionDialogUtilities.ProstheticslabsTextPerLevel(faction, requirements));
                if (Utilities.FactionsWar().GetByFaction(faction).investments.Prosthetics >= 4)
                {
                    prostheticslabsNode.options.Add(new DiaOption("FactionInvestmentProstheticslabsUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentMaxed".Translate()
                    });
                }
                else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
                {
                    prostheticslabsNode.options.Add(new DiaOption("FactionInvestmentProstheticslabsUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentCoolDown".Translate()
                    });
                }
                else if (!canUpgrade)
                {
                    prostheticslabsNode.options.Add(new DiaOption("FactionInvestmentProstheticslabsUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentNoPrice".Translate()
                    });
                }
                else
                {
                    prostheticslabsNode.options.Add(new DiaOption("FactionInvestmentProstheticslabsUpgrade".Translate())
                    {
                        action = () =>
                        {
                            if (Utilities.FactionsWar().GetByFaction(faction).investments.Prosthetics < 4)
                            {
                                Utilities.FactionsWar().GetByFaction(faction).investments.Prosthetics++;
                                prostheticslabsNode.text = FactionDialogUtilities.ProstheticslabsTextPerLevel(faction, requirements);
                                Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                            }
                        },
                        resolveTree = true
                    });
                }

                prostheticslabsNode.options.Add(new DiaOption("GoBack".Translate())
                {
                    link = mainNode
                });

                mainNode.options.Add(new DiaOption("FactionInvestmentProstheticslabs".Translate())
                {
                    link = prostheticslabsNode
                });

                #endregion Prosthetics labs
            }

            #region Food
            canUpgrade = FactionDialogUtilities.CanUpgradeFood(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Food);
            DiaNode foodNode = new DiaNode(FactionDialogUtilities.FoodTextPerLevel(faction, requirements));

            if (Utilities.FactionsWar().GetByFaction(faction).investments.Food >= 3)
            {
                foodNode.options.Add(new DiaOption("FactionInvestmentFoodUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentMaxed".Translate()
                });
            }
            else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
            {
                foodNode.options.Add(new DiaOption("FactionInvestmentFoodUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentCoolDown".Translate()
                });
            } 
            else if (!canUpgrade)
            {
                foodNode.options.Add(new DiaOption("FactionInvestmentDruglabsUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentNoPrice".Translate()
                });
            }
            else
            {
                foodNode.options.Add(new DiaOption("FactionInvestmentFoodUpgrade".Translate())
                {
                    action = () =>
                    {
                        if (Utilities.FactionsWar().GetByFaction(faction).investments.Food < 3)
                        {
                            Utilities.FactionsWar().GetByFaction(faction).investments.Food++;
                            foodNode.text = FactionDialogUtilities.FoodTextPerLevel(faction, requirements);
                            Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                        }
                    }
                });
            }

            foodNode.options.Add(new DiaOption("GoBack".Translate())
            {
                link = mainNode
            });

            mainNode.options.Add(new DiaOption("FactionInvestmentFood".Translate())
            {
                link = foodNode
            });
            #endregion Food

            
            // Need Steel
            if (Utilities.FactionsWar().GetByFaction(faction).investments.Mining >= 2)
            {
                canUpgrade = FactionDialogUtilities.CanUpgradeArmoryNWeaponry(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Armory);

                #region Armory

                DiaNode armoryNode = new DiaNode(FactionDialogUtilities.ArmoryTextPerLevel(faction, requirements));
                if (Utilities.FactionsWar().GetByFaction(faction).investments.Armory >= 4)
                {
                    armoryNode.options.Add(new DiaOption("FactionInvestmentArmoryUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentMaxed".Translate()
                    });
                }
                else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
                {
                    armoryNode.options.Add(new DiaOption("FactionInvestmentArmoryUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentCoolDown".Translate()
                    });
                }
                else if (!canUpgrade)
                {
                    armoryNode.options.Add(new DiaOption("FactionInvestmentArmoryUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentNoPrice".Translate()
                    });
                }
                else
                {
                    armoryNode.options.Add(new DiaOption("FactionInvestmentArmoryUpgrade".Translate())
                    {
                        action = () =>
                        {
                            if (Utilities.FactionsWar().GetByFaction(faction).investments.Armory < 4)
                            {
                                Utilities.FactionsWar().GetByFaction(faction).investments.Armory++;
                                armoryNode.text = FactionDialogUtilities.ArmoryTextPerLevel(faction, requirements);
                                Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                            }
                        },
                        resolveTree = true
                    });
                }

                armoryNode.options.Add(new DiaOption("GoBack".Translate())
                {
                    link = mainNode
                });

                mainNode.options.Add(new DiaOption("FactionInvestmentArmory".Translate())
                {
                    link = armoryNode
                });

                #endregion Armory

                canUpgrade = FactionDialogUtilities.CanUpgradeArmoryNWeaponry(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Weaponry);

                #region Weaponry

                DiaNode weaponryNode = new DiaNode(FactionDialogUtilities.WeaponryTextPerLevel(faction, requirements));

                if(Utilities.FactionsWar().GetByFaction(faction).investments.Weaponry >= 4)
                {
                    weaponryNode.options.Add(new DiaOption("FactionInvestmentWeaponryUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentMaxed".Translate()
                    });
                }
                else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
                {
                    weaponryNode.options.Add(new DiaOption("FactionInvestmentWeaponryUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentCoolDown".Translate()
                    });
                }
                else if (!canUpgrade)
                {
                    weaponryNode.options.Add(new DiaOption("FactionInvestmentWeaponryUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentNoPrice".Translate()
                    });
                }
                else
                {
                    weaponryNode.options.Add(new DiaOption("FactionInvestmentWeaponryUpgrade".Translate())
                    {
                        action = () =>
                        {
                            if (Utilities.FactionsWar().GetByFaction(faction).investments.Weaponry < 4)
                            {
                                Utilities.FactionsWar().GetByFaction(faction).investments.Weaponry++;
                                weaponryNode.text = FactionDialogUtilities.WeaponryTextPerLevel(faction, requirements);
                                Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                            }
                        },
                        resolveTree = true
                    });
                }

                weaponryNode.options.Add(new DiaOption("GoBack".Translate())
                {
                    link = mainNode
                });

                mainNode.options.Add(new DiaOption("FactionInvestmentWeaponry".Translate())
                {
                    link = weaponryNode
                });

                #endregion Weaponry

                #region Components
                canUpgrade = FactionDialogUtilities.CanUpgradeComponents(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Components);
                DiaNode componentsNode = new DiaNode(FactionDialogUtilities.ComponentsTextPerLevel(faction, requirements));

                if (Utilities.FactionsWar().GetByFaction(faction).investments.Components >= 3)
                {
                    componentsNode.options.Add(new DiaOption("FactionInvestmentComponentsUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentMaxed".Translate()
                    });
                }
                else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
                {
                    componentsNode.options.Add(new DiaOption("FactionInvestmentComponentsUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentCoolDown".Translate()
                    });
                }
                else if(!canUpgrade)
                {
                    componentsNode.options.Add(new DiaOption("FactionInvestmentComponentsUpgrade".Translate())
                    {
                        disabled = true,
                        disabledReason = "FactionInvestmentNoPrice".Translate()
                    });
                }
                else
                { 
                    componentsNode.options.Add(new DiaOption("FactionInvestmentComponentsUpgrade".Translate())
                    {
                        action = () =>
                        {
                            if (Utilities.FactionsWar().GetByFaction(faction).investments.Components < 3)
                            {
                                Utilities.FactionsWar().GetByFaction(faction).investments.Components++;
                                componentsNode.text = FactionDialogUtilities.ComponentsTextPerLevel(faction, requirements);
                                Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                            }
                        },
                        resolveTree = true
                    });
                }

                componentsNode.options.Add(new DiaOption("GoBack".Translate())
                {
                    link = mainNode
                });

                mainNode.options.Add(new DiaOption("FactionInvestmentComponents".Translate())
                {
                    link = componentsNode
                });
                #endregion Components
            }

            #region Trade

            canUpgrade = FactionDialogUtilities.CanUpgradeTrade(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Trade);
            DiaNode tradeNode = new DiaNode(FactionDialogUtilities.TradeTextPerLevel(faction, requirements));

            if (Utilities.FactionsWar().GetByFaction(faction).investments.Trade >= 3)
            {
                tradeNode.options.Add(new DiaOption("FactionInvestmentTradeUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentMaxed".Translate()
                });
            }
            else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
            {
                tradeNode.options.Add(new DiaOption("FactionInvestmentTradeUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentCoolDown".Translate()
                });
            }
            else if (!canUpgrade)
            {
                tradeNode.options.Add(new DiaOption("FactionInvestmentTradeUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentNoPrice".Translate()
                });
            } else
            { 
                tradeNode.options.Add(new DiaOption("FactionInvestmentTradeUpgrade".Translate())
                {
                    action = () =>
                    {
                        if (Utilities.FactionsWar().GetByFaction(faction).investments.Trade < 3)
                        {
                            Utilities.FactionsWar().GetByFaction(faction).investments.Trade++;
                            tradeNode.text = FactionDialogUtilities.TradeTextPerLevel(faction, requirements);
                            Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                        }
                    },
                    resolveTree = true
                });
            }

            tradeNode.options.Add(new DiaOption("GoBack".Translate())
            {
                link = mainNode
            });

            mainNode.options.Add(new DiaOption("FactionInvestmentTrade".Translate())
            {
                link = tradeNode
            });
            #endregion Trade

            #region Relations

            canUpgrade = FactionDialogUtilities.CanUpgradeRelations(faction, requirements, Utilities.FactionsWar().GetByFaction(faction).investments.Relations);
            DiaNode relationsNode = new DiaNode(FactionDialogUtilities.RelationsTextPerLevel(faction, requirements));

            if (Utilities.FactionsWar().GetByFaction(faction).investments.Relations >= 3)
            {
                relationsNode.options.Add(new DiaOption("FactionInvestmentRelationsUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentMaxed".Translate()
                });
            }
            else if (Find.TickManager.TicksGame < Utilities.FactionsWar().GetByFaction(faction).investments.cooldown)
            {
                relationsNode.options.Add(new DiaOption("FactionInvestmentRelationsUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentCoolDown".Translate()
                });
            }
            else if(!canUpgrade)
            {
                relationsNode.options.Add(new DiaOption("FactionInvestmentRelationsUpgrade".Translate())
                {
                    disabled = true,
                    disabledReason = "FactionInvestmentNoPrice".Translate()
                });
            }
            else
            {

                relationsNode.options.Add(new DiaOption("FactionInvestmentRelationsUpgrade".Translate())
                {
                    action = () =>
                    {
                        if (Utilities.FactionsWar().GetByFaction(faction).investments.Relations < 3)
                        {
                            Utilities.FactionsWar().GetByFaction(faction).investments.Relations++;
                            relationsNode.text = FactionDialogUtilities.RelationsTextPerLevel(faction, requirements);
                            Utilities.FactionsWar().GetByFaction(faction).investments.cooldown = Find.TickManager.TicksGame + cooldown;
                        }
                    },
                    resolveTree = true
                    
                });
            }

            relationsNode.options.Add(new DiaOption("GoBack".Translate())
            {
                link = mainNode
            });

            mainNode.options.Add(new DiaOption("FactionInvestmentRelations".Translate())
            {
                link = relationsNode
            });
            #endregion Relations

            //------------------------------------------------------------------------------\\
            mainNode.options.Add(new DiaOption("GoBack".Translate())
            {
                link = diaNode1
            });

            diaNode1.options.Add(new DiaOption("FactionInvestment".Translate())
            {
                link=mainNode
            });


        }
    };

    
    class HistoryDialogDataBase
    {

        public HistoryDialogDataBase()
        {

        }
        public static List<string> GetOptions()
        {
            List<string> HistoryOptions = new List<string>();

            if ("HistoryFactions".TryTranslate(out string text))
            {
                HistoryOptions.Add("HistoryFactions");
                int y = 2;
                while (("HistoryFactions" + y).TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryFactions" + y);
                    y++;
                }
                y = 1;
                while (("HistoryFactions" + y + "_Dead").TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryFactions" + y + "_Dead");
                    y++;
                }
            }
            if ("HistoryTown".TryTranslate(out text))
            {
                HistoryOptions.Add("HistoryTown");
                int y = 2;
                while (("HistoryTown" + y).TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryTown" + y);
                    y++;
                }
                y = 2;
                while (("HistoryTown" + y + "_Destroyed").TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryTown" + y + "_Destroyed");
                    y++;
                }
            }
            if ("HistoryPolitic".TryTranslate(out text))
            {
                HistoryOptions.Add("HistoryPolitic");
                int y = 2;
                while (("HistoryPolitic" + y).TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryPolitic" + y);
                    y++;
                }
            }

            if ("HistoryGeneric".TryTranslate(out text))
            {
                HistoryOptions.Add("HistoryGeneric");
                int y = 2;
                while (("HistoryGeneric" + y).TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryGeneric" + y);
                    y++;
                }
            }

            return HistoryOptions;
        }
        public static string GenerateHistory(Faction subject, ref int disposition)
        {
            
            IntRange leapInYears = new IntRange(7, 40);
            List<string> HistoryOptions = GetOptions();

            if (HistoryOptions.NullOrEmpty())
            {
                Log.Error("HistoryDialogDataBase not constracted correctly, HistoryOptions count: " + HistoryOptions.Count());
                return "";
            }
            string text = "";
            
            int year = Find.TickManager.StartingYear-300;
            while(year< Find.TickManager.StartingYear)
            {
                if (!HistoryOptions.TryRandomElement(out string option))
                {
                    return "";
                }
                text += "HistoryDate".Translate(year);

                if (option.Contains("HistoryFactions"))
                {
                    HistoryOptions.Remove(option);

                    if (option.Contains("DeadFaction"))
                    {
                        List<string> extantNames = new List<string>();
                         
                        text += TrimKeys(option.Translate(subject, NameGenerator.GenerateName(subject.def.factionNameMaker, Find.FactionManager.AllFactionsVisible.Select<Faction, string>((Func<Faction, string>)(fac => fac.Name)))), subject, ref disposition);
                    }
                    else text += TrimKeys( option.Translate(subject, Find.FactionManager.AllFactionsVisible.Where(fac=> fac!=subject && !fac.def.hidden).RandomElement().Name),subject, ref disposition);
                }
                if(option.Contains("HistoryTown"))
                {
                    HistoryOptions.Remove(option);
                    text += TrimKeys(option.Translate(option.Contains("Destroyed") ? SettlementNameGenerator.GenerateSettlementName(Find.WorldObjects.Settlements.FindAll(x => x.Faction == subject).RandomElement()) : Find.WorldObjects.Settlements.FindAll(x => x.Faction == subject).RandomElement().Name), subject, ref disposition);
                }
                if (option.Contains("HistoryPolitic"))
                {
                    HistoryOptions.Remove(option);
                    text += TrimKeys( TranslatorFormattedStringExtensions.Translate(option,PawnGenerator.GeneratePawn(subject.RandomPawnKind(), subject)), subject,ref disposition);
                }
                if (option.Contains("HistoryGeneric"))
                {
                    HistoryOptions.Remove(option);
                    text += TrimKeys(option.Translate(), subject, ref disposition);
                }
                year += leapInYears.RandomInRange;
            }
            return text;
        }

        private static string TrimKeys(string option, Faction subject, ref int disposition)
        {
            if (option.NullOrEmpty() || option == "")
            {
                Log.Warning("null");
                return "";
            }
            if (option.Contains("_!Tribal"))
            {
                if (subject.def.techLevel.IsNeolithicOrWorse())
                {
                    option = "";
                    option += "HistoryTribalFallback".Translate();
                }
                else option = option.Replace("_!Tribal", "");
            }
            if (option.Contains("_Warlike"))
            {
                disposition++;
                option=option.Replace("_Warlike", "");
            }
            if (option.Contains("_Peacelike"))
            {
                disposition--;
                option=option.Replace("_Peacelike", "");
            }
            return option;
        }

        public static string GetWarInfo(Faction subject, War war)
        {
            string text = "";
            List<War> wars = Utilities.FactionsWar().GetWars();

            while (wars.Count() > 20)
                wars.Remove(wars.First());

            float resourceAtt = Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources;
            float resourceDefe = Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources;
            if (resourceAtt / resourceDefe == 0 && resourceDefe / resourceAtt == 0)
                text += "FactionWarInfoSettlemate".Translate(subject, (subject == war.DefenderFaction() ? war.AttackerFaction() : war.DefenderFaction()));
            else text += "FactionWarInfo".Translate(subject, (subject == war.DefenderFaction() ? war.AttackerFaction() : war.DefenderFaction()), war.AttackerFaction(), resourceAtt > resourceDefe ? war.AttackerFaction() : war.DefenderFaction(), resourceAtt > resourceDefe ? Math.Floor((1-(float)resourceDefe / (float)resourceAtt) * 100) : Math.Floor((1-(float)resourceAtt / (float)resourceDefe) * 100)) + "\n\n";
            if (Prefs.DevMode)
                text += "Attacker resources: "+resourceAtt + ", defender resources: " + resourceDefe + "\n\n";
            text += war.warHistory;

            if (text == "")
            {
                text = "FactionAtPeace".Translate(subject);
            }
            return text;
        }
    };
}
