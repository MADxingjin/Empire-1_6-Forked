using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    [StaticConstructorOnStartup]
    public static class TexLoad
    {
        static TexLoad()
        {
            var icons = ContentFinder<Texture2D>.GetAllInFolder("FactionIcons");
            factionIcons = icons.ToList();
            if (factionIcons.NullOrEmpty())
            {
                LogUtil.Error("No faction icons found, will probably result in Empire not working properly.");
            }
            checkerboard = CreateCheckerboard();
            gradientHorizontal = CreateHorizontalGradient();
        }

        public static readonly Texture2D iconTest100 = ContentFinder<Texture2D>.Get("GUI/100x");
        public static readonly Texture2D questionmark = ContentFinder<Texture2D>.Get("GUI/questionmark");
        public static readonly Texture2D buildingLocked = ContentFinder<Texture2D>.Get("GUI/LockedBuildingSlot");
        public static readonly Texture2D refreshIcon = ContentFinder<Texture2D>.Get("GUI/Buttons/Refresh");

        // Tynan made the definitions in Verse internal so we gotta get them here
        public static readonly Texture2D deleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete");

        //test icons
        public static readonly Texture2D iconHappiness = ContentFinder<Texture2D>.Get("GUI/Happiness");
        public static readonly Texture2D iconLoyalty = ContentFinder<Texture2D>.Get("GUI/Loyalty");
        public static readonly Texture2D iconUnrest = ContentFinder<Texture2D>.Get("GUI/Unrest");
        public static readonly Texture2D iconProsperity = ContentFinder<Texture2D>.Get("GUI/Prosperity");
        public static readonly Texture2D iconMilitary = ContentFinder<Texture2D>.Get("GUI/MilitaryLevel");
        public static readonly Texture2D iconCustomize = ContentFinder<Texture2D>.Get("GUI/customizebutton");
        public static readonly Texture2D iconUpgrade = ContentFinder<Texture2D>.Get("UI/Buttons/ReorderUp");

        public static readonly Texture2D iconTrade = ContentFinder<Texture2D>.Get("UI/Commands/Trade");
        public static readonly Texture2D codexLogo = ContentFinder<Texture2D>.Get("UI/Icons/EmpireLogo");


        //Trait Icons
        public static readonly Texture2D traitAuthoritarianLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Authoritarian");
        public static readonly Texture2D traitAuthoritarianDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/AuthoritarianDark");
        public static readonly Texture2D traitEgalitarianLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Egalitarian");
        public static readonly Texture2D traitEgalitarianDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/EgalitarianDark");
        public static readonly Texture2D traitExpansionistLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Expansionist");
        public static readonly Texture2D traitExpansionistDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/ExpansionistDark");
        public static readonly Texture2D traitFeudalLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Feudal");
        public static readonly Texture2D traitFeudalDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/FeudalDark");
        public static readonly Texture2D traitIsolationistLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Isolationist");
        public static readonly Texture2D traitIsolationistDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/IsolationistDark");
        public static readonly Texture2D traitMilitaristicLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Militaristic");
        public static readonly Texture2D traitMilitaristicDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/MilitaristicDark");
        public static readonly Texture2D traitPacifistLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Pacifist");
        public static readonly Texture2D traitPacifistDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/PacifistDark");
        public static readonly Texture2D traitTechnocraticLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Technocratic");
        public static readonly Texture2D traitTechnocraticDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/TechnocraticDark");
        public static readonly Texture2D traitSlaverLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Feudal");
        public static readonly Texture2D traitSlaverDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/FeudalDark");

        //UnitCustomization
        public static readonly Texture2D unitCircle = ContentFinder<Texture2D>.Get("GUI/unitCircle");

        // Patch notes link button textures
        public static readonly Texture2D discordIcon = ContentFinder<Texture2D>.Get("GUI/Buttons/discordlogo");
        public static readonly Texture2D githubIcon = ContentFinder<Texture2D>.Get("GUI/Buttons/githublogo");
        public static readonly Texture2D wikiIcon = ContentFinder<Texture2D>.Get("GUI/Buttons/wikilogo");

        public static List<Texture2D> factionIcons = new List<Texture2D>();
        public static readonly Texture2D checkerboard;
        public static readonly Texture2D gradientHorizontal;

        private static Texture2D CreateHorizontalGradient()
        {
            int width = 256;
            Texture2D tex = new Texture2D(width, 1, TextureFormat.ARGB32, false);
            tex.name = "GradientHorizontalTex";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < width; x++)
            {
                float alpha = 1f - (float)x / (width - 1);
                tex.SetPixel(x, 0, new Color(1f, 1f, 1f, alpha));
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Draws a horizontal gradient that fades from <paramref name="color"/> to transparent (left to right).
        /// Uses the cached gradient texture tinted via GUI.color.
        /// </summary>
        public static void DrawHorizontalGradient(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, gradientHorizontal, ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        private static Texture2D CreateCheckerboard()
        {
            int size = 8;
            int cellSize = 4;
            Color light = new Color(1f, 1f, 1f, 0.15f);
            Color dark = new Color(0f, 0f, 0f, 0.1f);

            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.name = "CheckerboardTex";
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLight = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                    tex.SetPixel(x, y, isLight ? light : dark);
                }
            }

            tex.Apply();
            return tex;
        }

    }
}
