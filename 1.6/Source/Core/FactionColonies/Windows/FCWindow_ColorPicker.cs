using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FCWindow_ColorPicker : Window
    {
        private Color color;
        private Color oldColor;
        private Action<Color> onAccept;
        private string title;

        private bool hsvWheelDragging;
        private string[] textfieldBuffers = new string[6];
        private Color textfieldColorBuffer;
        private string previousFocusedControlName;

        private List<Color> presetColors;

        private const float WheelSize = 140f;
        private const int SwatchSize = 22;
        private const int SwatchPadding = 2;
        private const int SwatchesPerRow = 12;
        private const float SectionGap = 6f;
        private const float LabelHeight = 20f;

        public override Vector2 InitialSize => new Vector2(620f, 520f);

        public FCWindow_ColorPicker(string title, Color initialColor, Action<Color> onAccept)
        {
            this.title = title;
            this.color = initialColor;
            this.oldColor = initialColor;
            this.onAccept = onAccept;

            forcePause = false;
            draggable = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnAccept = false;

            BuildPresetColors();
        }

        private void BuildPresetColors()
        {
            presetColors = new List<Color> { Color.white, Color.black };
            foreach (ColorDef cd in DefDatabase<ColorDef>.AllDefsListForReading)
            {
                if (cd.colorType == ColorType.Ideo)
                    presetColors.Add(cd.color);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            float y = inRect.y;

            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f), title);
            y += 35f;
            Widgets.DrawLineHorizontal(inRect.x, y, inRect.width);
            y += SectionGap;

            // === Top section: HSV wheel (left) + preview/textfields (right) ===
            float topSectionHeight = WheelSize + 10f;

            // HSV Wheel
            Rect wheelRect = new Rect(inRect.x, y, WheelSize, WheelSize);
            Widgets.HSVColorWheel(wheelRect, ref color, ref hsvWheelDragging, null, "fcColorWheel");

            // Right side: preview + textfields
            float rightX = wheelRect.xMax + 20f;
            float rightWidth = inRect.xMax - rightX;

            // Current / Old color preview
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            float previewBoxWidth = 60f;
            float previewRowHeight = 22f;
            float labelWidth = 55f;

            Widgets.Label(new Rect(rightX, y, labelWidth, previewRowHeight), "Current:");
            Widgets.DrawBoxSolidWithOutline(
                new Rect(rightX + labelWidth, y + 1f, previewBoxWidth, previewRowHeight - 2f),
                color, Color.gray);

            y += previewRowHeight + 2f;

            Widgets.Label(new Rect(rightX, y, labelWidth, previewRowHeight), "Old:");
            Widgets.DrawBoxSolidWithOutline(
                new Rect(rightX + labelWidth, y + 1f, previewBoxWidth, previewRowHeight - 2f),
                oldColor, Color.gray);

            y += previewRowHeight + 8f;

            // RGB/HSV textfields
            RectAggregator aggregator = new RectAggregator(
                new Rect(rightX, y, 125f, 0f), 827364);
            Widgets.ColorTextfields(ref aggregator, ref color, ref textfieldBuffers,
                ref textfieldColorBuffer, previousFocusedControlName, "fcColorTextfields",
                Widgets.ColorComponents.All, Widgets.ColorComponents.All);

            if (Event.current.type == EventType.Layout)
            {
                previousFocusedControlName = GUI.GetNameOfFocusedControl();
            }

            y = wheelRect.yMax + SectionGap + 4f;

            // === Ideology Colors ===
            if (ModsConfig.IdeologyActive)
            {
                y = DrawIdeoSection(inRect.x, y, inRect.width);
            }

            // === Preset Colors ===
            y = DrawPresetSection(inRect.x, y, inRect.width);

            // === Saved Colors ===
            y = DrawSavedSection(inRect.x, y, inRect.width);

            // === Bottom buttons ===
            float btnWidth = 120f;
            float btnHeight = 30f;
            float btnY = inRect.yMax - btnHeight;

            if (Widgets.ButtonText(new Rect(inRect.xMax - btnWidth, btnY, btnWidth, btnHeight), "Accept".Translate()))
            {
                onAccept(color);
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.xMax - btnWidth * 2 - 10f, btnY, btnWidth, btnHeight), "Cancel".Translate()))
            {
                Close();
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private float DrawIdeoSection(float x, float y, float width)
        {
            List<Ideo> ideos = null;
            try
            {
                if (Find.IdeoManager != null)
                    ideos = Find.IdeoManager.IdeosListForReading;
            }
            catch
            {
                // IdeoManager not available
            }

            if (ideos == null || ideos.Count == 0)
                return y;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, width, LabelHeight), "fcColorPickerActiveIdeos".Translate());
            y += LabelHeight;

            float startX = x;
            int col = 0;
            foreach (Ideo ideo in ideos)
            {
                Color ideoColor = ideo.Color;
                if (ideoColor == Color.white) continue;

                Rect swatchRect = new Rect(
                    startX + col * (SwatchSize + SwatchPadding),
                    y,
                    SwatchSize, SwatchSize);

                bool isSelected = color.IndistinguishableFrom(ideoColor);
                Color outline = isSelected ? Color.white : new Color(0.4f, 0.4f, 0.4f);
                Widgets.DrawBoxSolidWithOutline(swatchRect, ideoColor, outline);

                if (Mouse.IsOver(swatchRect))
                {
                    Widgets.DrawHighlight(swatchRect);
                }

                TooltipHandler.TipRegion(swatchRect, ideo.name);

                if (Widgets.ButtonInvisible(swatchRect))
                {
                    color = ideoColor;
                }

                col++;
                if (col >= SwatchesPerRow)
                {
                    col = 0;
                    y += SwatchSize + SwatchPadding;
                }
            }

            y += SwatchSize + SwatchPadding + SectionGap;
            return y;
        }

        private float DrawPresetSection(float x, float y, float width)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, width, LabelHeight), "fcColorPickerPresets".Translate());
            y += LabelHeight;

            Rect selectorRect = new Rect(x, y, width, 200f);
            float selectorHeight;
            Widgets.ColorSelector(selectorRect, ref color, presetColors, out selectorHeight);
            y += selectorHeight + SectionGap;
            return y;
        }

        private float DrawSavedSection(float x, float y, float width)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, width, LabelHeight), "fcColorPickerSaved".Translate());
            y += LabelHeight;

            List<Color> saved = FCSettings.savedPickerColors;

            float startX = x;
            int col = 0;
            for (int i = 0; i < saved.Count; i++)
            {
                Color savedColor = saved[i];
                Rect swatchRect = new Rect(
                    startX + col * (SwatchSize + SwatchPadding),
                    y,
                    SwatchSize, SwatchSize);

                bool isSelected = color.IndistinguishableFrom(savedColor);
                Color outline = isSelected ? Color.white : new Color(0.4f, 0.4f, 0.4f);
                Widgets.DrawBoxSolidWithOutline(swatchRect, savedColor, outline);

                if (Mouse.IsOver(swatchRect))
                {
                    Widgets.DrawHighlight(swatchRect);
                }

                if (Widgets.ButtonInvisible(swatchRect))
                {
                    // Left click selects, right click removes
                    if (Event.current.button == 1)
                    {
                        saved.RemoveAt(i);
                        break;
                    }
                    else
                    {
                        color = savedColor;
                    }
                }

                col++;
                if (col >= SwatchesPerRow)
                {
                    col = 0;
                    y += SwatchSize + SwatchPadding;
                }
            }

            if (col > 0)
                y += SwatchSize + SwatchPadding;

            // Save Current button on its own row
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect saveBtn = new Rect(x, y, 90f, SwatchSize);
            if (Widgets.ButtonText(saveBtn, "fcColorPickerSaveCurrent".Translate()))
            {
                if (saved.Count >= FCSettings.MaxSavedPickerColors)
                    saved.RemoveAt(0);
                saved.Add(color);
            }

            y += SwatchSize + SectionGap;
            return y;
        }
    }
}
