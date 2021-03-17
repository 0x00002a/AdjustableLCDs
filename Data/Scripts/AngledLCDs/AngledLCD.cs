﻿using Sandbox.Common.ObjectBuilders;
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
        private float offset_ = 0f;
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
        public float Offset
        {
            set
            {
                offset_ = value;
                rotated_ = false;
            }
        }
        private Matrix origin_;
        private MyIni ini_helper_ = new MyIni();
        private const string INI_SEC_NAME = "AngledLCDs Save Data";
        private readonly MyIniKey azimuth_key_ = new MyIniKey(INI_SEC_NAME, "azimuth");
        private readonly MyIniKey pitch_key_ = new MyIniKey(INI_SEC_NAME, "pitch");
        private readonly MyIniKey offset_key_ = new MyIniKey(INI_SEC_NAME, "offset");
        

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // this method is called async! always do stuff in the first update unless you're sure it must be in this one.
            // NOTE the objectBuilder arg is not the Entity's but the component's, and since the component wasn't loaded from an OB that means it's always null, which it is (AFAIK).

            block = (T)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            if (!controls_created_)
            {
                CreateTermControls();
            }

        }
        private static void CreateTermControls()
        {
            var xrot = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>("xrot_slider");
            xrot.Title = MyStringId.GetOrCompute("Azimuth rotation");
            xrot.Tooltip = MyStringId.GetOrCompute("-180 to 180 in degrees");
            xrot.SetLimits(-180, 180);
            xrot.Getter = b => b.GameLogic.GetAs<AngledLCD<T>>().AzimuthDegs;
            xrot.Setter = (b, value) =>
            {
                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                lcd.AzimuthDegs = value;
                lcd.SaveData();
            };
            xrot.Writer = (b, str) => str.Append(Math.Round(b.GameLogic.GetAs<AngledLCD<T>>().AzimuthDegs, 2).ToString());

            var zrot = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>("zrot_slider");
            zrot.Title = MyStringId.GetOrCompute("Pitch");
            zrot.Tooltip = MyStringId.GetOrCompute("-180 to 180 in degrees");
            zrot.SetLimits(-180, 180);
            zrot.Getter = b => b.GameLogic.GetAs<AngledLCD<T>>().PitchDegs;
            zrot.Setter = (b, value) =>
            {
                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                lcd.PitchDegs = value;
                lcd.SaveData();
            };
            zrot.Writer = (b, str) => str.Append(Math.Round(b.GameLogic.GetAs<AngledLCD<T>>().PitchDegs, 2).ToString());

            
            var offs = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>("offs_slider");
            offs.Title = MyStringId.GetOrCompute("Offset");
            offs.Tooltip = MyStringId.GetOrCompute("0 to 1, forward offset");
            offs.SetLimits(-10, 10);
            offs.Getter = b => b.GameLogic.GetAs<AngledLCD<T>>().offset_;
            offs.Setter = (b, value) =>
            {
                var lcd = b.GameLogic.GetAs<AngledLCD<T>>();
                lcd.Offset = value;
                lcd.SaveData();
            };
            offs.Writer = (b, str) => str.Append(Math.Round(b.GameLogic.GetAs<AngledLCD<T>>().offset_, 2).ToString());


            MyAPIGateway.TerminalControls.AddControl<T>(xrot);
            MyAPIGateway.TerminalControls.AddControl<T>(zrot);
            MyAPIGateway.TerminalControls.AddControl<T>(offs);

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
                Offset = (float)ini_helper_.Get(offset_key_).ToDouble();
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
            ini_helper_.Set(offset_key_, offset_);

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
                    Matrix relativeTransform;

                    //subpartLocalMatrix *= relativeTransform;
                    //OriginRotate(ref subpartLocalMatrix, subpartLocalMatrix.Translation, Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(AzimuthDegs), MathHelper.ToRadians(PitchDegs), 0));
                    OriginRotate(ref subpartLocalMatrix, subpartLocalMatrix.Translation, Matrix.CreateFromAxisAngle(localForward, MathHelper.ToRadians(pitch_degs_)) * Matrix.CreateFromAxisAngle(origin_.Up, MathHelper.ToRadians(azimuth_degs_)));
                    subpartLocalMatrix *= Matrix.CreateTranslation(subpartLocalMatrix.Forward * offset_);

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

        public override void UpdateAfterSimulation()
        {
            // executed 60 times a second before physics simulation, unless game is paused.
            // triggered only if NeedsUpdate contains MyEntityUpdateEnum.EACH_FRAME.
            /*  MyEntitySubpart sp;
              if (!setup_ && Entity.TryGetSubpart(SUBPART_ROT_ID, out sp))
              {
                  screenRender = new MyRenderComponentScreenAreas(sp);
                  sp.Render.ContainerBase.Add(screenRender);
                  screenRender = sp.Render.GetAs<MyRenderComponentScreenAreas>();
                  screenRender.AddScreenArea(sp.Render.RenderObjectIDs, "ScreenArea");
                  screenRender.UpdateModelProperties();
                  setup_ = true;

              }*/
        }

        public override void UpdateAfterSimulation100()
        {
            // executed approximately every 100 ticks (~1.66s), unless game is paused.
            // why approximately? Explained at the "Important information" in: https://forum.keenswh.com/threads/pb-scripting-guide-how-to-use-self-updating.7398267/
            // there's also a 10-tick variant.
            // triggered only if NeedsUpdate contains MyEntityUpdateEnum.EACH_100TH_FRAME, same for UpdateBeforeSimulation100().

        }
    }
}
