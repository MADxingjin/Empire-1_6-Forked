using FactionColonies.util;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FactionCustomizePoliciesWindowFC : Window
    {
        private const float fullwidth = 600f;
        private const float fullheight = 500f;
        private const float margin = 5f;
        private const float smallMargin = 3f;
        public override Vector2 InitialSize => new Vector2(fullwidth, fullheight);

        private FactionFC faction;

        public string desc;
        public string header;

        string alertText = "";

        bool traitsChosen;

        private const float traitButtonSize = 30f;

        // Button rects for the circular policy layout are constant (fixed window size, no resize).
        // Computed once at class load instead of every frame.
        private static readonly Rect[] PolicyButtonRects = BuildPolicyButtonRects();

        private static Rect[] BuildPolicyButtonRects()
        {
            const double arc = (double)2 / 9;
            const float xrot = 60f, yrot = 60f;
            const float xbase = (fullwidth - 18f * 2f) * 0.2f; // inRect.xMax * 0.2f for fixed-size window
            const float ybase = 200f;
            var rects = new Rect[9];
            for (int i = 0; i < 9; i++)
            {
                rects[i] = new Rect(
                    (float)(xbase + xrot * Math.Cos(arc * i * Math.PI)),
                    (float)(ybase + yrot * Math.Sin(arc * i * Math.PI)),
                    traitButtonSize, traitButtonSize);
            }
            return rects;
        }

        List<FCPolicyDef> selectedPolicies = new List<FCPolicyDef>();
        static List<Vector2> policyScrollBars = new List<Vector2>();

        public FactionCustomizePoliciesWindowFC(FactionFC faction)
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            this.faction = faction;
            header = "FCPolicySelection".Translate();

            if (faction.policies.Count != 0)
            {
                foreach (FCPolicy policy in faction.policies)
                {
                    selectedPolicies.Add(policy.def);
                }
            }
            if (faction.policies.Count == FCSettings.maxPolicyCount)
            {
                traitsChosen = true;
            }
            else
            {
                traitsChosen = false;
                faction.policies = new List<FCPolicy>();
            }
            policyScrollBars.Clear();
            for (int i = 0; i < FCSettings.maxPolicyCount; i++)
            {
                policyScrollBars.Add(new Vector2());
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            //setup all the rects
            Rect labelFaction = new Rect(0, 0, 200, 40);

            float headerHeight = labelFaction.yMax + (margin * 4);

            Rect labelPickTrait = new Rect(inRect.x + margin, headerHeight, inRect.width / 2f, 60);

            Rect buttonConfirm = new Rect((inRect.xMax - 200)/2f, inRect.yMax - 50, 200, 30);

            Rect buttonMilitaristic  = PolicyButtonRects[0];
            Rect buttonAuthoritarian = PolicyButtonRects[1];
            Rect buttonIsolationist  = PolicyButtonRects[2];
            Rect buttonFeudal        = PolicyButtonRects[3];
            Rect buttonPacifist      = PolicyButtonRects[4];
            Rect buttonEgalitarian   = PolicyButtonRects[5];
            Rect buttonExpansionist  = PolicyButtonRects[6];
            Rect buttonTechnocrat    = PolicyButtonRects[7];
            Rect buttonSlaver        = PolicyButtonRects[8];

            Rect descBox = new Rect(inRect.xMax / 2f, headerHeight, (inRect.xMax / 2f) - margin, buttonConfirm.y - headerHeight - margin);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;

            Widgets.Label(labelFaction, header);
            Widgets.DrawLineHorizontal(labelFaction.x, labelFaction.yMax + margin, fullwidth - (Margin * 2));

            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(buttonConfirm, "ConfirmChanges".Translate()))
            {
                if (!traitsChosen)
                {
                    foreach (FCPolicyDef policy in selectedPolicies)
                    {
                        if (!faction.policies.Any((FCPolicy p) => p.def == policy))
                        {
                            faction.policies.Add(new FCPolicy(policy));
                        }
                    }
                }

                Find.WindowStack.TryRemove(this);
            }


            if (!traitsChosen)
            {
                if (selectedPolicies.Count < FCSettings.maxPolicyCount)
                {
                    alertText = "FCSelectTraits0".Translate(FCSettings.maxPolicyCount);
                }
                else
                { 
                    alertText = "FCSelectTraits2".Translate();
                }
            }
            else
            {
                alertText = "FCTraitsChosen".Translate();
            }


            Widgets.Label(labelPickTrait, alertText);

            // Militaristic policy
            DrawPolicyButton(buttonMilitaristic, FCPolicyDefOf.militaristic, FCPolicyDefOf.pacifist);

            // Authortarian policy
            DrawPolicyButton(buttonAuthoritarian, FCPolicyDefOf.authoritarian, FCPolicyDefOf.egalitarian);

            // Isolationist policy
            DrawPolicyButton(buttonIsolationist, FCPolicyDefOf.isolationist, FCPolicyDefOf.expansionist);

            // Feudal policy
            DrawPolicyButton(buttonFeudal, FCPolicyDefOf.feudal, FCPolicyDefOf.technocratic);

            // Pacifist policy
            DrawPolicyButton(buttonPacifist, FCPolicyDefOf.pacifist, FCPolicyDefOf.militaristic);

            // Egalitarian policy
            DrawPolicyButton(buttonEgalitarian, FCPolicyDefOf.egalitarian, FCPolicyDefOf.authoritarian);

            // Expansionist policy
            DrawPolicyButton(buttonExpansionist, FCPolicyDefOf.expansionist, FCPolicyDefOf.isolationist);

            // Technocrat policy
            DrawPolicyButton(buttonTechnocrat, FCPolicyDefOf.technocratic, FCPolicyDefOf.feudal);

            // Slaver policy
            DrawPolicyButton(buttonSlaver, FCPolicyDefOf.slaver, FCPolicyDefOf.technocratic);

            DrawPolicyExplanations(descBox);

            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }
        /// <summary>
        /// Handles clicking on a policy's icon. If the policy is selected, then it is deselected. If the policy is not selected, then it is selected.
        /// </summary>
        /// <param name="policy">The policy to handle.</param>
        /// <param name="opposite">The opposite of the policy to handle. If this policy is selected, then the handled policy cannot be selected.</param>
        private void HandlePolicySelection(FCPolicyDef policy, FCPolicyDef opposite = null)
        {
            bool selected = selectedPolicies.Contains(policy);
            if (selectedPolicies.Count < FCSettings.maxPolicyCount || selected)
            {
                if (opposite is null || !selectedPolicies.Contains(opposite))
                {
                    if (selected)
                    {
                        selectedPolicies.Remove(policy);
                    }
                    else
                    {
                        selectedPolicies.Add(policy);
                    }
                }
                else
                {
                    Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
            }
        }
        private void DrawPolicyButton(Rect button, FCPolicyDef policy, FCPolicyDef opposite = null)
        {
            Texture2D icon;
            if (selectedPolicies.Contains(policy))
                icon = policy.IconLight;
            else
                icon = policy.IconDark;

            UIUtil.TipRegionByText(button, returnPolicyText(policy));

            if (Widgets.ButtonImage(button, icon) && !traitsChosen)
            {
                HandlePolicySelection(policy, opposite);
            }
        }
        private void DrawPolicyExplanations(Rect inRect)
        {
            float heightPerTrait = (inRect.height - ((margin * 2) * (FCSettings.maxPolicyCount -  1))) / FCSettings.maxPolicyCount;
            for (int i = 0; i < FCSettings.maxPolicyCount; i++)
            {
                Rect boundBox = new Rect(inRect.x, inRect.y + i*(heightPerTrait + margin), inRect.width, heightPerTrait);
                Rect iconBox = new Rect(boundBox.x, boundBox.y, traitButtonSize, traitButtonSize);
                Rect labelBox = new Rect(iconBox.xMax + margin, iconBox.y, boundBox.width - iconBox.width - margin, iconBox.height);
                Rect labelText = new Rect(labelBox.x + smallMargin, labelBox.y, labelBox.width - (smallMargin * 2), labelBox.height);
                Rect expBox = new Rect(boundBox.x, labelBox.yMax + margin, boundBox.width, boundBox.height - labelBox.height - margin);
                Rect expTextBox = new Rect(expBox.x + margin, expBox.y, expBox.width - (margin*2), expBox.height);

                if (selectedPolicies.Count >= i+1)
                {
                    Widgets.Label(iconBox, new GUIContent(selectedPolicies[i].IconLight));
                    Widgets.DrawHighlight(labelBox);
                    Widgets.Label(labelText, selectedPolicies[i].LabelCap);
                    Widgets.DrawMenuSection(expBox);

                    string desc = returnPolicyDesc(selectedPolicies[i]);
                    float textHeight = Text.CalcHeight(desc.StripTags(), expTextBox.width);
                    if (textHeight > expTextBox.height)
                    {
                        textHeight = Text.CalcHeight(desc.StripTags(), expTextBox.width - 16f);
                        Rect bigBox = new Rect(expTextBox.x, expTextBox.y, expTextBox.width - 16f, textHeight);
                        Vector2 scrollBar = policyScrollBars[i];

                        Widgets.BeginScrollView(expTextBox, ref scrollBar, bigBox);
                        Widgets.Label(bigBox, desc);
                        Widgets.EndScrollView();
                        policyScrollBars[i] = scrollBar;
                    }
                    else
                    {
                        Widgets.Label(expTextBox, desc);
                    }
                }
            }
        }

        string returnPolicyText(FCPolicyDef def)
        {
            return def.LabelCap + "\n\n" + returnPolicyDesc(def);
        }
        string returnPolicyDesc(FCPolicyDef def)
        {
            // We *could* make a validity check here, but honestly, if the def that this funcion receives isn't in the full list, then that's an error that needs to be fixed in code ASAP.
            //   Validation would just eat the time we saved by making this cache.
            return FactionCache.FCPolicyDescs[def];
        }
    }
}