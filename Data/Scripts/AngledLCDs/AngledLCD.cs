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

namespace Natomic.AngledLCDs
{
    // This object gets attached to entities depending on their type and optionally subtype aswell.
    // The 2nd arg, "false", is for entity-attached update if set to true which is not recommended, see for more info: https://forum.keenswh.com/threads/modapi-changes-jan-26.7392280/
    // Remove any method that you don't need, they're only there to show what you can use, and also remove comments you've read as they're only for example purposes and don't make sense in a final mod.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false )]
    public class AngledLCD : MyGameLogicComponent
    {
        private IMyCubeBlock block; // storing the entity as a block reference to avoid re-casting it every time it's needed, this is the lowest type a block entity can be.
            private const string SUBPART_ROT_ID = "screen";
            private bool foundSubpart = false;
            private Matrix subpartLocalMatrix;
        bool rotated_ = false;
        MyRenderComponentScreenAreas screenRender;
        bool setup_ = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // this method is called async! always do stuff in the first update unless you're sure it must be in this one.
            // NOTE the objectBuilder arg is not the Entity's but the component's, and since the component wasn't loaded from an OB that means it's always null, which it is (AFAIK).

            block = (IMyCubeBlock)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME; // allow UpdateOnceBeforeFrame() to execute, remove if not needed
            Log.Info("Initied block");
           
        }

        public override void UpdateOnceBeforeFrame()
        {
            // first update, remove if not needed

            if(block.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            // do stuff

            // you can access stuff from session via Example_Session.Instance.[...]

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME; // allow UpdateAfterSimulation() and UpdateAfterSimulation100() to execute, remove if not needed
        }

        public override void MarkForClose()
        {
            // called when entity is about to be removed for whatever reason (block destroyed, entity deleted, ship despawn because of sync range, etc)
        }

        public override void UpdateBeforeSimulation()
        {
            try
            {
                // executed 60 times a second after physics simulation, unless game is paused.
                // triggered only if NeedsUpdate contains MyEntityUpdateEnum.EACH_FRAME.

                var shouldSpin = block.IsWorking;

                //        var camPos = MyAPIGateway.Session.Camera.WorldMatrix.Translation;
               /* MyEntitySubpart sp;
                if (Entity.TryGetSubpart(SUBPART_ROT_ID, out sp) && !rotated_)
                {
                    if (!foundSubpart)
                    {
                        foundSubpart = true;
                        subpartLocalMatrix = sp.PositionComp.LocalMatrix;
                        subpartLocalMatrix = Matrix.Normalize(subpartLocalMatrix);
                    }
                    subpartLocalMatrix *= Matrix.CreateFromAxisAngle(subpartLocalMatrix.Up, MathHelper.ToRadians(-45));
                    subpartLocalMatrix = Matrix.Normalize(subpartLocalMatrix);

                    sp.PositionComp.LocalMatrix = subpartLocalMatrix;
                    rotated_ = true;
                }*/
               if (!rotated_)
                {

                    if (!foundSubpart)
                    {
                        foundSubpart = true;
                        subpartLocalMatrix = block.PositionComp.LocalMatrix;
                        subpartLocalMatrix = Matrix.Normalize(subpartLocalMatrix);
                    }
                    subpartLocalMatrix *= Matrix.CreateFromAxisAngle(subpartLocalMatrix.Up, MathHelper.ToRadians(-45));
                    subpartLocalMatrix = Matrix.Normalize(subpartLocalMatrix);

                    block.PositionComp.LocalMatrix = subpartLocalMatrix;
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
