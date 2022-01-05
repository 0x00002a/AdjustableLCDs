using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace Natomic.AngledLCDs
{
    class AnimationController
    {
        private List<AnimationStep> steps;
        private List<AnimationStage> stages;
        private uint tick = 1;
        private int currStep = 0;

        private AnimationStage FindStage(string name) => stages.Find(s => s.Name == name);

        public AnimationController(List<AnimationStage> stages)
        {
            this.stages = stages;
        }

        public void Reset(List<AnimationStep> steps)
        {
            this.steps = steps;
            tick = 1;
            currStep = 0;
        }
        public bool Valid => (steps?.Count ?? -1) > currStep;
        public Matrix Step(Matrix origin)
        {
            var step = steps[currStep];
            ++tick;
            if (step.Ticks < tick)
            {
                ++currStep;
                tick = 1;
                if (currStep < steps.Count)
                {
                    return Step(origin);
                } else {
                    return FindStage(steps[steps.Count - 1].StageTo).TargetLocation(origin);
                }
            } else
            {
                var from = FindStage(step.StageFrom);
                return FindStage(step.StageTo).TargetLocationAtTimecode(origin, from.TargetLocation(origin), step, tick);
            }
        }
    }
}
