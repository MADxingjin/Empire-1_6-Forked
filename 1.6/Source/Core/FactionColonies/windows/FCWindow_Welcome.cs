using UnityEngine;
using Verse;
using RimWorld;
using System;

namespace FactionColonies
{
    public class FCWindow_Welcome : Window
    {
        public override Vector2 InitialSize => new Vector2(700f, 650f);

        public FCWindow_Welcome()
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = false;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);

            // Header with Empire banner
            if (TexLoad.empireIcon != null)
            {
                Rect bannerRect = new Rect(inRect.x + (inRect.width - 300f) / 2f, ls.CurHeight, 300f, 100f);
                GUI.DrawTexture(bannerRect, TexLoad.empireIcon);
                ls.Gap(110f);
            }

            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            ls.Label("FCWelcomeTitle".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            ls.Gap(20f);

            // Welcome message (shortened version for window)
            ls.Label("FCWelcomeTitle".Translate());
            ls.Gap(10f);
            ls.Label("FCWelcomeDescription".Translate());
            ls.Gap(15f);

            // Quick start guide
            Text.Font = GameFont.Small;
            ls.Label("FCWelcomeQuickStart".Translate());
            ls.Label("FCWelcomeStep1".Translate());
            ls.Label("FCWelcomeStep2".Translate());
            ls.Label("FCWelcomeStep3".Translate());
            ls.Label("FCWelcomeStep4".Translate());
            ls.Gap(15f);

            // Links section with icons
            ls.Label("FCWelcomeHelpfulResources".Translate());
            ls.Gap(5f);

            // Discord link with icon
            Rect discordRect = ls.GetRect(30f);
            if (TexLoad.discordIcon != null)
            {
                Rect iconRect = new Rect(discordRect.x, discordRect.y, 25f, 25f);
                GUI.DrawTexture(iconRect, TexLoad.discordIcon);
                Rect textRect = new Rect(discordRect.x + 30f, discordRect.y, discordRect.width - 30f, discordRect.height);
                if (Widgets.ButtonText(textRect, "FCWelcomeDiscord".Translate(), true, true, true))
                {
                    Application.OpenURL("https://discord.gg/JKGNMqnVaB");
                }
            }

            // Steam Workshop link with icon
            Rect steamRect = ls.GetRect(30f);
            if (TexLoad.wikiIcon != null)
            {
                Rect iconRect = new Rect(steamRect.x, steamRect.y, 25f, 25f);
                GUI.DrawTexture(iconRect, TexLoad.wikiIcon);
                Rect textRect = new Rect(steamRect.x + 30f, steamRect.y, steamRect.width - 30f, steamRect.height);
                if (Widgets.ButtonText(textRect, "FCWelcomeGuide".Translate(), true, true, true))
                {
                    Application.OpenURL("https://docs.google.com/document/d/1_b2spgBlr7oYszDlt3vfB5tKCiDfzUXq2xXn69UmXGY/edit?usp=sharing");
                }
            }

            // GitHub link with icon
            Rect githubRect = ls.GetRect(30f);
            if (TexLoad.githubIcon != null)
            {
                Rect iconRect = new Rect(githubRect.x, githubRect.y, 25f, 25f);
                GUI.DrawTexture(iconRect, TexLoad.githubIcon);
                Rect textRect = new Rect(githubRect.x + 30f, githubRect.y, githubRect.width - 30f, githubRect.height);
                if (Widgets.ButtonText(textRect, "FCWelcomeGitHub".Translate(), true, true, true))
                {
                    Application.OpenURL("https://github.com/littlertom/Empire-1_6-Continued");
                }
            }

            ls.Gap(20f);

            // Tip section
            ls.Label("FCWelcomeTip".Translate());
            ls.Gap(20f);

            // Bottom buttons
            Rect buttonRect = ls.GetRect(35f);
            Rect leftButton = new Rect(buttonRect.x, buttonRect.y, (buttonRect.width - 10f) / 2f, buttonRect.height);
            Rect rightButton = new Rect(buttonRect.x + leftButton.width + 10f, buttonRect.y, leftButton.width, buttonRect.height);

            if (Widgets.ButtonText(leftButton, "FCWelcomeBegin".Translate()))
            {
                // Close this welcome window first
                Close();
                
                // Open the Empire main tab properly
                MainButtonDef empireMainButton = DefDatabase<MainButtonDef>.GetNamed("PColonyMainButton");
                if (empireMainButton != null)
                {
                    Find.MainTabsRoot.SetCurrentTab(empireMainButton, true);
                }
            }

            if (Widgets.ButtonText(rightButton, "FCWelcomeClose".Translate()))
            {
                Close();
            }

            ls.End();
        }
    }
}
