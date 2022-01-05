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
                if (value != useModStorage)
                {
                    TerminalHelper.RefreshAll();
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
        private static void CreateTermControls()
        {
            controls_created_ = true;

            var sep = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, T>("angled_lcds_sep");
            MyAPIGateway.TerminalControls.AddControl<T>(sep);

            TerminalHelper.AddTermSlider<T>("zrot_slider", "Pitch", "-180 to 180 in degrees", -180, 180, (b, v) => b.PitchDegs = v, b => b.PitchDegs);
            TerminalHelper.AddTermSlider<T>("xrot_slider", "Yaw", "-180 to 180 in degrees", -180, 180, (b, v) => b.AzimuthDegs = v, b => b.AzimuthDegs);
            TerminalHelper.AddTermSlider<T>("yrot_slider", "Roll", "-180 to 180 in degrees", -180, 180, (b, v) => b.RollDegs = v, b => b.RollDegs);
            TerminalHelper.AddTermSlider<T>("xffs_slider", "Z Offset", "-10 to 10, forward offset", -10, 10, (b, v) => b.ForwardOffset = v, b => b.ForwardOffset);
            TerminalHelper.AddTermSlider<T>("zffs_slider", "X Offset", "-10 to 10, left offset", -10, 10, (b, v) => b.LeftOffset = v, b => b.LeftOffset);
            TerminalHelper.AddTermSlider<T>("yffs_slider", "Y Offset", "-10 to 10, up offset", -10, 10, (b, v) => b.UpOffset = v, b => b.UpOffset);


            TerminalHelper.AddTermTxtbox<T>("anistagename_ent", "Stage name", "Name for new stage", (b, v) => b.stageNameStr = v, b => b.stageNameStr);
            TerminalHelper.AddTermBtn<T>("anistage_add_btn", "Add stage", "Add saved stage", lcd => lcd.AddStage());


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
                b.current_stage_ = (AnimationStage)items[0].UserData;
                b.positionUnchanged = false;
                b.selectedStages.Clear();
                b.selectedStages.AddRange(items.Select(item => (AnimationStage)item.UserData));
                TerminalHelper.RefreshAll();

            }, 5, true);
            AddEnabled(lcd => lcd != null && (lcd.selectedStages.Count > 0 && lcd.selectedStages.Count < lcd.settings.Stages.Count), TerminalHelper.AddTermBtn<T>("anistage_rm_btn", "Remove stage", "Remove saved stage", lcd =>
            {
                if (lcd.selectedStages.Count < lcd.settings.Stages.Count)
                {
                    lcd.settings.Stages.RemoveAll(s => lcd.selectedStages.Contains(s));
                }
                lcd.selectedStages.Clear();
                TerminalHelper.RefreshAll();
                lcd.SaveData();
            }));

            AddEnabled(logic => logic != null && (!logic.useModStorage || logic.settings.Steps.Count == 0), TerminalHelper.AddTermChbox<T>("modstore_chbox", "Use mod storage", "Untick to select custom data, it persists even when the mod isn't loaded but may cause conflicts with some scripts", (b, v) => b.UseModStorage = v, b => b.UseModStorage));

            CtrlReqMStore(TerminalHelper.AddTermTxtbox<T>("anitimeframe_ent", "Animation ticks", "Ticks for the animation to take", (b, v) => b.animationTicksStr = v, b => b.animationTicksStr));
            CtrlReqMStore(
                AddEnabled(lcd => lcd != null && lcd.selectedStages.Count > 1,
                TerminalHelper.AddTermBtn<T>("anistep_add", "Add step", "Add animation step", lcd => lcd.AddAnimationStep())));
            CtrlReqMStore(TerminalHelper.AddTermListSel<T>("anisteps_list", "Steps", "Animation steps", (b, content, sel) =>
                {
                    foreach (var step in b.settings.Steps)
                    {
                        var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(step.ToString()), MyStringId.GetOrCompute(""), step);
                        content.Add(item);
                        if (step == b.currentAnimation)
                        {
                            sel.Add(item);
                        }
                    }

                }, (b, items) =>
                {
                    b.currentAnimation = (LCDSettings.AnimationChain)items[0].UserData;
                    TerminalHelper.RefreshAll();
                }, 5, false));
            CtrlReqMStore(AddEnabled(lcd => lcd != null && lcd.currentAnimation != null,
            TerminalHelper.AddTermBtn<T>("anistep_rm_btn", "Remove step", "Removes selected step", lcd => lcd.RemoveCurrAnimation()))); 
            CtrlReqMStore(
                AddEnabled(lcd => lcd != null && lcd.currentAnimation != null, 
            TerminalHelper.AddTermBtn<T>("anistart_btn", "Start animation", "Starts the selected animation", lcd => lcd.StartAnimation())));
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
                var timesteps = animationTicksStr.ToString().Split(',');
                if (stages.Count > 1 && timesteps.Length >= 1)
                {
                    var steps = new List<AnimationStep>();
                    for (var n = 0; n != stages.Count - 1; ++n)
                    {
                        var from = stages[n];
                        var to = stages[n + 1];
                        var ticks = timesteps.Length < n ? timesteps[n] : timesteps[timesteps.Length - 1];
                        steps.Add(new AnimationStep { StageFrom = from.Name, StageTo = to.Name, Ticks = uint.Parse(ticks) });
                    }
                    settings.Steps.Add(new LCDSettings.AnimationChain { Steps = steps });
                    selectedStages.Clear();
                    TerminalHelper.RefreshAll();
                    SaveData();
                }
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
            current_stage_ = settings.Stages[0];
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
