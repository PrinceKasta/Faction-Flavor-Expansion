using System;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{

    public class Window_Faction : Window
    {
        private Vector2 scrollPosition;
        private const float factionBoxX = 20;
        private const float factionBoxY = 100;
        private const float factionBoxXMax = 490;
        private const float factionBoxYMax = 455;
        public War war;

        public Window_Faction()
        {
            draggable = true;
            doCloseButton = true;
            forcePause = true;
            doWindowBackground = true;
            scrollPosition = Vector2.zero;
        }

        public Window_Faction(War war)
        {
            draggable = true;
            doCloseButton = true;
            forcePause = true;
            doWindowBackground = true;
            scrollPosition = Vector2.zero;
            this.war = war;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (war == null)
                Close(false);
            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Rect rect2 = inRect.AtZero();
            rect2.height = 36f;
            rect2.xMin += 9f;
            rect2.yMin += 5f;
            var header = new Listing_Standard();
            header.Begin(rect2);
            header.Label("WindowWarOverview".Translate( war.AttackerFaction(),war.DefenderFaction()), 50);
            header.GapLine();
            header.End();

            DevModeSliders(inRect);

            // Defender resource box
            float faction1Resources = Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources / Utilities.FactionsWar().MaxResourcesForFaction(war.DefenderFaction());
            GUI.color = Color.blue;
            Rect faction1Box = new Rect(inRect.x + factionBoxX, inRect.y + factionBoxY, inRect.xMax - factionBoxXMax, inRect.yMax - factionBoxYMax);
            Widgets.DrawBox(faction1Box,5);
            Rect faction1 = new Rect(inRect.x + factionBoxX, inRect.y + factionBoxY + 109f * (1 - faction1Resources), inRect.xMax - factionBoxXMax, inRect.yMax - factionBoxYMax - 109f * (1- faction1Resources));
            Widgets.DrawBoxSolid(faction1, new Color(0.19607843137f, 0.2627450980f, 1f, 1));
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(faction1Box, war.DefenderFaction().Name + "'s Resources: " + Math.Round(faction1Resources * 100) + "%");
            Text.Font = GameFont.Small;

            //defender info

            Rect faction1Info = new Rect(inRect.x + factionBoxX, inRect.y + 220, inRect.xMax - factionBoxXMax, inRect.yMax - factionBoxYMax-50);
            Text.Anchor = TextAnchor.UpperLeft;
            Faction faction = war.DefenderFaction();
            Widgets.Label(faction1Info, faction.GetInfoText());
            
            if (faction != null)
            {
                FactionRelationKind playerRelationKind = faction.PlayerRelationKind;
                GUI.color = playerRelationKind.GetColor();
                Widgets.Label(new Rect(faction1Info.x, faction1Info.y + Text.CalcHeight(faction.GetInfoText(), faction1Info.width) + Text.SpaceBetweenLines, faction1Info.width, 30f), playerRelationKind.GetLabel());
            }
            
            // attacker resource boxes
            float faction2Resources =  Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources / Utilities.FactionsWar().MaxResourcesForFaction(war.AttackerFaction());
            Rect faction2Box = new Rect(inRect.xMax - 230, inRect.y + factionBoxY, 210, inRect.yMax - factionBoxYMax);
            GUI.color = Color.red;
            Widgets.DrawBox(faction2Box,5);
            Rect faction2 = new Rect(inRect.xMax - 230, inRect.y + factionBoxY + 109f * (1 - faction2Resources), 210, inRect.yMax - factionBoxYMax - 109f *(1- faction2Resources));
            Widgets.DrawBoxSolid(faction2, new Color(0.8117647058f, 0f, 0.0588235f, 1));
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(faction2Box, war.AttackerFaction().Name + "'s Resources: "+Math.Round(faction2Resources * 100)+"%");


            //attacker info
            Rect faction1Info2 = new Rect(inRect.xMax - 230, inRect.y + 220, 210, inRect.yMax - factionBoxYMax - 50);
            Text.Anchor = TextAnchor.UpperRight;
            Faction faction3 = war.AttackerFaction();
            Widgets.Label(faction1Info2, faction3.GetInfoText());

            if (faction3 != null)
            {
                FactionRelationKind playerRelationKind = faction3.PlayerRelationKind;
                GUI.color = playerRelationKind.GetColor();
                Widgets.Label(new Rect(faction1Info2.x, faction1Info2.y + Text.CalcHeight(faction.GetInfoText(), faction1Info2.width) + Text.SpaceBetweenLines, faction1Info2.width, 30f), playerRelationKind.GetLabel());
            }

            /*Text box*/
            Text.Font = GameFont.Small;
            Rect textRect = new Rect(inRect);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            textRect.xMin += 5;
            textRect.xMax -= 5;
            textRect.yMin +=300;
            textRect.yMax -= 50;
            Widgets.TextAreaScrollable(textRect, war.warHistory, ref scrollPosition,true);
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.color = Color.white;
            //GUI.EndGroup();
        }

        private void DevModeSliders(Rect inRect)
        {
            if(!Prefs.DevMode)
                return;
            Rect rect = new Rect(inRect.x + factionBoxX, inRect.y + factionBoxY - 75, inRect.xMax - factionBoxXMax, inRect.yMax - factionBoxYMax);
            Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources= Widgets.HorizontalSlider(rect, Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources, 0, Utilities.FactionsWar().MaxResourcesForFaction(war.DefenderFaction()), true, "(DevMode) " + Utilities.FactionsWar().GetByFaction(war.DefenderFaction()).resources);
            Rect rect2 = new Rect(inRect.xMax - 230, inRect.y + factionBoxY - 75, 210, inRect.yMax - factionBoxYMax);
            Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources = Widgets.HorizontalSlider(rect2, Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources, 0, Utilities.FactionsWar().MaxResourcesForFaction(war.AttackerFaction()), true, "(DevMode) " + Utilities.FactionsWar().GetByFaction(war.AttackerFaction()).resources);
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(720f, 600f);
            }
        }
    }
}
