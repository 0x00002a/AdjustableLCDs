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

        private static void LogInvalidScript()
        {
            Log.Error("GameLogic returned null for AngledLCD", "An installed mod is incompatible with Adjustable LCDs. The only current known incompatability is with https://steamcommunity.com/workshop/filedetails/?id=2217821984");
        }
        public static IMyTerminalControlListbox AddTermListSel<T>(string name, string title, string tooltip, Action<AngledLCD<T>, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> setContent, Action<AngledLCD<T>, MyTerminalControlListBoxItem> onSel, int visibleRows)
            where T: IMyFunctionalBlock
        {
            var box = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(name);
            box.Title = MyStringId.GetOrCompute(title);
            box.Tooltip = MyStringId.GetOrCompute(tooltip);
            box.SupportsMultipleBlocks = false;
            box.VisibleRowsCount = visibleRows;
            box.Multiselect = true;
            box.ListContent = (b, content, sel) =>
            {
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
                    onSel(lcd, item[0]);
                }
            };

            MyAPIGateway.TerminalControls.AddControl<T>(box);
            return box;
        }
        public static void AddTermChbox<T>(string name, string title, string tooltip, Action<AngledLCD<T>, bool> set, Func<AngledLCD<T>, bool> get)
            where T: IMyFunctionalBlock
        {
            var box = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, T>(name);
            box.Title = MyStringId.GetOrCompute(title);
            box.Tooltip = MyStringId.GetOrCompute(tooltip);
            box.Getter = b =>
            {
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
        }
        public static void AddTermSlider<T>(string name, string title, string tooltip, int lower, int upper, Action<AngledLCD<T>, float> set, Func<AngledLCD<T>, float> get)
            where T: IMyFunctionalBlock
        {
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);
            slider.Title = MyStringId.GetOrCompute(title);
            slider.Tooltip = MyStringId.GetOrCompute(tooltip);
            slider.SetLimits(lower, upper);
            slider.Getter = b =>
            {
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

            MyAPIGateway.TerminalControls.AddControl<T>(slider);
        }
    }
}
