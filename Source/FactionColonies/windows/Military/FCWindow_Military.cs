using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FCWindow_Military : Window
    {
        private readonly MilitaryWindow militaryWindow;
        private readonly string title;

        public override Vector2 InitialSize => new Vector2(838f, 600f);

        public FCWindow_Military(MilitaryWindow militaryWindow, string title)
        {
            this.militaryWindow = militaryWindow;
            this.title = title;

            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
        }

        public override void PostClose()
        {
            base.PostClose();
            FactionCache.FactionComp?.militaryCustomizationUtil?.checkMilitaryUtilForErrors();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(5f, 5f, inRect.width - 10f, 35f), title);

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;

            militaryWindow.DrawTab(inRect);

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        public void SetActive(IExposable selecting)
        {
            militaryWindow.Select(selecting);
        }

        public MilitaryWindow GetMilitaryWindow() => militaryWindow;
    }
}
