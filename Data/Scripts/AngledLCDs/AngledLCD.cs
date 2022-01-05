﻿/*
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
using Sandbox.Game.EntityComponents;

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

        private bool useModStorage = false;

        private static bool controls_created_ = false;
        private AnimationStage current_stage_;
        private LCDSettings settings;


        private readonly List<string> sections_cache = new List<string>();

        private bool UseModStorage
        {
            set
            {
                if (value && !useModStorage)
                {
                    MigrateToModStore();
                }
                else if (!value && useModStorage)
                {
                    MigrateToCustomDataStore();
                }
            }
            get
            {
                return useModStorage;
            }
        }

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

        private void MigrateToModStore()
        {
            useModStorage = true;
            LCDSettings.ReloadSectionsCache(ini_helper_, sections_cache);

            foreach(var sect in sections_cache)
            {
                ini_helper_.DeleteSection(sect);
            }
            block.CustomData = ini_helper_.ToString();
            SaveData();
        }
        private void MigrateToCustomDataStore()
        {
            useModStorage = false;

            block.Storage?.Remove(LCDSettings.modStorageId);
            SaveData();
        }
        

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
        private static void CreateTermControls()
        {
            var sep = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, T>("angled_lcds_sep");
            MyAPIGateway.TerminalControls.AddControl<T>(sep);

            TerminalHelper.AddTermSlider<T>("zrot_slider", "Pitch", "-180 to 180 in degrees", -180, 180, (b, v) => b.PitchDegs = v, b => b.PitchDegs);
            TerminalHelper.AddTermSlider<T>("xrot_slider", "Yaw", "-180 to 180 in degrees", -180, 180, (b, v) => b.AzimuthDegs = v, b => b.AzimuthDegs);
            TerminalHelper.AddTermSlider<T>("yrot_slider", "Roll", "-180 to 180 in degrees", -180, 180, (b, v) => b.RollDegs = v, b => b.RollDegs);
            TerminalHelper.AddTermSlider<T>("xffs_slider", "Z Offset", "-10 to 10, forward offset", -10, 10, (b, v) => b.ForwardOffset = v, b => b.ForwardOffset);
            TerminalHelper.AddTermSlider<T>("zffs_slider", "X Offset", "-10 to 10, left offset", -10, 10, (b, v) => b.LeftOffset = v, b => b.LeftOffset);
            TerminalHelper.AddTermSlider<T>("yffs_slider", "Y Offset", "-10 to 10, up offset", -10, 10, (b, v) => b.UpOffset= v, b => b.UpOffset);
            TerminalHelper.AddTermChbox<T>("modstore_chbox", "Use mod storage", "-10 to 10, up offset", -10, 10, (b, v) => b.UseModStorage = v, b => b.UseModStorage);
            

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
            useModStorage = block.Storage?.ContainsKey(LCDSettings.modStorageId) ?? false;
            if (!UseModStorage)
            {
                var data = block.CustomData;
                if (ini_helper_.TryParse(data))
                {
                    if (!ini_helper_.ContainsSection(LCDSettings.INI_SEC_NAME)) // First use of mod, default to mod storage
                    {
                        UseModStorage = true;
                        return;
                    }

                    settings = LCDSettings.LoadFrom(ini_helper_, sections_cache);
                }
            } else
            {
                settings = LCDSettings.LoadFrom(block.Storage);
            }

        }
        public void SaveData()
        {
            if (!UseModStorage) { 
                if (block.CustomData.Length > 0 && !ini_helper_.TryParse(block.CustomData))
                {
                    var msg = $"Custom data of {block.CustomName} is incorrectly formatted";
                    Log.Error($"Failed to save block data: {msg}", msg);
                    return;
                }

                settings.SaveTo(ini_helper_);
                block.CustomData = ini_helper_.ToString();
            } else
            {
                settings.SaveTo(block.Storage);
            }
            
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

                    // This is hacky af but turning it off and on again updates it apparently
                    block.Enabled = false;
                    block.Enabled = true;

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
