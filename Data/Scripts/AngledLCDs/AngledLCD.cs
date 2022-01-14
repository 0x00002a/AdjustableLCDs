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

using Digi;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Natomic.AngledLCDs
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false)]
    public class AngledLCDTextPanel : AngledLCD<IMyTextPanel> { }


    public class AngledLCD<T> : MyGameLogicComponent where T : IMyFunctionalBlock
    {
        private T block; // storing the entity as a block reference to avoid re-casting it every time it's needed, this is the lowest type a block entity can be.

        private Matrix subpartLocalMatrix;
        private bool positionUnchanged = true;
        private bool originValid = false;

        private bool useModStorage = false;

        private static bool controls_created_ = false;
        private StringBuilder animationTicksStr = new StringBuilder();
        private StringBuilder stageNameStr = new StringBuilder();
        private List<AnimationStage> selectedStages = new List<AnimationStage>();

        private AnimationStage current_stage_;
        private LCDSettings settings;
        private AnimationController animationCtrl;
        private LCDSettings.AnimationChain currentAnimation;



        private readonly List<string> sections_cache = new List<string>();

        private bool CustomRotOrigin
        {
            get
            {
                return current_stage_.CustomRotOrigin;
            } set { current_stage_.CustomRotOrigin = value; TerminalHelper.RefreshAll(); positionUnchanged = false; }
        }
        private AnimationStage CurrentStage
        {
            set
            {
                settings.ActiveStage = settings.Stages.IndexOf(value);
                current_stage_ = value;
            }
            get
            {
                return current_stage_;
            }
        }
        private LCDSettings.AnimationChain CurrentAnimation
        {
            set
            {
                settings.SelectedStep = settings.Steps.IndexOf(value);
                currentAnimation = value;
            }
            get
            {
                if (currentAnimation == null && settings.SelectedStep < settings.Steps.Count)
                {
                    currentAnimation = settings.Steps[settings.SelectedStep];
                }
                return currentAnimation;
            }
        }

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
            }
            get { return current_stage_.RollDegs; }
        }

        public float AzimuthDegs
        {
            set
            {
                current_stage_.AzimuthDegs = value;
                positionUnchanged = false;
            }
            get { return current_stage_.AzimuthDegs; }
        }
        public float PitchDegs
        {
            set
            {
                current_stage_.PitchDegs = value;
                positionUnchanged = false;
            }
            get { return current_stage_.PitchDegs; }
        }
        public float ForwardRotOrigin
        {
            set
            {
                current_stage_.RotationOriginOffset.X = value;
                positionUnchanged = false;
            }
            get
            {
                return current_stage_.RotationOriginOffset.X;
            }
        }
        public float LeftRotOrigin
        {
            set
            {
                current_stage_.RotationOriginOffset.Y = value;
                positionUnchanged = false;
            }
            get
            {
                return current_stage_.RotationOriginOffset.Y;
            }
        }
        public float UpRotOrigin
        {
            set
            {
                current_stage_.RotationOriginOffset.Z = value;
                positionUnchanged = false;
            }
            get
            {
                return current_stage_.RotationOriginOffset.Z;
            }
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
        public bool CanSafelyStoreInCD => settings.Steps.Count == 0 && !CustomRotOrigin && settings.Stages.Count == 1;
        private Matrix origin_;
        private readonly MyIni ini_helper_ = new MyIni();

        private void MigrateToModStore()
        {
            useModStorage = true;
            LCDSettings.ReloadSectionsCache(ini_helper_, sections_cache);

            foreach (var sect in sections_cache)
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

            block.Hierarchy.OnParentChanged += (p1, p2) => OnMergeblockConnect();
        }


        private void OnMergeblockConnect()
        {
            // Mergeblock behaviour is to destroy the old grid and move the blocks, which means we gotta update or the transformations get reset
            positionUnchanged = false;
            originValid = false;
        }

        private void DecSelAnimation()
        {
            if (currentAnimation == null)
            {
                return;
            }
            var nextIdx = settings.Steps.IndexOf(currentAnimation) - 1;
            if (nextIdx < 0)
            {
                return;
            }
            currentAnimation = settings.Steps[nextIdx];
            if (animationCtrl?.Valid ?? false) StartAnimation();
        }
        private void IncSelAnimation()
        {
            if (currentAnimation == null)
            {
                return;
            }
            var nextIdx = settings.Steps.IndexOf(currentAnimation) + 1;
            if (nextIdx >= settings.Steps.Count)
            {
                return;
            }
            currentAnimation = settings.Steps[nextIdx];
            if (animationCtrl?.Valid ?? false) StartAnimation();
        }
        private static void CreateTermControls()
        {
            controls_created_ = true;

            var sep = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, T>("angled_lcds_sep");
            MyAPIGateway.TerminalControls.AddControl<T>(sep);

            TerminalHelper.AddTermAct<T>("trigger_animation_act", "Start animation", lcd => lcd?.StartAnimation());
            TerminalHelper.AddTermAct<T>("trigger_animation_inc_act", "Next animation", lcd => lcd?.IncSelAnimation());
            TerminalHelper.AddTermAct<T>("trigger_animation_dec_act", "Last animation", lcd => lcd?.DecSelAnimation());

            TerminalHelper.AddTermSlider<T>("zrot_slider", "Pitch", "-180 to 180 in degrees", -180, 180, (b, v) => b.PitchDegs = v, b => b.PitchDegs);
            TerminalHelper.AddTermSlider<T>("xrot_slider", "Yaw", "-180 to 180 in degrees", -180, 180, (b, v) => b.AzimuthDegs = v, b => b.AzimuthDegs);
            TerminalHelper.AddTermSlider<T>("yrot_slider", "Roll", "-180 to 180 in degrees", -180, 180, (b, v) => b.RollDegs = v, b => b.RollDegs);
            TerminalHelper.AddTermSlider<T>("xffs_slider", "Location offset forward/back", "Position forward/back", -10, 10, (b, v) => b.ForwardOffset = v, b => b.ForwardOffset);
            TerminalHelper.AddTermSlider<T>("zffs_slider", "Location offset left/right", "Position left/right", -10, 10, (b, v) => b.LeftOffset = v, b => b.LeftOffset);
            TerminalHelper.AddTermSlider<T>("yffs_slider", "Location offset up/down", "Position up/down", -10, 10, (b, v) => b.UpOffset = v, b => b.UpOffset);

            AddEnabled(logic => logic != null && (!logic.useModStorage || logic.CanSafelyStoreInCD), 
                TerminalHelper.AddTermChbox<T>(
                    "modstore_chbox", 
                    "Use mod storage", "Untick to select custom data, it persists even when the mod isn't loaded but may cause conflicts with some scripts. Most features are disabled without mod storage and it cannot be disabled while these are in use to prevent data loss", 
                    (b, v) => {
                        b.UseModStorage = v;
                        TerminalHelper.RefreshAll();
                        },
                    b => b.UseModStorage));
            Func<AngledLCD<T>, bool> visCheckForRel = lcd => lcd != null && lcd.CustomRotOrigin && lcd.UseModStorage;
            CtrlReqMStore( 
                TerminalHelper.AddTermChbox<T>("custom_rot_orig_chbox", "Offsets are absolute", "Apply offset transform before rotation, meaning rotation becomes relative to the offset rather than the other way round (default: off)", (lcd, v) => lcd.CustomRotOrigin = v, lcd => lcd.CustomRotOrigin)
            );

            TerminalHelper.AddTermTxtbox<T>("anistagename_ent", "Stage name", "Name for new stage", (b, v) => b.stageNameStr = v, b => b.stageNameStr);
            AddEnabled(
                lcd => lcd != null && lcd.UseModStorage,
                TerminalHelper.AddTermBtn<T>("anistage_add_btn", "Add stage", "Add saved stage", lcd => lcd.AddStage())
                );


            TerminalHelper.AddTermListSel<T>("aniframes_sel", "Stages", "Stages optionally used for animations", (b, content, sel) =>
            {
                foreach (var stage in b.settings.Stages)
                {
                    if (stage.Name.Length == 0)
                    {
                        continue;
                    }
                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(stage.Name), MyStringId.GetOrCompute(stage.Name), stage);
                    content.Add(item);
                    if (b.selectedStages.Contains(stage))
                    {
                        sel.Add(item);
                    }
                }

            }, (b, items) =>
            {
                b.CurrentStage = (AnimationStage)items[0].UserData;
                b.positionUnchanged = false;
                b.selectedStages.Clear();
                b.selectedStages.AddRange(items.Select(item => (AnimationStage)item.UserData));
                TerminalHelper.RefreshAll();
                b.SaveData();
            }, 5, true);
            AddEnabled(lcd => lcd != null && (lcd.selectedStages.Count > 0 && lcd.selectedStages.Count < lcd.settings.Stages.Count), TerminalHelper.AddTermBtn<T>("anistage_rm_btn", "Remove stage", "Remove saved stage", lcd =>
            {
                if (lcd.selectedStages.Count < lcd.settings.Stages.Count)
                {
                    lcd.settings.Stages.RemoveAll(s => lcd.selectedStages.Contains(s));
                }
                lcd.selectedStages.Clear();
                if (!lcd.settings.Stages.Contains(lcd.CurrentStage))
                {
                    lcd.CurrentStage = lcd.settings.Stages[0];
                }
                TerminalHelper.RefreshAll();
                lcd.SaveData();
            }));


            CtrlReqMStore(TerminalHelper.AddTermTxtbox<T>("anitimeframe_ent", "Animation ticks", "Ticks for the animation to take. Must be positive. There are 60 ticks in a second", (b, v) => b.animationTicksStr = v, b => b.animationTicksStr));
            CtrlReqMStore(
                AddEnabled(lcd => lcd != null && lcd.selectedStages.Count > 1,
                TerminalHelper.AddTermBtn<T>("anistep_add", "Add step", "Add animation step", lcd => lcd.AddAnimationStep())));
            CtrlReqMStore(TerminalHelper.AddTermListSel<T>("anisteps_list", "Steps", "Animation steps", (b, content, sel) =>
                {
                    foreach (var step in b.settings.Steps)
                    {
                        var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(step.ToString()), MyStringId.GetOrCompute(""), step);
                        content.Add(item);
                        if (step == b.CurrentAnimation)
                        {
                            sel.Add(item);
                        }
                    }

                }, (b, items) =>
                {
                    b.CurrentAnimation = (LCDSettings.AnimationChain)items[0].UserData;
                    b.SaveData();
                    TerminalHelper.RefreshAll();
                }, 5, false));
            CtrlReqMStore(AddEnabled(lcd => lcd != null && lcd.CurrentAnimation != null,
            TerminalHelper.AddTermBtn<T>("anistep_rm_btn", "Remove step", "Removes selected step", lcd => lcd.RemoveCurrAnimation()))); 
            CtrlReqMStore(
                AddEnabled(lcd => lcd != null && lcd.CurrentAnimation != null, 
            TerminalHelper.AddTermBtn<T>("anistart_btn", "Start animation", "Starts the selected animation", lcd => lcd.StartAnimation())));
        }
        private static IMyTerminalControl AddVisible(Func<AngledLCD<T>, bool> enable, IMyTerminalControl ctrl)
        {
            var savedVis = (Func<IMyTerminalBlock, bool>)ctrl.Visible.Clone();
            ctrl.Enabled = b =>
            {
                return savedVis(b) && enable(b.GameLogic.GetAs<AngledLCD<T>>());
            };
            return ctrl;

        }
        private static IMyTerminalControl AddEnabled(Func<AngledLCD<T>, bool> enable, IMyTerminalControl ctrl)
        {
            var savedEnable = (Func<IMyTerminalBlock, bool>)ctrl.Enabled.Clone();
            ctrl.Enabled = b =>
            {
                return savedEnable(b) && enable(b.GameLogic.GetAs<AngledLCD<T>>());
            };
            return ctrl;
        }
        private static void CtrlReqMStore(IMyTerminalControl ctrl)
        {
            var ctrlEnable = (Func<IMyTerminalBlock, bool>)ctrl.Enabled.Clone();
            ctrl.Enabled = b =>
            {
                return ctrlEnable(b) &&
                (b.GameLogic.GetAs<AngledLCD<T>>()?.useModStorage ?? false);
            };
        }
        private void AddAnimationStep()
        {
            try
            {
                var stages = selectedStages;
                var timestep = -1;
                int.TryParse(animationTicksStr.ToString(), out timestep);
                if (timestep < 0)
                {
                    Log.Error("timestep is invalid, must be a (positive) number");
                    return;
                }
                var steps = new List<AnimationStep>();
                for (var n = 0; n != stages.Count - 1; ++n)
                {
                    var from = stages[n];
                    var to = stages[n + 1];
                    steps.Add(new AnimationStep { StageFrom = from.Name, StageTo = to.Name, Ticks = (uint)timestep });
                }
                settings.Steps.Add(new LCDSettings.AnimationChain { Steps = steps });
                selectedStages.Clear();
                TerminalHelper.RefreshAll();
                SaveData();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        private void AddStage()
        {
            if (stageNameStr.Length == 0)
            {
                return;
            }
            if (settings.Stages.Count == 1 && settings.Stages[0].Name == "") // Special case: first stage added for this
            {
                settings.Stages[0].Name = stageNameStr.ToString();
            }
            else
            {
                settings.Stages.Add(new AnimationStage { Name = stageNameStr.ToString() });
            }
            stageNameStr.Clear();
            TerminalHelper.RefreshAll();
            SaveData();
        }
        private void RemoveCurrAnimation()
        {
            if (currentAnimation == null)
            {
                return;
            }
            settings.Steps.Remove(currentAnimation);
            currentAnimation = null;
            TerminalHelper.RefreshAll();
            SaveData();
        }

        private void StartAnimation()
        {
            if (currentAnimation == null)
            {
                return;
            }
            if (animationCtrl == null)
            {
                animationCtrl = new AnimationController(settings.Stages);
            }
            animationCtrl.Reset(currentAnimation.Steps);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }
        public override void UpdateAfterSimulation() // Used for animation
        {
            if (!animationCtrl?.Valid ?? false)
            {
                NeedsUpdate ^= MyEntityUpdateEnum.EACH_FRAME;
                return;
            }
            subpartLocalMatrix = animationCtrl.Step(origin_);
            UpdateBlockPos();
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (!controls_created_)
            {
                CreateTermControls();
            }

            if (block.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
            LoadData();
            if (settings.Stages.Count == 0)
            {
                settings.Stages.Add(new AnimationStage());
            }
            current_stage_ = settings.Stages[settings.ActiveStage];
            TerminalHelper.RefreshAll();
        }
        private void ReportErr(string msg, Exception e)
        {
            Log.Error($"{msg} (block: {block.Name}, id: {block.EntityId}) {e.Message}\n{e.StackTrace}");
        }

        private void LoadData()
        {
            try
            {
                useModStorage = block.Storage?.ContainsKey(LCDSettings.modStorageId) ?? false;
                if (!UseModStorage)
                {
                    var data = block.CustomData;
                    var success = ini_helper_.TryParse(data);
                    if (success && ini_helper_.ContainsSection(LCDSettings.INI_SEC_NAME))
                    {
                        settings = LCDSettings.LoadFrom(ini_helper_, sections_cache);
                    }
                    else
                    {
                        settings = new LCDSettings();
                        if (success || data.Length == 0)
                        {
                            UseModStorage = true;
                        }
                        else
                        {
                            Log.Info($"Failed to load custom data config for block: {block.CustomName}, defaulting to mod storage with empty config");
                        }
                    }
                }
                else
                {
                    settings = LCDSettings.LoadFrom(block.Storage);
                }
            }
            catch (Exception e)
            {
                ReportErr("Failed to load config", e);
                if (settings == null)
                {
                    settings = new LCDSettings();
                    UseModStorage = true;
                }
            }

        }
        public void SaveData()
        {
            try
            {
                if (!UseModStorage)
                {
                    if (block.CustomData.Length > 0 && !ini_helper_.TryParse(block.CustomData))
                    {
                        var msg = $"Custom data of {block.CustomName} is incorrectly formatted";
                        Log.Error($"Failed to save block data: {msg}", msg);
                        return;
                    }

                    settings.SaveTo(ini_helper_);
                    block.CustomData = ini_helper_.ToString();
                }
                else
                {
                    settings.SaveTo(block.Storage);
                }
            }
            catch (Exception e)
            {
                ReportErr("Failed to save config", e);
            }

        }
        private void UpdateBlockPos()
        {
            block.PositionComp.SetLocalMatrix(ref subpartLocalMatrix);

            // This is hacky af but turning it off and on again updates it apparently
            block.Enabled = false;
            block.Enabled = true;

            positionUnchanged = true;
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
                    UpdateBlockPos();

                }

            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);

            }

        }


    }
}
