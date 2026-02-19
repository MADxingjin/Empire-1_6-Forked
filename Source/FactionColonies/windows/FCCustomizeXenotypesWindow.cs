using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FCCustomizeXenotypesWindow : Window
    {
        FactionFC faction;
        List<XenotypeDef> allXenotypes;
        List<CustomXenotype> allCustomXenotypes;
        List<ThingDef> allRaces;
        XenotypeFilter filter;
        private List<string> weightBufXenos = new List<string>();
        private List<string> weightBufCustoms = new List<string>();
        private List<string> weightBufRaces = new List<string>();
        public override Vector2 InitialSize => new Vector2(400f, 500f);

        private Vector2 xenoScrollBar = new Vector2();
        private Vector2 raceScrollBar = new Vector2();

        private const int margin = 5;
        private const int smallMargin = 3;
        private const int rowHeight = 23;
        private const int bigRowHeight = 26;
        private const int scrollSpacing = 16;

        public FCCustomizeXenotypesWindow()
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            resizeable = true;
            doCloseButton = true;
        }
        public override void PreOpen()
        {
            base.PreOpen();

            faction = FactionCache.FactionComp;
            if (faction == null)
            {
                LogUtil.Error("Null FactionFC WorldComponent when opening FCCustomizeXenotypesWindow");
                Close();
            }
            allXenotypes = FactionCache.XenotypeDefs;
            allCustomXenotypes = FactionCache.CustomXenotypes;
            allRaces = FactionCache.HumanlikeRaces;
            float width = 400f;
            if (allRaces.Count > 1)
            {
                // increase the width of the window, so that we have the xenotype selection on the left, and race selection on the right
                width *= 2;
            }
            float height = 500f;
            windowRect = new Rect(((float)UI.screenWidth - width) / 2f, ((float)UI.screenHeight - height) / 2f, width, height);

            filter = faction.xenotypeFilter;
            filter.ValidateCustomXenotypes();
            for (int i = 0; i < allXenotypes.Count; i++)
            {
                weightBufXenos.Add("");
            }
            for (int i = 0; i < allCustomXenotypes.Count; ++i)
            {
                weightBufCustoms.Add("");
            }
            for (int i = 0; i < allRaces.Count; ++i)
            {
                weightBufRaces.Add("");
            }
        }
        public override void PostClose()
        {
            base.PostClose();
            filter.CullWeights();
        }

        public override void DoWindowContents(Rect boundingBox)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            string titleText;
            if (allRaces.Count > 1)
            {
                titleText = "XenotypeRaceSelection".Translate();
            }
            else
            {
                titleText = "XenotypeSelection".Translate();
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect header = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, 35f);
            Widgets.Label(header, titleText);
            Widgets.DrawLineHorizontal(header.x, header.yMax, header.width);

            Text.Font = GameFont.Small;
            Rect subHeader = new Rect(boundingBox.x, header.yMax, boundingBox.width, 30f);
            Widgets.Label(subHeader, FactionCache.PlayerColonyFaction.Name);

            float availHeight = boundingBox.yMax - subHeader.yMax - CloseButSize.y - margin;

            if (allRaces.Count > 1)
            {
                Rect xenoBox = new Rect(boundingBox.x, subHeader.yMax, (boundingBox.width - (margin*2)) / 2, availHeight);
                Rect raceBox = new Rect(xenoBox.xMax + (margin * 2), xenoBox.y, xenoBox.width, availHeight);
                Widgets.DrawLineVertical(xenoBox.xMax + margin, xenoBox.y, xenoBox.height);
                DoXenotypeSelection(xenoBox);
                DoRaceSelection(raceBox);
            }
            else
            {
                Rect xenoBox = new Rect(boundingBox.x, subHeader.yMax, boundingBox.width, availHeight);
                DoXenotypeSelection(xenoBox);
            }


            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        //xenotypes and custom xenotypes in the same list on the left
        //races in a list on the right
        //if the total weight in a category is zero, show a red-colored message to the player stating that all options will be reset to 1 on closing the window if they leave it that way
        // Beneath both lists, have one button to "disable all non-baseliner/non-human", and one button to "enable all"
        // Perhaps display a percentage chance next to each option, calculated from (weight/total weight)
        // Have single-increment buttons for the numeric inputs
        private void DoXenotypeSelection(Rect boundingBox)
        {
            float bottomY = boundingBox.yMax;
            if(filter.XenoCompleteWeight == 0)
            {
                Rect errorBox = new Rect(boundingBox.x, boundingBox.yMax - bigRowHeight, boundingBox.width, bigRowHeight);
                Rect errorLabel = new Rect(errorBox.x + smallMargin, errorBox.y+smallMargin, errorBox.width - (smallMargin * 2), errorBox.height - smallMargin);
                TaggedString errorText = "XenotypeWeightError".Translate();
                errorText = errorText.Colorize(Color.red);

                Widgets.DrawHighlight(errorBox);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(errorLabel, errorText);
                UIUtil.TipRegionByText(errorBox, "XenotypeWeightErrorDesc".Translate());

                bottomY -= (errorBox.height + margin);
            }

            Rect enableButton = new Rect(boundingBox.x, bottomY - bigRowHeight, boundingBox.width / 2, bigRowHeight);
            Rect disableButton = new Rect(enableButton.xMax, enableButton.y, enableButton.width, enableButton.height);
            if (Widgets.ButtonText(enableButton, "XenoRaceEnableAll".Translate()))
            {
                filter.ResetToAllXenotypes();
            }
            if (Widgets.ButtonText(disableButton, "XenoDisableNonBaseliner".Translate()))
            {
                filter.ResetToBaselinerXenotypeOnly();
            }
            bottomY -= (enableButton.height + margin);

            float renderHeight = bottomY - boundingBox.y;
            float totalHeight = rowHeight * (allXenotypes.Count + allCustomXenotypes.Count);
            Rect drawBox = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, renderHeight);
            Rect selectedListBox = new Rect(drawBox.x + 2, drawBox.y + 2, drawBox.width - 4, drawBox.height - 4);
            float width;
            if (totalHeight > renderHeight)
            {
                width = selectedListBox.width - scrollSpacing;
            }
            else
            {
                width = selectedListBox.width;
            }
            Rect innerScrollBox = new Rect(selectedListBox.x, selectedListBox.y, width, totalHeight);
            Widgets.DrawMenuSection(selectedListBox);
            Widgets.BeginScrollView(selectedListBox, ref xenoScrollBar, innerScrollBox);

            Text.Anchor = TextAnchor.MiddleCenter;
            for (int i = 0; i < allXenotypes.Count + allCustomXenotypes.Count; i++)
            {
                Rect row = new Rect(innerScrollBox.x, innerScrollBox.y + (i * rowHeight), innerScrollBox.width, rowHeight);
                Rect icon = new Rect(row.x + margin, row.y, rowHeight, rowHeight);
                Rect percentLabel = new Rect(row.xMax - 60f, row.y, 60f, rowHeight);
                Rect inputBox = new Rect(percentLabel.x - 80f, row.y + 2, 80f, rowHeight-4);
                Rect label = new Rect(icon.x + margin, row.y, inputBox.x - icon.xMax, rowHeight);
                if (i % 2 == 0)
                {
                    Widgets.DrawHighlight(row);
                }

                if (i < allXenotypes.Count)
                {
                    XenotypeDef xenotype = allXenotypes[i];
                    Widgets.Label(icon, new GUIContent(xenotype.Icon));
                    Widgets.Label(label, xenotype.LabelCap);
                    Widgets.Label(percentLabel, Math.Round(filter.GetXenotypeChance(xenotype)*100, 2).ToString() + "%");
                    UIUtil.TipRegionByText(label, xenotype.description);

                    float weight = filter.GetXenotypeWeight(xenotype);
                    float oldWeight = weight;
                    string buf = weightBufXenos[i];
                    DoWeightField(inputBox, ref weight, ref buf);
                    weight = Math.Clamp(weight, 0f, float.MaxValue);
                    buf = weight.ToString();
                    if (oldWeight != weight)
                    {
                        filter.AddXenotypeWithWeight(xenotype, weight);
                    }
                    weightBufXenos[i] = buf;
                }
                else
                {
                    int customIndex = i - allXenotypes.Count;
                    CustomXenotype xenotype = allCustomXenotypes[customIndex];
                    Texture2D img = xenotype.IconDef?.Icon;
                    if (img != null)
                        Widgets.Label(icon, new GUIContent(xenotype.IconDef.Icon));

                    Widgets.Label(label, xenotype.name);
                    Widgets.Label(percentLabel, Math.Round(filter.GetCustomXenotypeChance(xenotype)*100, 2).ToString() + "%");

                    float weight = filter.GetCustomXenotypeWeight(xenotype);
                    float oldWeight = weight;
                    string buf = weightBufCustoms[customIndex];
                    DoWeightField(inputBox, ref weight, ref buf);
                    weight = Math.Clamp(weight, 0f, float.MaxValue);
                    buf = weight.ToString();
                    if (oldWeight != weight)
                    {
                        filter.AddCustomXenotypeWithWeight(xenotype, weight);
                    }
                    weightBufCustoms[customIndex] = buf;
                }
            }

            Widgets.EndScrollView();
        }
        private void DoRaceSelection(Rect boundingBox)
        {
            float bottomY = boundingBox.yMax;
            if (filter.RaceTotalWeight == 0)
            {
                Rect errorBox = new Rect(boundingBox.x, boundingBox.yMax - bigRowHeight, boundingBox.width, bigRowHeight);
                Rect errorLabel = new Rect(errorBox.x + smallMargin, errorBox.y, errorBox.width - (smallMargin * 2), errorBox.height);
                TaggedString errorText = "RaceWeightError".Translate();
                errorText = errorText.Colorize(Color.red);

                Widgets.DrawHighlight(errorBox);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(errorLabel, errorText);
                UIUtil.TipRegionByText(errorBox, "RaceWeightErrorDesc".Translate());

                bottomY -= (errorBox.height + margin);
            }
            else if (filter.GetRaceWeight(ThingDefOf.Human) == 0)
            {
                string noticeText = "DisabledHumanWarning".Translate();
                float textHeight = Text.CalcHeight(noticeText, boundingBox.width - (smallMargin * 2));
                Rect noticeBox = new Rect(boundingBox.x, bottomY - textHeight - (smallMargin * 2), boundingBox.width, textHeight + (smallMargin * 2));
                Rect noticeLabel = new Rect(noticeBox.x + smallMargin, noticeBox.y, noticeBox.width - (smallMargin * 2), textHeight);

                Widgets.DrawHighlight(noticeBox);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(noticeLabel, noticeText.Colorize(Color.yellow));

                bottomY -= (noticeBox.height + margin);
            }

            Rect enableButton = new Rect(boundingBox.x, bottomY - bigRowHeight, boundingBox.width / 2, bigRowHeight);
            Rect disableButton = new Rect(enableButton.xMax, enableButton.y, enableButton.width, enableButton.height);
            if (Widgets.ButtonText(enableButton, "XenoRaceEnableAll".Translate()))
            {
                filter.ResetToAllRaces();
            }
            if (Widgets.ButtonText(disableButton, "RaceDisableNonHuman".Translate()))
            {
                filter.ResetToHumanRaceOnly();
            }
            bottomY -= (enableButton.height + margin);

            float renderHeight = bottomY - boundingBox.y;
            float totalHeight = rowHeight * (allRaces.Count);
            Rect drawBox = new Rect(boundingBox.x, boundingBox.y, boundingBox.width, renderHeight);
            Rect selectedListBox = new Rect(drawBox.x + 2, drawBox.y + 2, drawBox.width - 4, drawBox.height - 4);
            float width;
            if (totalHeight > renderHeight)
            {
                width = selectedListBox.width - scrollSpacing;
            }
            else
            {
                width = selectedListBox.width;
            }
            Rect innerScrollBox = new Rect(selectedListBox.x, selectedListBox.y, width, totalHeight);
            Widgets.DrawMenuSection(selectedListBox);
            Widgets.BeginScrollView(selectedListBox, ref raceScrollBar, innerScrollBox);

            Text.Anchor = TextAnchor.MiddleCenter;
            for (int i = 0; i < allRaces.Count; i++)
            {
                Rect row = new Rect(innerScrollBox.x, innerScrollBox.y + (i * rowHeight), innerScrollBox.width, rowHeight);
                Rect icon = new Rect(row.x + margin, row.y, rowHeight, rowHeight);
                Rect percentLabel = new Rect(row.xMax - 60f, row.y, 60f, rowHeight);
                Rect inputBox = new Rect(percentLabel.x - 80f, row.y + 2, 80f, rowHeight - 4);
                Rect label = new Rect(icon.x + margin, row.y, inputBox.x - icon.xMax, rowHeight);
                if (i % 2 == 0)
                {
                    Widgets.DrawHighlight(row);
                }

                ThingDef race = allRaces[i];
                Widgets.Label(icon, new GUIContent(race.uiIconPath));
                Widgets.Label(label, race.LabelCap);
                Widgets.Label(percentLabel, Math.Round(filter.GetRaceChance(race)*100, 2).ToString() + "%");
                UIUtil.TipRegionByText(label, race.description);

                float weight = filter.GetRaceWeight(race);
                float oldWeight = weight;
                string buf = weightBufRaces[i];
                DoWeightField(inputBox, ref weight, ref buf);
                weight = Math.Clamp(weight, 0f, float.MaxValue);
                buf = weight.ToString();
                if (oldWeight != weight)
                {
                    filter.AddRaceWithWeight(race, weight);
                }
                weightBufRaces[i] = buf;
            }


            Widgets.EndScrollView();
        }

        private void DoWeightField(Rect boundingBox, ref float value, ref string buffer, float min = 0, float max = float.MaxValue)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            float buttonSize = boundingBox.height;
            Rect decButton = new Rect(boundingBox.x, boundingBox.y, buttonSize, buttonSize);
            Rect incButton = new Rect(boundingBox.xMax - buttonSize, boundingBox.y, buttonSize, buttonSize);
            Rect fieldBox = new Rect(decButton.xMax, boundingBox.y, incButton.x - decButton.xMax, boundingBox.height);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            if (Widgets.ButtonText(decButton, "<"))
            {
                value = Math.Max(value - 1f, min);
            }
            if (Widgets.ButtonText(incButton, ">"))
            {
                value = Math.Min(value + 1f, max);
            }
            Widgets.TextFieldNumeric(fieldBox, ref value, ref buffer, min, max);

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }
    }
}
