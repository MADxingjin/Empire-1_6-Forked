using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Draws the Edicts tab content in the main faction window.
    /// Three columns (Social, Tax, Military), each showing available edicts
    /// with radio-style selection and upkeep display.
    /// </summary>
    public static class EdictTabDrawer
    {
        private static readonly FCPolicyCategory[] EdictCategories =
        {
            FCPolicyCategory.Social,
            FCPolicyCategory.Tax,
            FCPolicyCategory.Military
        };

        private static Dictionary<FCPolicyCategory, List<FCPolicyDef>> cachedEdictsByCategory;
        private static Vector2 scrollPosition;

        private const float Margin = 5f;
        private const float ColumnGap = 8f;
        private const float HeaderHeight = 30f;
        private const float EdictRowHeight = 80f;
        private const float BottomBarHeight = 35f;
        private const float RadioSize = 24f;
        private const float CategoryPadding = 6f;

        public static void OnTabSwitch()
        {
            scrollPosition = Vector2.zero;
            cachedEdictsByCategory = null;
        }

        private static Dictionary<FCPolicyCategory, List<FCPolicyDef>> GetEdictsByCategory()
        {
            if (cachedEdictsByCategory != null) return cachedEdictsByCategory;

            cachedEdictsByCategory = new Dictionary<FCPolicyCategory, List<FCPolicyDef>>();
            foreach (FCPolicyCategory cat in EdictCategories)
            {
                cachedEdictsByCategory[cat] = DefDatabase<FCPolicyDef>.AllDefs
                    .Where(d => d.category == cat)
                    .ToList();
            }
            return cachedEdictsByCategory;
        }

        private static string GetCategoryLabel(FCPolicyCategory category)
        {
            switch (category)
            {
                case FCPolicyCategory.Social: return "FCEdictCategorySocial".Translate();
                case FCPolicyCategory.Tax: return "FCEdictCategoryTax".Translate();
                case FCPolicyCategory.Military: return "FCEdictCategoryMilitary".Translate();
                default: return category.ToString();
            }
        }

        public static void Draw(Rect rect, FactionFC faction)
        {
            Dictionary<FCPolicyCategory, List<FCPolicyDef>> edictsByCategory = GetEdictsByCategory();

            // Description header
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect descRect = new Rect(rect.x + Margin, rect.y + Margin, rect.width - Margin * 2, 20f);
            Widgets.Label(descRect, "FCEdictsDesc".Translate());

            float topY = descRect.yMax + Margin;
            float bottomBarY = rect.yMax - BottomBarHeight;
            float columnsHeight = bottomBarY - topY - Margin;
            float columnWidth = (rect.width - Margin * 2 - ColumnGap * 2) / 3f;

            // Draw three columns
            for (int i = 0; i < EdictCategories.Length; i++)
            {
                FCPolicyCategory category = EdictCategories[i];
                float colX = rect.x + Margin + i * (columnWidth + ColumnGap);
                Rect colRect = new Rect(colX, topY, columnWidth, columnsHeight);
                DrawColumn(colRect, category, edictsByCategory[category], faction);
            }

            // Bottom bar - total upkeep
            DrawBottomBar(new Rect(rect.x + Margin, bottomBarY, rect.width - Margin * 2, BottomBarHeight), faction);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void DrawColumn(Rect rect, FCPolicyCategory category, List<FCPolicyDef> edicts, FactionFC faction)
        {
            // Column background
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f, 0.5f));
            Widgets.DrawBox(rect);

            bool unlocked = faction.IsEdictCategoryUnlocked(category);
            int requiredLevel;
            FactionFC.EdictCategoryUnlockLevels.TryGetValue(category, out requiredLevel);

            // Header
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, HeaderHeight);
            Widgets.DrawBoxSolid(headerRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, GetCategoryLabel(category));

            float contentY = headerRect.yMax + CategoryPadding;

            if (!unlocked)
            {
                // Locked state
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect lockedRect = new Rect(rect.x + Margin, contentY, rect.width - Margin * 2, 40f);
                GUI.color = Color.gray;
                Widgets.Label(lockedRect, "FCEdictLockedUntilLevel".Translate(requiredLevel));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Active edict status
            FCPolicy activeEdict = faction.GetActiveEdict(category);
            Rect statusRect = new Rect(rect.x + CategoryPadding, contentY, rect.width - CategoryPadding * 2, 20f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            if (activeEdict != null)
            {
                string statusText = "FCEdictActive".Translate() + ": " + activeEdict.def.LabelCap;
                Widgets.Label(statusRect, statusText);

                // Activating indicator
                if (!activeEdict.IsFullyActive)
                {
                    contentY = statusRect.yMax;
                    Rect activatingRect = new Rect(rect.x + CategoryPadding, contentY, rect.width - CategoryPadding * 2, 20f);
                    float daysRemaining = (activeEdict.def.enactDuration - (Find.TickManager.TicksGame - activeEdict.timeEnacted)) / 60000f;
                    GUI.color = Color.yellow;
                    Widgets.Label(activatingRect, "FCEdictActivating".Translate(daysRemaining.ToString("F1")));
                    GUI.color = Color.white;
                    contentY = activatingRect.yMax;
                }
                else
                {
                    contentY = statusRect.yMax;
                }

                // Revoke button
                Rect revokeRect = new Rect(rect.x + CategoryPadding, contentY, rect.width - CategoryPadding * 2, 24f);
                if (Widgets.ButtonText(revokeRect, "FCEdictRevoke".Translate()))
                {
                    faction.RevokeEdict(category);
                }
                contentY = revokeRect.yMax + CategoryPadding;
            }
            else
            {
                Widgets.Label(statusRect, "FCEdictActive".Translate() + ": " + "FCEdictNone".Translate());
                contentY = statusRect.yMax + CategoryPadding;
            }

            // Separator line
            Widgets.DrawLineHorizontal(rect.x + CategoryPadding, contentY, rect.width - CategoryPadding * 2);
            contentY += CategoryPadding;

            // Edict list
            foreach (FCPolicyDef def in edicts)
            {
                if (contentY + EdictRowHeight > rect.yMax) break;

                Rect rowRect = new Rect(rect.x + CategoryPadding, contentY, rect.width - CategoryPadding * 2, EdictRowHeight);
                DrawEdictRow(rowRect, def, faction, activeEdict);
                contentY = rowRect.yMax + 2f;
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static FCPolicyDef GetBlockingPolicy(FCPolicyDef def, FactionFC faction)
        {
            if (def.incompatiblePolicies.NullOrEmpty()) return null;
            foreach (FCPolicyDef blocked in def.incompatiblePolicies)
            {
                if (faction.HasPolicy(blocked) || faction.HasTrait(blocked))
                    return blocked;
            }
            return null;
        }

        private static void DrawEdictRow(Rect rect, FCPolicyDef def, FactionFC faction, FCPolicy activeEdict)
        {
            bool isActive = activeEdict != null && activeEdict.def == def;
            bool meetsLevel = def.factionLevelRequirement <= 0 || faction.factionLevel >= def.factionLevelRequirement;
            FCPolicyDef blocker = GetBlockingPolicy(def, faction);
            bool available = meetsLevel && blocker == null;

            // Row background
            if (isActive)
                Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.3f, 0.1f, 0.4f));
            else if (Mouse.IsOver(rect))
                Widgets.DrawHighlight(rect);

            // Radio button area
            Rect radioRect = new Rect(rect.x, rect.y + (rect.height - RadioSize) / 2f, RadioSize, RadioSize);

            // Label and description
            float textX = radioRect.xMax + Margin;
            float textWidth = rect.width - RadioSize - Margin;

            Rect labelRect = new Rect(textX, rect.y + 2f, textWidth, 20f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            if (!available)
                GUI.color = Color.gray;

            Widgets.Label(labelRect, def.LabelCap);

            // Description
            Rect descRect = new Rect(textX, labelRect.yMax, textWidth, 36f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(descRect, def.desc);

            // Upkeep
            Rect upkeepRect = new Rect(textX, descRect.yMax, textWidth, 18f);
            GUI.color = available ? new Color(1f, 0.85f, 0.4f) : Color.gray;
            Widgets.Label(upkeepRect, "FCEdictUpkeep".Translate(def.upkeepSilver));

            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Radio button drawing and click handling
            if (available)
            {
                bool selected = isActive;
                if (Widgets.RadioButton(radioRect.x, radioRect.y, selected))
                {
                    if (!isActive)
                    {
                        faction.EnactEdict(def);
                        cachedEdictsByCategory = null; // Force refresh
                    }
                }
            }
            else
            {
                // Draw grayed radio
                GUI.color = Color.gray;
                Widgets.RadioButton(radioRect.x, radioRect.y, false);
                GUI.color = Color.white;
            }

            // Tooltip
            string tooltip = def.PolicyText();
            if (!meetsLevel)
                tooltip += "\n\n" + "FCEdictLevelRequired".Translate(def.factionLevelRequirement);
            if (blocker != null)
                tooltip += "\n\n" + "FCEdictIncompatible".Translate(def.LabelCap, blocker.LabelCap);
            TooltipHandler.TipRegion(rect, tooltip);

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawBottomBar(Rect rect, FactionFC faction)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.2f, 0.2f, 0.8f));

            int totalUpkeep = faction.GetEdictUpkeep();

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect upkeepLabelRect = new Rect(rect.x + Margin, rect.y, rect.width - Margin * 2, rect.height);
            Widgets.Label(upkeepLabelRect, "FCEdictTotalUpkeep".Translate(totalUpkeep));
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
