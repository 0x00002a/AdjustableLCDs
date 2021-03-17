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


    public class AngledLCD<T> : MyGameLogicComponent where T: IMyTerminalBlock
    {
        private T block; // storing the entity as a block reference to avoid re-casting it every time it's needed, this is the lowest type a block entity can be.
        private bool foundSubpart = false;
        private Matrix subpartLocalMatrix;
        bool rotated_ = true;
        private float azimuth_degs_ = 0f;
        private float pitch_degs_ = 0f;
        private static bool controls_created_ = false;
        private Vector3D offset_ = new Vector3D(0, 0, 0);
        public float AzimuthDegs { set
            {
                azimuth_degs_ = value;
                rotated_ = false;
            } get { return azimuth_degs_; } }
        public float PitchDegs
        {
            set
            {
                pitch_degs_ = value;
                rotated_ = false;
            }
            get { return pitch_degs_; }
        }
        public float ForwardOffset
        {
            set
            {
                offset_.X = value;
                rotated_ = false;
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
                rotated_ = false;
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
                rotated_ = false;
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
        private readonly MyIniKey offset_key_ = new MyIniKey(INI_SEC_NAME, "offset");
        

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = (T)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            if (!controls_created_)
            {
                CreateTermControls();
            }

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
                Vector3D.TryParse(ini_helper_.Get(offset_key_).ToString(), out offset_);
            } else
            {
                SaveData();
            }

        }
        private void SaveData()
        {
            ini_helper_.TryParse(block.CustomData);

            ini_helper_.AddSection(INI_SEC_NAME);
            ini_helper_.Set(azimuth_key_, azimuth_degs_);
            ini_helper_.Set(pitch_key_, pitch_degs_);
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
                if (!rotated_)
                {

                    if (!foundSubpart)
                    {
                        foundSubpart = true;
                        origin_ = block.PositionComp.LocalMatrixRef;
                        origin_ = Matrix.Normalize(origin_);
                    }
                    subpartLocalMatrix = origin_;
                    var localForward = origin_.Right;
                    var gridForward = block.CubeGrid.PositionComp.LocalMatrixRef.Forward;

                    //subpartLocalMatrix *= relativeTransform;
                    //OriginRotate(ref subpartLocalMatrix, subpartLocalMatrix.Translation, Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(AzimuthDegs), MathHelper.ToRadians(PitchDegs), 0));
                    OriginRotate(ref subpartLocalMatrix, subpartLocalMatrix.Translation, Matrix.CreateFromAxisAngle(localForward, MathHelper.ToRadians(pitch_degs_)) * Matrix.CreateFromAxisAngle(origin_.Up, MathHelper.ToRadians(azimuth_degs_)));
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
                    rotated_ = true;
                }

            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);

            }

        }

        
    }
}
