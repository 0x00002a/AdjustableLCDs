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

        private float azimuth_degs_ = 0f;
        private float pitch_degs_ = 0f;
        private static bool controls_created_ = false;
        private Vector3D offset_ = new Vector3D(0, 0, 0);

        internal float roll_degs_ = 0f;

        public float RollDegs
        {
            set
            {
                roll_degs_ = value;
                positionUnchanged = false;
            } get { return roll_degs_;  }
        }

        public float AzimuthDegs { set
            {
                azimuth_degs_ = value;
                positionUnchanged = false;
            } get { return azimuth_degs_; } }
        public float PitchDegs
        {
            set
            {
                pitch_degs_ = value;
                positionUnchanged = false;
            }
            get { return pitch_degs_; }
        }
        public float ForwardOffset
        {
            set
            {
                offset_.X = value;
                positionUnchanged = false;
            } 
            get
            {
                return (float)offset_.X;
            }
        }
        public float LeftOffset
        {
            set
            {
                offset_.Y = value;
                positionUnchanged = false;
            }
            get
            {
                return (float)offset_.Y;
            }
        }
        public float UpOffset
        {
            set
            {
                offset_.Z = value;
                positionUnchanged = false;
            } 
            get
            {
                return (float)offset_.Z;
            }
        }
        private Matrix origin_;
        private readonly MyIni ini_helper_ = new MyIni();
        private const string INI_SEC_NAME = "AngledLCDs Save Data";
        private readonly MyIniKey azimuth_key_ = new MyIniKey(INI_SEC_NAME, "azimuth");
        private readonly MyIniKey pitch_key_ = new MyIniKey(INI_SEC_NAME, "pitch");
        private readonly MyIniKey roll_key_ = new MyIniKey(INI_SEC_NAME, "roll");
        private readonly MyIniKey offset_key_ = new MyIniKey(INI_SEC_NAME, "offset");
        

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

        private static void AddTermSlider(string name, string title, string tooltip, int lower, int upper, Action<AngledLCD<T>, float> set, Func<AngledLCD<T>, float> get)
        {
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(name);
            slider.Title = MyStringId.GetOrCompute(title);
            slider.Tooltip = MyStringId.GetOrCompute(tooltip);
            slider.SetLimits(lower, upper);
            slider.Getter = b => get(b.GameLogic.GetAs<AngledLCD<T>>());
            slider.Setter = (b, val) =>
            {
                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                set(lcd, val);
                lcd.SaveData();
            };
            slider.Writer = (b, str) => str.Append(Math.Round(get(b.GameLogic.GetAs<AngledLCD<T>>()), 2));

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
            var data = block.CustomData;
            if (ini_helper_.TryParse(data))
            {
                AzimuthDegs = (float)ini_helper_.Get(azimuth_key_).ToDouble();
                PitchDegs = (float)ini_helper_.Get(pitch_key_).ToDouble();
                double roll = roll_degs_;
                ini_helper_.Get(roll_key_).TryGetDouble(out roll);
                RollDegs = (float)roll;
                Vector3D.TryParse(ini_helper_.Get(offset_key_).ToString(), out offset_);
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

            ini_helper_.AddSection(INI_SEC_NAME);
            ini_helper_.Set(azimuth_key_, azimuth_degs_);
            ini_helper_.Set(pitch_key_, pitch_degs_);
            ini_helper_.Set(roll_key_, roll_degs_);
            ini_helper_.Set(offset_key_, offset_.ToString());

            block.CustomData = ini_helper_.ToString();
        }

        private void OriginRotate(ref Matrix by, Vector3D origin, Matrix translation)
        {
            by *= Matrix.CreateTranslation(-origin); // Move it to the origin
             
            by *= translation; // Pivot around 0, 0

            by *= Matrix.CreateTranslation(origin); // Move it back
 
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

                    subpartLocalMatrix = origin_;
                    var localForward = origin_.Right;
                    var gridForward = block.CubeGrid.PositionComp.LocalMatrixRef.Forward;

                    var angle_transform = Matrix.CreateFromAxisAngle(localForward, MathHelper.ToRadians(pitch_degs_))
                                        * Matrix.CreateFromAxisAngle(origin_.Up, MathHelper.ToRadians(azimuth_degs_))
                                        * Matrix.CreateFromAxisAngle(origin_.Forward, MathHelper.ToRadians(roll_degs_));
                    OriginRotate(ref subpartLocalMatrix, subpartLocalMatrix.Translation, angle_transform);
                    subpartLocalMatrix *= Matrix.CreateTranslation((Vector3D)subpartLocalMatrix.Forward * offset_.X);
                    subpartLocalMatrix *= Matrix.CreateTranslation((Vector3D)subpartLocalMatrix.Left * LeftOffset);
                    subpartLocalMatrix *= Matrix.CreateTranslation((Vector3D)subpartLocalMatrix.Up * UpOffset);

                    subpartLocalMatrix = Matrix.Normalize(subpartLocalMatrix);

                    block.PositionComp.SetLocalMatrix(ref subpartLocalMatrix);



                    var b = block as IMyFunctionalBlock;
                    if (b != null) // This is hacky af but turning it off and on again updates it apparently
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
