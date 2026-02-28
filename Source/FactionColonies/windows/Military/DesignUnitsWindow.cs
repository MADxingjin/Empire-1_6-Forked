using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class DesignUnitsWindow : MilitaryWindow
    {
        private readonly MilitaryCustomizationUtil util;
        private readonly FactionFC faction;
        private MilUnitFC selectedUnit;

        /// <summary>
        /// Describes an apparel equipment slot for the unit designer UI.
        /// </summary>
        private struct ApparelSlotDef
        {
            public Rect rect;
            public ApparelLayerDef layer;
            public BodyPartGroupDef bodyPart; // null = no body part filter
            public string labelKey;

            public bool ThingFitsSlot(ThingDef thing)
            {
                if (!thing.IsApparel) return false;
                if (!thing.apparel.layers.Contains(layer)) return false;
                if (bodyPart != null && !thing.apparel.bodyPartGroups.Contains(bodyPart)) return false;
                if (!thing.apparel.PawnCanWear(Gender.None, DevelopmentalStage.Adult)) return false;
                return CraftUtil.canCraftItem(thing);
            }

            public bool ApparelInSlot(ThingDef def)
            {
                return MilUnitFC.MatchesSlot(def, layer, bodyPart);
            }
        }

        // Populated each frame in DrawTab (rects depend on layout constants)
        private ApparelSlotDef[] apparelSlots;

        public DesignUnitsWindow(MilitaryCustomizationUtil util, FactionFC faction)
        {
            this.util = util;
            this.faction = faction;

            selectedText = "Select A Unit";

            util.checkMilitaryUtilForErrors();
        }

        public override void Select(IExposable selecting)
        {
            MilUnitFC unit = (MilUnitFC)selecting;
            selectedUnit = unit;
            selectedText = unit.name;
        }

        public override void DrawTab(Rect rect)
        {
            // --- Layout Rects ---
            Rect SelectionBar = new Rect(5, 45, 200, 30);
            Rect createUnitButton = new Rect(5, SelectionBar.y + SelectionBar.height + 10, 200, 30);
            Rect importButton = new Rect(5, createUnitButton.y + createUnitButton.height + 10, 200, 30);
            Rect nameTextField = new Rect(5, importButton.y + importButton.height + 10, 250, 30);
            Rect isCivilian = new Rect(5, nameTextField.y + nameTextField.height + 10, 100, 30);
            Rect isTrader = new Rect(isCivilian.x, isCivilian.y + isCivilian.height + 5, isCivilian.width,
                isCivilian.height);

            Rect unitIcon = new Rect(560, 235, 120, 120);
            Rect animalIcon = new Rect(560, 335, 120, 120);

            Rect ApparelHead = new Rect(600, 140, 50, 50);
            Rect ApparelTorsoSkin = new Rect(700, 170, 50, 50);
            Rect ApparelBelt = new Rect(700, 240, 50, 50);
            Rect ApparelLegs = new Rect(700, 310, 50, 50);

            Rect AnimalCompanion = new Rect(500, 160, 50, 50);
            Rect ApparelTorsoShell = new Rect(500, 230, 50, 50);
            Rect ApparelTorsoMiddle = new Rect(500, 310, 50, 50);
            Rect EquipmentWeapon = new Rect(440, 230, 50, 50);

            Rect ApparelWornItems = new Rect(440, 385, 330, 175);
            Rect EquipmentTotalCost = new Rect(450, 50, 350, 40);

            Rect ResetButton = new Rect(700, 50, 100, 30);
            Rect DeleteButton = new Rect(ResetButton.x, ResetButton.y + ResetButton.height + 5,
                ResetButton.width, ResetButton.height);
            Rect SavePawn = new Rect(DeleteButton.x, DeleteButton.y + DeleteButton.height + 5,
                DeleteButton.width, DeleteButton.height);
            Rect ChangeRace = new Rect(325, ResetButton.y, SavePawn.width, SavePawn.height);
            Rect ChangeXeno = new Rect(575, DeleteButton.y, SavePawn.width, SavePawn.height);
            Rect RollNewPawn = new Rect(325, ResetButton.y + SavePawn.height + 5, SavePawn.width,
                SavePawn.height);

            // Build apparel slot descriptors
            apparelSlots = new[]
            {
                new ApparelSlotDef { rect = ApparelHead, layer = ApparelLayerDefOf.Overhead, bodyPart = null, labelKey = "fcLabelHead" },
                new ApparelSlotDef { rect = ApparelTorsoShell, layer = ApparelLayerDefOf.Shell, bodyPart = BodyPartGroupDefOf.Torso, labelKey = "fcLabelOver" },
                new ApparelSlotDef { rect = ApparelTorsoMiddle, layer = ApparelLayerDefOf.Middle, bodyPart = BodyPartGroupDefOf.Torso, labelKey = "fcLabelChest" },
                new ApparelSlotDef { rect = ApparelTorsoSkin, layer = ApparelLayerDefOf.OnSkin, bodyPart = BodyPartGroupDefOf.Torso, labelKey = "fcLabelShirt" },
                new ApparelSlotDef { rect = ApparelLegs, layer = ApparelLayerDefOf.OnSkin, bodyPart = BodyPartGroupDefOf.Legs, labelKey = "fcLabelPants" },
                new ApparelSlotDef { rect = ApparelBelt, layer = ApparelLayerDefOf.Belt, bodyPart = null, labelKey = "fcLabelBelt" },
            };

            // --- Create New Unit Button ---
            if (Widgets.ButtonText(createUnitButton, "FCCreateNewUnit".Translate()))
            {
                MilUnitFC newUnit = new MilUnitFC(false)
                {
                    name = $"New Unit {util.units.Count + 1}"
                };
                selectedText = newUnit.name;
                selectedUnit = newUnit;
                util.units.Add(newUnit);
            }

            // --- Unit Selection Dropdown ---
            if (Widgets.CustomButtonText(ref SelectionBar, selectedText, Color.gray, Color.white, Color.black) && util.units.Count > 0)
            {
                List<FloatMenuOption> Units = new List<FloatMenuOption>();

                foreach (MilUnitFC unit in util.units)
                {
                    void action()
                    {
                        selectedText = unit.name;
                        selectedUnit = unit;
                    }

                    // Prevent units being modified when their squads are deployed
                    FactionFC factionFC = FactionCache.FactionComp;
                    List<MilSquadFC> squadsContainingUnit = factionFC?.militaryCustomizationUtil?.squads
                        .Where(squad => squad?.units != null && squad.units.Contains(unit)).ToList();
                    List<WorldSettlementFC> settlementsContainingSquad = factionFC?.settlements
                        ?.FindAll(settlement => settlement?.MilitaryComp?.militarySquad?.outfit != null &&
                            squadsContainingUnit.Any(squad => settlement.MilitaryComp.militarySquad.outfit == squad));

                    if ((settlementsContainingSquad?.Count ?? 0) > 0)
                    {
                        if (settlementsContainingSquad.Any(settlement => settlement.MilitaryComp.militarySquad.isDeployed))
                        {
                            Units.Add(new FloatMenuOption(unit.name, delegate { Messages.Message("CantBeModified".Translate(unit.name, "ReasonDeployed".Translate()), MessageTypeDefOf.NeutralEvent, false); }));
                            continue;
                        }
                        else if (settlementsContainingSquad.Any(settlement => settlement.MilitaryComp.isUnderAttack && settlementsContainingSquad.Contains(settlement.MilitaryComp.defenderForce.homeSettlement)))
                        {
                            Units.Add(new FloatMenuOption(unit.name, delegate { Messages.Message("CantBeModified".Translate(unit.name, "ReasonDefending".Translate()), MessageTypeDefOf.NeutralEvent, false); }));
                            continue;
                        }
                    }

                    if (unit.HasWeapon)
                    {
                        Units.Add(new FloatMenuOption(unit.name, action, unit.weapons[0].thing));
                    }
                    else
                    {
                        Units.Add(new FloatMenuOption(unit.name, action));
                    }
                }

                FloatMenu selection = new Searchable_FloatMenu(Units);
                Find.WindowStack.Add(selection);
            }

            if (Widgets.ButtonText(importButton, "importUnit".Translate()))
            {
                Find.WindowStack.Add(new Dialog_ManageUnitExportsFC(
                    FactionColoniesMilitary.SavedUnits.ToList()));
            }

            // --- Worn Items Section ---
            Widgets.DrawMenuSection(ApparelWornItems);

            // Save and set text style
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;

            // Draw slot labels and backgrounds
            foreach (ApparelSlotDef slot in apparelSlots)
            {
                Widgets.Label(new Rect(new Vector2(slot.rect.x, slot.rect.y - 15), slot.rect.size), slot.labelKey.Translate());
                Widgets.DrawMenuSection(slot.rect);
            }

            Widgets.Label(new Rect(new Vector2(EquipmentWeapon.x, EquipmentWeapon.y - 15), EquipmentWeapon.size), "fcLabelWeapon".Translate());
            Widgets.DrawMenuSection(EquipmentWeapon);
            Widgets.Label(new Rect(new Vector2(AnimalCompanion.x, AnimalCompanion.y - 15), AnimalCompanion.size), "fcLabelAnimal".Translate());
            Widgets.DrawMenuSection(AnimalCompanion);

            // Restore text style
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;

            // --- Unit Selected Content ---
            if (selectedUnit == null) return;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;

            if (Widgets.ButtonText(ResetButton, "resetUnitToDefaultButton".Translate()))
            {
                selectedUnit.ClearAllEquipment();
            }

            if (Widgets.ButtonText(DeleteButton, "deleteUnitButton".Translate()))
            {
                selectedUnit.removeUnit();
                util.checkMilitaryUtilForErrors();
                selectedUnit = null;
                selectedText = "selectAUnitButton".Translate();

                Text.Font = fontBefore;
                Text.Anchor = anchorBefore;
                return;
            }

            if (Widgets.ButtonText(RollNewPawn, "rollANewUnitButton".Translate()))
            {
                selectedUnit.RerollPreviewPawn();
            }

            if (Widgets.ButtonText(ChangeRace, "changeUnitRaceButton".Translate()))
            {
                List<string> races = new List<string>();
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                foreach (PawnKindDef def in FactionCache.AllPawnKindDefs.Where(def => def.IsHumanLikeRace() && !races.Contains(def.race.label) && faction.raceFilter.Allows(def.race)))
                {
                    if (def.race == ThingDefOf.Human && def.LabelCap != "Colonist") continue;
                    races.Add(def.race.label);

                    string optionStr = def.race.label.CapitalizeFirst() + " - Cost: " + Math.Floor(def.race.BaseMarketValue * FCSettings.militaryRaceCostMultiplier);
                    options.Add(new FloatMenuOption(optionStr, delegate
                    {
                        selectedUnit.pawnKind = def;
                        selectedUnit.RerollPreviewPawn();
                    }));
                }

                if (!options.Any())
                {
                    options.Add(new FloatMenuOption("changeUnitRaceNoRaces".Translate(), null));
                }

                options.Sort(CompareUtil.CompareFloatMenuOption);
                FloatMenu menu = new Searchable_FloatMenu(options);
                Find.WindowStack.Add(menu);
            }

            if (Widgets.ButtonText(ChangeXeno, "changeUnitXenoButton".Translate()))
            {
                List<string> xeno = new List<string>();
                List<FloatMenuOption> options1 = new List<FloatMenuOption>();

                foreach (XenotypeDef def in DefDatabase<XenotypeDef>.AllDefsListForReading.Where(def => !xeno.Contains(def.label)))
                {
                    if (def == XenotypeDefOf.Baseliner && def.LabelCap != "Colonist") continue;
                    xeno.Add(def.label);

                    string optionStr = def.label.CapitalizeFirst() + " - Cost: " + 0;
                    options1.Add(new FloatMenuOption(optionStr, delegate
                    {
                        selectedUnit.xenotype = def;
                        selectedUnit.RerollPreviewPawn();
                    }));
                }
                if (!options1.Any())
                {
                    options1.Add(new FloatMenuOption("changeUnitXenoNoXenos".Translate(), null));
                }

                options1.Sort(CompareUtil.CompareFloatMenuOption);
                FloatMenu menu = new Searchable_FloatMenu(options1);
                Find.WindowStack.Add(menu);
            }

            if (Widgets.ButtonText(SavePawn, "exportUnitButton".Translate()))
            {
                FactionColoniesMilitary.SaveUnit(new SavedUnitFC(selectedUnit));
                Messages.Message("ExportUnit".Translate(), MessageTypeDefOf.TaskCompletion);
            }

            // Unit Name
            selectedUnit.name = Widgets.TextField(nameTextField, selectedUnit.name);

            selectedUnit.setTrader(selectedUnit.isTrader);
            selectedUnit.setCivilian(selectedUnit.isCivilian);

            // Restore text style
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;

            // Draw Pawn Preview
            Pawn preview = selectedUnit.PreviewPawn;
            if (preview != null)
            {
                Widgets.ThingIcon(unitIcon, preview);
            }

            // --- Animal Companion Slot ---
            if (Widgets.ButtonInvisible(AnimalCompanion))
            {
                List<FloatMenuOption> list = (from animal in FactionCache.AllAnimalKindDefs
                    select new FloatMenuOption(animal.LabelCap + " - Cost: " +
                        Math.Floor(animal.race.BaseMarketValue * FCSettings.militaryAnimalCostMultiplier),
                        delegate { selectedUnit.animal = animal; selectedUnit.changeTick(); },
                        animal.race.uiIcon, Color.white)).ToList();

                list.Sort(CompareUtil.CompareFloatMenuOption);

                list.Insert(0, new FloatMenuOption("unitActionUnequipThing".Translate(), delegate
                {
                    selectedUnit.animal = null;
                    selectedUnit.changeTick();
                }));
                FloatMenu menu = new Searchable_FloatMenu(list);
                Find.WindowStack.Add(menu);
            }

            // --- Weapon Slot ---
            if (Widgets.ButtonInvisible(EquipmentWeapon))
            {
                List<ThingDef> weaponDefs = DefDatabase<ThingDef>.AllDefs
                    .Where(t => t.IsWeapon && t.BaseMarketValue != 0 && CraftUtil.canCraftItem(t))
                    .OrderBy(t => t.label)
                    .ToList();

                SavedThing? currentWeapon = selectedUnit.HasWeapon ? selectedUnit.weapons[0] : (SavedThing?)null;
                Find.WindowStack.Add(new FCWindow_ItemStuffPicker(
                    weaponDefs,
                    onConfirm: (item, stuff) => selectedUnit.SetWeapon(item, stuff),
                    onUnequip: () => selectedUnit.ClearWeapon(),
                    titleKey: "fcPickWeapon",
                    initialItem: currentWeapon?.thing,
                    initialStuff: currentWeapon?.stuff
                ));
            }

            // --- Apparel Slots (unified handler) ---
            foreach (ApparelSlotDef slot in apparelSlots)
            {
                HandleApparelSlot(slot, selectedUnit);
            }

            // --- Worn Items List ---
            int i = 0;
            foreach (SavedThing item in selectedUnit.apparel)
            {
                if (item.thing == null) continue;
                Rect tmp = new Rect(ApparelWornItems.x, ApparelWornItems.y + i * 25, ApparelWornItems.width, 25);
                i++;

                string label = item.stuff != null
                    ? item.thing.LabelCap + " (" + item.stuff.LabelCap + ") Cost: " + item.MarketValue
                    : item.thing.LabelCap + " Cost: " + item.MarketValue;
                if (Widgets.CustomButtonText(ref tmp, label, Color.white, Color.black, Color.black))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(item.thing, item.stuff));
                }
            }

            foreach (SavedThing w in selectedUnit.weapons)
            {
                if (w.thing == null) continue;
                Rect tmp = new Rect(ApparelWornItems.x, ApparelWornItems.y + i * 25, ApparelWornItems.width, 25);
                i++;
                string label = w.stuff != null
                    ? w.thing.LabelCap + " (" + w.stuff.LabelCap + ") Cost: " + w.MarketValue
                    : w.thing.LabelCap + " Cost: " + w.MarketValue;
                if (Widgets.CustomButtonText(ref tmp, label, Color.white, Color.black, Color.black))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(w.thing, w.stuff));
                }
            }

            // Animal icon
            if (selectedUnit.animal != null)
            {
                Widgets.ButtonImage(AnimalCompanion, selectedUnit.animal.race.uiIcon);
            }

            // Draw equipped icons in slots
            foreach (ApparelSlotDef slot in apparelSlots)
            {
                SavedThing? worn = selectedUnit.apparel
                    .Cast<SavedThing?>()
                    .FirstOrDefault(a => slot.ApparelInSlot(a.Value.thing));
                if (worn.HasValue && worn.Value.thing != null)
                {
                    Widgets.ButtonImage(slot.rect, worn.Value.thing.uiIcon);
                }
            }

            if (selectedUnit.HasWeapon)
            {
                Widgets.ButtonImage(EquipmentWeapon, selectedUnit.weapons[0].thing.uiIcon);
            }

            float totalCost = (float)selectedUnit.getTotalCost;
            Widgets.Label(EquipmentTotalCost, "totalEquipmentCostLabel".Translate() + totalCost);
        }

        // --- Helper Methods ---

        /// <summary>
        /// Handles the click interaction for a single apparel slot.
        /// Opens an item+stuff picker window for matching apparel.
        /// </summary>
        private void HandleApparelSlot(ApparelSlotDef slot, MilUnitFC unit)
        {
            if (!Widgets.ButtonInvisible(slot.rect)) return;

            List<ThingDef> apparelDefs = DefDatabase<ThingDef>.AllDefs
                .Where(t => slot.ThingFitsSlot(t))
                .OrderBy(t => t.label)
                .ToList();

            SavedThing? currentApparel = unit.apparel
                .Cast<SavedThing?>()
                .FirstOrDefault(a => slot.ApparelInSlot(a.Value.thing));

            Find.WindowStack.Add(new FCWindow_ItemStuffPicker(
                apparelDefs,
                onConfirm: (item, stuff) => unit.SetApparel(item, stuff),
                onUnequip: () => unit.RemoveApparel(slot.layer, slot.bodyPart),
                titleKey: "fcPickApparel",
                initialItem: currentApparel?.thing,
                initialStuff: currentApparel?.stuff
            ));
        }
    }
}
