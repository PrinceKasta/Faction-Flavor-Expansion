using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;


namespace Flavor_Expansion
{

    class FactionHistoryDialog
    {
        public static DiaOption RequestFactionInfoOption(Faction faction, Pawn negotiator)
        {

            DiaOption diaOption1 = new DiaOption("FactionInfo".Translate());

            int disposition = Utilities.FactionsWar().GetByFaction(faction).disposition;
            DiaNode diaNode1 = new DiaNode("FactionChose".Translate(disposition == 0 ? "FactionNeutral".Translate() : disposition > 0 ? (disposition> 4 ? "FactionGenocidal".Translate() : "FactionWarlike".Translate()) : disposition<-4 ? "FactionPacifistic".Translate() : "FactionPeacelike".Translate()) + (Prefs.DevMode ? " (Debug): (" + disposition + ")\n" : "\n") + "FactionDispositionInfo".Translate()+ "FactionResourcesInfo".Translate(faction,Math.Floor(Utilities.FactionsWar().GetByFaction(faction).resources/Utilities.FactionsWar().MaxResourcesForFaction(faction) * 100))+(Prefs.DevMode ? (" (DevMode) resources:"+ Utilities.FactionsWar().GetByFaction(faction).resources)+", Total Capacity: "+ Utilities.FactionsWar().MaxResourcesForFaction(faction) : ""));
            
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
            
            RequestVassalOptions(faction, diaNode1);

            diaNode1.options.Add(new DiaOption("GoBack".Translate())
            {
                linkLateBind = () => FactionDialogMaker.FactionDialogFor(negotiator, faction)

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
                            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap), Find.AnyPlayerHomeMap, new List<Thing>() { thing }, 110, false, false, false);
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
                if (faction.def.techLevel < TechLevel.Industrial)
                    return;
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
        private static readonly IntRange leapInYears = new IntRange(7, 40);
        private static readonly IntRange startingYear = new IntRange(300, 400);

        private class WeightedString
        {
            public string option;
            public int weight;

            public WeightedString(string option, int weight)
            {
                this.option = option;
                this.weight = weight;
            }
        }

        public HistoryDialogDataBase()
        {

        }
        public static string GenerateHistory(Faction subject, ref int disposition)
        {
            
            List<WeightedString> HistoryOptions = new List<WeightedString>();
            List<string> history = new List<string>();
            string text = "";

            int year = Find.TickManager.StartingYear - startingYear.RandomInRange;
            while (year < Find.TickManager.StartingYear)
            {
                HistoryOptions.Clear();
                HistoryOptions.Add(new WeightedString(FE_GrammarUtility.History(null, null, null, PawnGenerator.GeneratePawn(subject.RandomPawnKind(), subject)), 12));
                HistoryOptions.Add(new WeightedString(FE_GrammarUtility.History(subject), 4));
                HistoryOptions.Add(new WeightedString(FE_GrammarUtility.History(subject, Find.FactionManager.AllFactionsVisible.Where(fac => !fac.IsPlayer && fac != subject && !fac.def.hidden).RandomElement()), 3));
                HistoryOptions.Add(new WeightedString(FE_GrammarUtility.History(null, null, Find.WorldObjects.Settlements.FindAll(x => x.Faction == subject).RandomElement(), null), 16));
                HistoryOptions.Add(new WeightedString(FE_GrammarUtility.History(null, null, null, null, SettlementNameGenerator.GenerateSettlementName(null, subject.def.settlementNameMaker)), 10));
                HistoryOptions.Add(new WeightedString(FE_GrammarUtility.History(), 9));
                
                if (!HistoryOptions.TryRandomElementByWeight(x => x.weight, out WeightedString option))
                {
                    return "";
                }
                if (!CompareAlmostEqual(option.option, history))
                {
                    history.Add("HistoryDate".Translate(year));
                    history.Add(option.option + "\n\n");
                    year += leapInYears.RandomInRange;
                }
            }
            for (int i = 0; i < history.Count - 1; i+=2)
            {
                text+=TrimKeys(history[i]+history[i+1], subject, ref disposition);
        
            }
            return text;
        }

        private static bool CompareAlmostEqual(string text, List<string> history)
        {
            foreach(string t in history)
            {
                if(FactionDialogUtilities.LevenshteinDistance.Calculate(text,t) < 40)
                {
                    return true;
                }
            }
            return false;
        }

        private static string TrimKeys(string option, Faction subject, ref int disposition)
        {
            if (option.NullOrEmpty() || option == "")
            {
                Log.Error("History option is null.");
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
    };
}
