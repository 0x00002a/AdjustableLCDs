using Digi;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ModAPI;
using VRage.Utils;

namespace Natomic.AngledLCDs
{
    static class TerminalHelper
    {
        private static List<IMyTerminalControl> controls_ = new List<IMyTerminalControl>();

        public static void RefreshAll()
        {
            foreach(var ctrl in controls_)
            {
                ctrl.UpdateVisual();
            }
        }

        private static void LogInvalidScript()
        {
            Log.Error("GameLogic returned null for AngledLCD", "An installed mod is incompatible with Adjustable LCDs. The only current known incompatability is with https://steamcommunity.com/workshop/filedetails/?id=2217821984");
        }
        public static IMyTerminalAction AddTermAct<T>(string id, string name, Action<AngledLCD<T>> cb)
            where T: IMyFunctionalBlock
        {
            var act = MyAPIGateway.TerminalControls.CreateAction<T>(id);
            act.Name = new StringBuilder(name);
            act.Action = b => cb(b.GameLogic.GetAs<AngledLCD<T>>());
            MyAPIGateway.TerminalControls.AddAction<T>(act);
            return act;
        }
        public static IMyTerminalControlListbox AddTermListSel<T>(string name, string title, string tooltip, Action<AngledLCD<T>, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> setContent, Action<AngledLCD<T>, List<MyTerminalControlListBoxItem>> onSel, int visibleRows, bool multiSel)
            where T: IMyFunctionalBlock
        {
            var box = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(name);
            box.Title = MyStringId.GetOrCompute(title);
            box.Tooltip = MyStringId.GetOrCompute(tooltip);
            box.SupportsMultipleBlocks = false;
            box.VisibleRowsCount = visibleRows;
            box.Multiselect = multiSel;
            box.ListContent = (b, content, sel) =>
            {
                if (b?.GameLogic == null)
                {
                    return;
                }
                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                if (lcd == null)
                {
                    LogInvalidScript();
                } else
                {
                    setContent(lcd, content, sel);
                }
            };
            box.ItemSelected = (b, item) => {
                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                if (lcd == null)
                {
                    LogInvalidScript();
                }
                else
                {
                    onSel(lcd, item);
                }
            };

            MyAPIGateway.TerminalControls.AddControl<T>(box);
            controls_.Add(box);
            return box;
        }
        public static IMyTerminalControlCheckbox AddTermChbox<T>(string name, string title, string tooltip, Action<AngledLCD<T>, bool> set, Func<AngledLCD<T>, bool> get)
            where T: IMyFunctionalBlock
        {
            var box = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, T>(name);
            box.Title = MyStringId.GetOrCompute(title);
            box.Tooltip = MyStringId.GetOrCompute(tooltip);
            box.Getter = b =>
            {
                if (b?.GameLogic == null)
                {
                    return false;
                }

                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                if (lcd == null)
                {
                    LogInvalidScript();
                    return false;
                }
                else
                {
                    return get(lcd);
                }
            };
            box.Setter = (b, val) =>
            {
                if (b?.GameLogic == null)
                {
                    return;
                }

                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                if (lcd == null)
                {
                    LogInvalidScript();
                }
                else
                {
                    set(lcd, val);
                    lcd.SaveData();
                }
            };

            MyAPIGateway.TerminalControls.AddControl<T>(box);
            controls_.Add(box);
            return box;
        }
        public static IMyTerminalControlButton AddTermBtn<T>(string name, string title, string tooltip, Action<AngledLCD<T>> act)
            where T: IMyFunctionalBlock
        {
            var txtbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, T>(name);
            txtbox.Title = MyStringId.GetOrCompute(title);
            txtbox.Tooltip = MyStringId.GetOrCompute(tooltip);
            txtbox.Enabled = b => b.GameLogic.GetAs<AngledLCD<T>>() != null;
            txtbox.Action = b => { 
                var lcd = b?.GameLogic?.GetAs<AngledLCD<T>>();
                if (lcd == null) {
                    return;
                }
                act(lcd);
            };

            MyAPIGateway.TerminalControls.AddControl<T>(txtbox);
            controls_.Add(txtbox);
            return txtbox;
        }
        public static IMyTerminalControlTextbox AddTermTxtbox<T>(string name, string title, string tooltip, Action<AngledLCD<T>, StringBuilder> set, Func<AngledLCD<T>, StringBuilder> get)
            where T: IMyFunctionalBlock
        {
            var txtbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, T>(name);
            txtbox.Title = MyStringId.GetOrCompute(title);
            txtbox.Tooltip = MyStringId.GetOrCompute(tooltip);
            txtbox.Enabled = b => b.GameLogic.GetAs<AngledLCD<T>>() != null;
            txtbox.Getter = b =>
            {
                var lcd = b?.GameLogic?.GetAs<AngledLCD<T>>();
                if (lcd == null)
                {
                    return new StringBuilder();
                }
                return get(lcd);
            };
            txtbox.Setter = (b, val) =>
            {
                var lcd = b?.GameLogic?.GetAs<AngledLCD<T>>();
                if (lcd == null)
                {
                    return;
                }
                set(lcd, val);
            };

            MyAPIGateway.TerminalControls.AddControl<T>(txtbox);
            return txtbox;
        }
        public static IMyTerminalControlSlider AddTermSlider<T>(string name, string title, string tooltip, int lower, int upper, Action<AngledLCD<T>, float> set, Func<AngledLCD<T>, float> get)
            where T: IMyFunctionalBlock
        {
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);
            slider.Title = MyStringId.GetOrCompute(title);
            slider.Tooltip = MyStringId.GetOrCompute(tooltip);
            slider.SetLimits(lower, upper);
            slider.Getter = b =>
            {
                if (b.GameLogic == null)
                {
                    return 0.0f;
                }
                
                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                if (lcd == null)
                {
                    LogInvalidScript();
                    return 0.0f;
                }
                else
                {
                    return get(lcd);
                }
            };
            slider.Setter = (b, val) =>
            {
                if (b.GameLogic == null)
                {
                    return;
                }
                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                if (lcd == null)
                {
                    LogInvalidScript();
                }
                else
                {
                    set(lcd, val);
                    lcd.SaveData();
                }
            };
            slider.Writer = (b, str) =>
            {
                if (b.GameLogic == null)
                {
                    return;
                }
                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                if (lcd == null)
                {
                    LogInvalidScript();
                }
                else
                {
                    str.Append(Math.Round(get(lcd), 2));
                }
            };
            controls_.Add(slider);
            MyAPIGateway.TerminalControls.AddControl<T>(slider);
            return slider;
        }
    }
}
