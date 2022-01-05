/*
    Adjustable LCDs Space Engineers mod
    Copyright (C) 2021 Natasha England-Elbro

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using System;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRageMath;
using Digi;
using Sandbox.Game.Components;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using VRage.Game.ModAPI.Ingame.Utilities;
using System.Collections.Generic;

namespace Natomic.AngledLCDs
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false)]
    public class AngledLCDTextPanel : AngledLCD<IMyTextPanel> { }


    public class AngledLCD<T> : MyGameLogicComponent where T: IMyFunctionalBlock
    {
        private T block; // storing the entity as a block reference to avoid re-casting it every time it's needed, this is the lowest type a block entity can be.

        private Matrix subpartLocalMatrix;
        private bool positionUnchanged = true;
        private bool originValid = false;

        private static bool controls_created_ = false;
        private AnimationStage current_stage_;
        private List<AnimationStage> stages_ = new List<AnimationStage>();


        public float RollDegs
        {
            set
            {
                current_stage_.RollDegs = value;
                positionUnchanged = false;
            } get { return current_stage_.RollDegs;  }
        }

        public float AzimuthDegs { set
            {
                current_stage_.AzimuthDegs = value;
                positionUnchanged = false;
            } get { return current_stage_.AzimuthDegs; } }
        public float PitchDegs
        {
            set
            {
                current_stage_.PitchDegs = value;
                positionUnchanged = false;
            }
            get { return current_stage_.PitchDegs; }
        }
        public float ForwardOffset
        {
            set
            {
                current_stage_.X = value;
                positionUnchanged = false;
            } 
            get
            {
                return (float)current_stage_.X;
            }
        }
        public float LeftOffset
        {
            set
            {
                current_stage_.Y = value;
                positionUnchanged = false;
            }
            get
            {
                return (float)current_stage_.Y;
            }
        }
        public float UpOffset
        {
            set
            {
                current_stage_.Z = value;
                positionUnchanged = false;
            } 
            get
            {
                return (float)current_stage_.Z;
            }
        }
        private Matrix origin_;
        private readonly MyIni ini_helper_ = new MyIni();
        private const string INI_SEC_NAME = "AngledLCDs Save Data";
        private MyIniKey AzimuthKey(string sec) => new MyIniKey(sec, "azimuth");
        private MyIniKey PitchKey(string sec) => new MyIniKey(sec, "pitch");
        private MyIniKey RollKey(string sec) => new MyIniKey(sec, "roll");
        private MyIniKey OffsetKey(string sec) => new MyIniKey(sec, "offset");
        private MyIniKey TimecodeKey(string sec) => new MyIniKey(sec, "timecode");

        private static string CreateStageSectName(int num) => AnimationStage.SectionName + ":" + num.ToString();
        

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = (T)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            if (!controls_created_)
            {
                CreateTermControls();
            }
            block.Hierarchy.OnParentChanged += (p1, p2) => OnMergeblockConnect();
        }


        private void OnMergeblockConnect()
        {
            // Mergeblock behaviour is to destroy the old grid and move the blocks, which means we gotta update or the transformations get reset
            positionUnchanged = false;
            originValid = false;
        }

        private static void LogInvalidScript()
        {
            Log.Error("GameLogic returned null for AngledLCD", "An installed mod is incompatible with Adjustable LCDs. The only current known incompatability is with https://steamcommunity.com/workshop/filedetails/?id=2217821984");
        }
        private static void AddTermSlider(string name, string title, string tooltip, int lower, int upper, Action<AngledLCD<T>, float> set, Func<AngledLCD<T>, float> get)
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
        private static void CreateTermControls()
        {
            var sep = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, T>("angled_lcds_sep");
            MyAPIGateway.TerminalControls.AddControl<T>(sep);

            AddTermSlider("zrot_slider", "Pitch", "-180 to 180 in degrees", -180, 180, (b, v) => b.PitchDegs = v, b => b.PitchDegs);
            AddTermSlider("xrot_slider", "Yaw", "-180 to 180 in degrees", -180, 180, (b, v) => b.AzimuthDegs = v, b => b.AzimuthDegs);
            AddTermSlider("yrot_slider", "Roll", "-180 to 180 in degrees", -180, 180, (b, v) => b.RollDegs = v, b => b.RollDegs);
            AddTermSlider("xffs_slider", "Z Offset", "-10 to 10, forward offset", -10, 10, (b, v) => b.ForwardOffset = v, b => b.ForwardOffset);
            AddTermSlider("zffs_slider", "X Offset", "-10 to 10, left offset", -10, 10, (b, v) => b.LeftOffset = v, b => b.LeftOffset);
            AddTermSlider("yffs_slider", "Y Offset", "-10 to 10, up offset", -10, 10, (b, v) => b.UpOffset= v, b => b.UpOffset);
                

            controls_created_ = true;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
            LoadData();
        }
        
        private void LoadData()
        {
            var sections_cache = new List<string>();
            sections_cache.Add(INI_SEC_NAME);
            var data = block.CustomData;
            if (ini_helper_.TryParse(data))
            {
                ini_helper_.GetSections(sections_cache);
                foreach (var section in sections_cache)
                {
                    if (section.StartsWith(AnimationStage.SectionName))
                    {
                        var animation = new AnimationStage
                        {
                            AzimuthDegs = (float)ini_helper_.Get(AzimuthKey(section)).ToDouble(),
                            PitchDegs = (float)ini_helper_.Get(PitchKey(section)).ToDouble(),
                            RollDegs = (float)ini_helper_.Get(RollKey(section)).ToDouble(),
                            Timecode = ini_helper_.Get(TimecodeKey(section)).ToUInt32(),
                        };

                        Vector3D.TryParse(ini_helper_.Get(OffsetKey(section)).ToString(), out var offset);
                        animation.Offset = offset;
                    }
                }
            }

        }
        private void SaveToIni()
        {
            var n = 0;
            foreach (var stage in stages_)
            {
                var sect = CreateStageSectName(n);
                ini_helper_.AddSection(sect);
                ini_helper_.Set(AzimuthKey(sect), stage.AzimuthDegs);
                ini_helper_.Set(PitchKey(sect), stage.PitchDegs);
                ini_helper_.Set(RollKey(sect), stage.RollDegs);
                ini_helper_.Set(OffsetKey(sect), stage.Offset.ToString());
                ini_helper_.Set(TimecodeKey(sect), stage.Timecode);
                ++n;
            }
        }
        private void SaveData()
        {
            if (block.CustomData.Length > 0 && !ini_helper_.TryParse(block.CustomData))
            {
                var msg = $"Custom data of {block.CustomName} is incorrectly formatted";
                Log.Error($"Failed to save block data: {msg}", msg);
                return;
            }
            SaveToIni();
            
            block.CustomData = ini_helper_.ToString();
        }


        public override void UpdateBeforeSimulation10()
        {
            try
            {
                if (!positionUnchanged)
                {
                    if (!originValid)
                    {
                        origin_ = block.PositionComp.LocalMatrixRef;
                        origin_ = Matrix.Normalize(origin_);
                        originValid = true;
                    }

                    subpartLocalMatrix = current_stage_.TargetLocation(origin_);

                    block.PositionComp.SetLocalMatrix(ref subpartLocalMatrix);

                    if (block is IMyFunctionalBlock b) // This is hacky af but turning it off and on again updates it apparently
                    {
                        b.Enabled = false;
                        b.Enabled = true;
                    }
                    positionUnchanged = true;
                }

            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);

            }

        }

        
    }
}
