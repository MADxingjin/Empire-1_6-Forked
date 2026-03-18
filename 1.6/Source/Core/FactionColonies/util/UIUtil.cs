using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
    public static class UIUtil
    {
        public static void DrawProgressBar(Rect rect, float progress)
        {
            DrawProgressBarColors(rect, progress, Color.black, Color.cyan);
        }
        public static void DrawProgressBarColors(Rect rect, float progress, Color background, Color bar)
        {
            Rect baseRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            Rect progressRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
            Widgets.DrawBoxSolid(baseRect, background);
            Widgets.DrawBoxSolid(progressRect, bar);
        }

        public static void DrawPawnPortrait(Rect rect, Pawn pawn, float cameraZoom = 1f)
        {
            RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(rect.width, rect.height), Rot4.South, cameraZoom: cameraZoom);
            GUI.DrawTexture(rect, portrait);
        }

        public static bool ButtonFlat(Rect rect, string label, Color? labelColor = null, bool disabled = false, bool highlighted = false)
        {
            bool hovered = !disabled && Mouse.IsOver(rect);
            float normalBg = highlighted ? 0.15f : 0.22f;
            float hoverBg = highlighted ? 0.28f : 0.35f;
            float bg = hovered ? hoverBg : normalBg;
            Widgets.DrawBoxSolid(rect, new Color(bg, bg, bg));

            TextAnchor prevAnchor = Text.Anchor;
            bool prevWordWrap = Text.WordWrap;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.WordWrap = false;
            Color prevColor = GUI.color;
            GUI.color = disabled ? Color.gray : (labelColor ?? Color.white);
            Widgets.Label(rect, label);
            GUI.color = prevColor;
            Text.Anchor = prevAnchor;
            Text.WordWrap = prevWordWrap;

            if (!disabled && Widgets.ButtonInvisible(rect))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                return true;
            }
            return false;
        }

        public static void DrawTabDecoratorHorizontalTop(Rect tab, Rect boundingBox, Color color)
        {
            DrawTabDecoratorHorizontalTop(tab, boundingBox.x, boundingBox.xMax, color);
        }
        public static void DrawTabDecoratorHorizontalTop(Rect tab, float leftx, float rightx, Color color)
        {
            Color origColor = GUI.color;
            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(leftx, tab.yMax, tab.x - leftx);
            Widgets.DrawLineVertical(tab.x, tab.y, tab.height);
            Widgets.DrawLineHorizontal(tab.x, tab.y, tab.width);
            Widgets.DrawLineVertical(tab.xMax, tab.y, tab.height);
            Widgets.DrawLineHorizontal(tab.xMax, tab.yMax, rightx - tab.xMax);
            GUI.color = origColor;
        }
        public static void DrawTabDecoratorVerticalLeft(Rect tab, Rect boundingBox, Color color)
        {
            DrawTabDecoratorVerticalLeft(tab, boundingBox.y, boundingBox.yMax, color);
        }
        public static void DrawTabDecoratorVerticalLeft(Rect tab, float upy, float downy, Color color)
        {
            Color origColor = GUI.color;
            GUI.color = color;
            Widgets.DrawLineVertical(tab.xMax, upy, tab.y - upy);
            Widgets.DrawLineHorizontal(tab.x, tab.y, tab.width);
            Widgets.DrawLineVertical(tab.x, tab.y, tab.height);
            Widgets.DrawLineHorizontal(tab.x, tab.yMax, tab.width);
            Widgets.DrawLineVertical(tab.xMax, tab.yMax, downy - tab.yMax);
            GUI.color = origColor;
        }
    }
}
