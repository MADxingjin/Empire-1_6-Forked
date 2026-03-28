using System;
using FactionColonies.util;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    class FCEventWindow : Window
    {

        public List<FCEvent> events;
        public FactionFC faction;

        public int scroll = 0;
        public int maxScroll;
        public int scrollBoxHeight = 210;

        public int eventHeight = 30;

        //Rect Placements
        Rect eventsBox;
        Rect eventNameBase;
        Rect eventDescBase;
        Rect eventLocationBase;
        Rect eventTimeRemaining;



        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(628f, 278f);
            }
        }


        public FCEventWindow()
        {
            //Window Information
            this.faction = FactionCache.FactionComp;
            this.events = faction.events;

            this.scroll = 0;
            this.maxScroll = (events.Count * eventHeight) - scrollBoxHeight;


            //Window Properties
            this.forcePause = false;
            this.draggable = true;
            this.doCloseX = true;
            this.preventCameraMotion = false;




            //rect for title
            //Rect titleBox = new Rect(0, 0, 300, 60);
            //rect for box outline
            eventsBox = new Rect(0, 30, 590, 212);

            //rect for event name
            eventNameBase = new Rect(0, 0, 250, eventHeight);
            //rect for description
            eventDescBase = new Rect(eventNameBase.width, 0, 100, eventHeight);
            //rect for source button
            eventLocationBase = new Rect(eventDescBase.x + eventDescBase.width, 0, 100, eventHeight);
            //rect time time remaining
            eventTimeRemaining = new Rect(eventLocationBase.x + eventLocationBase.width, 0, 140, eventHeight);
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            this.maxScroll = (events.Count * eventHeight) - scrollBoxHeight;
        }

        public override void DoWindowContents(Rect inRect)
        {
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            //top label
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            //build outline
            Widgets.DrawMenuSection(eventsBox);


            //loop through each event
            //GoTo Here if change
            int i = 0;

            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (FCEvent evt in events)
            {
                i++;
                Rect name = new Rect();
                Rect desc = new Rect();
                Rect location = new Rect();
                Rect time = new Rect();
                Rect highlight = new Rect();


                name = eventNameBase;
                desc = eventDescBase;
                location = eventLocationBase;
                time = eventTimeRemaining;

                name.y = scroll + eventHeight * i;
                desc.y = scroll + eventHeight * i;
                location.y = scroll + eventHeight * i;
                time.y = scroll + eventHeight * i;

                highlight = new Rect(name.x, name.y, time.x + time.width, eventHeight);

                if (i % 2 == 0)
                {
                    Widgets.DrawHighlight(highlight);
                }
                Widgets.Label(name, evt.def.label);
                //
                if (Widgets.ButtonText(desc, "FCDesc".Translate()))
                {
                    if (evt.hasCustomDescription == false)
                    {
                        //If desc button clicked

                        string settlementString = evt.settlementTraitLocations.Join((settlement) => $" {settlement.Name}", "\n");
                        if (!settlementString.NullOrEmpty())
                        {
                            Find.WindowStack.Add(new DescWindowFc($"{evt.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}"));
                        }
                        else
                        {
                            Find.WindowStack.Add(new DescWindowFc(evt.def.desc));
                        }
                    }
                    else
                    {
                        //has custom description
                        Find.WindowStack.Add(new DescWindowFc(evt.customDescription));
                    }
                }
                //
                if (Widgets.ButtonText(location, "Location".Translate().CapitalizeFirst()))
                {
                    if (evt.hasDestination)
                    {
                        Find.WindowStack.Add(new SettlementWindowFc(faction.ReturnSettlementByLocation(evt.location)));
                    }
                    else
                    {
                        if (evt.settlementTraitLocations.Count > 0)
                        {
                            //if event affecting colonies
                            List<FloatMenuOption> list = new List<FloatMenuOption>();
                            foreach (WorldSettlementFC settlement in evt.settlementTraitLocations)
                            {
                                if (settlement != null)
                                {
                                    list.Add(new FloatMenuOption(settlement.Name, delegate { Find.WindowStack.Add(new SettlementWindowFc(settlement)); }));
                                }
                            }
                            if (list.Count == 0) { list.Add(new FloatMenuOption("None".Translate(), null)); }
                            Find.WindowStack.Add(new FloatMenu(list));

                        }
                        else
                        {
                            if (evt.def == FCEventDefOf.taxColony && evt.source != -1)
                            {
                                Find.WindowStack.Add(new SettlementWindowFc(faction.ReturnSettlementByLocation(evt.source)));
                            }
                        }
                    }
                }
                string timeStr;
                if (evt.HasVariableDuration)
                {
                    int now = Find.TickManager.TicksGame;
                    if (now < evt.timeMinTrigger)
                        timeStr = Math.Max(evt.timeMinTrigger - now, 0).ToTimeString();
                    else
                        timeStr = "FCEndsWithin".Translate(Math.Max(evt.timeMaxTrigger - now, 0).ToTimeString());
                }
                else
                {
                    timeStr = (evt.timeTillTrigger - Find.TickManager.TicksGame).ToTimeString();
                }
                Widgets.Label(time, timeStr);
            }



            //Top label
            Widgets.ButtonTextSubtle(eventNameBase, "Name".Translate());
            Widgets.ButtonTextSubtle(eventDescBase, "Description".Translate());
            Widgets.ButtonTextSubtle(eventLocationBase, "Source".Translate());
            Widgets.ButtonTextSubtle(eventTimeRemaining, "TimeRemaining".Translate());

            //Menu Outline
            Widgets.DrawBox(eventsBox);


            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;


            if (Event.current.type == EventType.ScrollWheel)
            {

                ScrollWindow(Event.current.delta.y);
            }

        }





        private void ScrollWindow(float num)
        {
            if (scroll - num * 5 < -1 * maxScroll)
            {
                scroll = -1 * maxScroll;
            }
            else if (scroll - num * 5 > 0)
            {
                scroll = 0;
            }
            else
            {
                scroll -= (int)Event.current.delta.y * 5;
            }
            Event.current.Use();
        }

    }
}
