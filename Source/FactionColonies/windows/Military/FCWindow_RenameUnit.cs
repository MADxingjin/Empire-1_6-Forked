using UnityEngine;
using Verse;
using RimWorld;

namespace FactionColonies
{
    public class FCWindow_RenameUnit : Window
    {
        private readonly MilUnitFC unit;
        private string curName;
        private bool focusedField;

        public override Vector2 InitialSize => new Vector2(300f, 175f);

        public FCWindow_RenameUnit(MilUnitFC unit)
        {
            this.unit = unit;
            curName = unit.name;
            doCloseX = true;
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(0, 0, inRect.width, 35f), "FCRenameUnit".Translate());

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.SetNextControlName("RenameField");
            Rect fieldRect = new Rect(0, 45f, inRect.width, 35f);
            string text = Widgets.TextField(fieldRect, curName);
            if (text.Length <= 28) curName = text;

            if (!focusedField)
            {
                UI.FocusControl("RenameField", this);
                focusedField = true;
            }

            bool enterPressed = Event.current.type == EventType.KeyDown
                && (Event.current.keyCode == KeyCode.Return
                    || Event.current.keyCode == KeyCode.KeypadEnter);
            if (enterPressed) Event.current.Use();

            Rect okBtn = new Rect(15f, inRect.height - 45f, inRect.width - 30f, 35f);
            if (Widgets.ButtonText(okBtn, "OK".Translate()) || enterPressed)
            {
                if (curName.Length > 0)
                {
                    unit.name = curName;
                    Close();
                }
                else
                {
                    Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }
    }
}
