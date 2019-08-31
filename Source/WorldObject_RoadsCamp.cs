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
            Pawn bestDiplomant = BestCaravanPawnUtility.FindBestNegotiator(caravan);
            Thing silver = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Silver"));
            silver.stackCount = (int)silverCurve.Evaluate(Math.Min(Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal / 2, 10000) / Utilities.FactionsWar().GetByFaction(Faction).resources);

            string text = "RoadsCampRequest".Translate(caravan.Name, Faction);
            DiaNode nodeRoot = new DiaNode(text);
            nodeRoot.options.Add(new DiaOption("RoadsCampRequest_Attack".Translate())
            {

                action = (Action)(() =>
                {
                    Faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Hostile,true, "RoadsCampRequest_AttackReason".Translate(Faction));
                    Utilities.FactionsWar().GetByFaction(Faction).resources -= FE_WorldComp_FactionsWar.MEDIUM_EVENT_RESOURCE_VALUE;
                    Find.WorldObjects.Remove(this);
                }),
                link = new DiaNode(TranslatorFormattedStringExtensions.Translate("RoadsCampRequestAttack", caravan, Faction.leader))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            DiaOption bribe= new DiaOption("RoadsCampRequest_Bribe".Translate(silver.stackCount))
            {
                action = (Action)(() =>
                {
                    Faction.TryAffectGoodwillWith(Faction.OfPlayer, -20);
                    caravan.GiveSoldThingToPlayer(silver, silver.stackCount, bestDiplomant);
                    this.extorted = true;
                }),
                link = new DiaNode(TranslatorFormattedStringExtensions.Translate("RoadsCampRequestBribe",silver.stackCount, caravan))
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
            string title = "RoadsCampRequestTitle".Translate(this.Faction);
            Find.WindowStack.Add((Window)new Dialog_NodeTreeWithFactionInfo(nodeRoot, this.Faction, true, true, title));
            Find.Archive.Add((IArchivable)new ArchivedDialog(nodeRoot.text, title, this.Faction));
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref extorted, "extorted");
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            return CaravanArrivalAction_RoadCamp.GetFloatMenuOptions(caravan,this);
        }
    }
}
