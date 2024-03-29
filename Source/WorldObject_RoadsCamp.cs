﻿using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{
    class WorldObject_RoadsCamp : WorldObject
    {
        private bool extorted = false;

        private readonly SimpleCurve silverCurve = new SimpleCurve()
        {
            {
                new CurvePoint(0.1f, 100f),
                true
            },
            {
                new CurvePoint(0.25f, 150f),
                true
            },
            {
                new CurvePoint(0.5f, 250f),
                true
            },
            {
                new CurvePoint(0.75f, 350f),
                true
            },
            {
                  new CurvePoint(1f, 500f),
               true
            }
        };
        private Material cachedMat;

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
            Thing silver = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Silver"));
            silver.stackCount = (int)silverCurve.Evaluate(Math.Min(Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal / 2, 10000) / Utilities.FactionsWar().GetByFaction(Faction).resources);

            DiaNode nodeRoot = new DiaNode("RoadsCampRequest".Translate(caravan.Name, Faction));
            nodeRoot.options.Add(new DiaOption("RoadsCampRequest_Attack".Translate())
            {

                action = () =>
                {
                    Faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Hostile, true, "RoadsCampRequest_AttackReason".Translate(Faction));
                    Utilities.FactionsWar().GetByFaction(Faction).resources -= FE_WorldComp_FactionsWar.MEDIUM_EVENT_RESOURCE_VALUE;
                    Find.WorldObjects.Remove(this);
                },
                link = new DiaNode(TranslatorFormattedStringExtensions.Translate("RoadsCampRequestAttack", caravan, Faction.leader))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            DiaOption bribe= new DiaOption("RoadsCampRequest_Bribe".Translate(silver.stackCount))
            {
                action = () =>
                {
                    Faction.TryAffectGoodwillWith(Faction.OfPlayer, -20);
                    caravan.GiveSoldThingToPlayer(silver, silver.stackCount, BestCaravanPawnUtility.FindBestNegotiator(caravan));
                    extorted = true;
                },
                link = new DiaNode(TranslatorFormattedStringExtensions.Translate("RoadsCampRequestBribe", silver.stackCount, caravan))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            };
            if(extorted)
            {
                bribe.Disable("RoadsCampRequestBribe_Disabled".Translate());
            }
            nodeRoot.options.Add(bribe);
            nodeRoot.options.Add(new DiaOption("RoadsCampRequest_Leave".Translate())
            {

                resolveTree = true
            });
            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(nodeRoot, Faction, true, true, "RoadsCampRequestTitle".Translate(Faction)));
            Find.Archive.Add(new ArchivedDialog(nodeRoot.text, "RoadsCampRequestTitle".Translate(Faction), Faction));
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref extorted, "extorted");
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan) => CaravanArrivalAction_RoadCamp.GetFloatMenuOptions(caravan, this);
    }
}
