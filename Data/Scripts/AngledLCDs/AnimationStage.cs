using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRageMath;

namespace Natomic.AngledLCDs
{
    class AnimationStage
    {
        public static readonly string SectionName = "ADJLCD-ANI";
        public float AzimuthDegs = 0f;
        public float PitchDegs = 0f;
        public float RollDegs = 0f;
        public uint Timecode = 0;
        private Vector3D offset_ = Vector3D.Zero;
        public double X { set => offset_.X = value; get => offset_.X; }
        public double Y { set => offset_.Y = value; get => offset_.Y; }
        public double Z {
            set => offset_.Z = value; get => offset_.Z;
        }
        public float ForwardOffset => (float)X;
        public float LeftOffset => (float)Y;
        public float UpOffset => (float)Z;
        public Vector3D Offset { set => offset_ = value; get => offset_; }
        


        private static void OriginRotate(ref Matrix by, Vector3D origin, Matrix translation)
        {
            by *= Matrix.CreateTranslation(-origin); // Move it to the origin
             
            by *= translation; // Pivot around 0, 0

            by *= Matrix.CreateTranslation(origin); // Move it back
 
        }
        public Matrix TargetLocation(Matrix origin)
        {
            var subpartLocalMatrix = origin;
            var localForward = origin.Right;

            var angle_transform = Matrix.CreateFromAxisAngle(localForward, MathHelper.ToRadians(PitchDegs))
                                * Matrix.CreateFromAxisAngle(origin.Up, MathHelper.ToRadians(AzimuthDegs))
                                * Matrix.CreateFromAxisAngle(origin.Forward, MathHelper.ToRadians(RollDegs));
            OriginRotate(ref subpartLocalMatrix, subpartLocalMatrix.Translation, angle_transform);
            subpartLocalMatrix *= Matrix.CreateTranslation((Vector3D)subpartLocalMatrix.Forward * offset_.X);
            subpartLocalMatrix *= Matrix.CreateTranslation((Vector3D)subpartLocalMatrix.Left * LeftOffset);
            subpartLocalMatrix *= Matrix.CreateTranslation((Vector3D)subpartLocalMatrix.Up * UpOffset);

            subpartLocalMatrix = Matrix.Normalize(subpartLocalMatrix);
            return subpartLocalMatrix;
        }
        public Matrix TargetLocationAtTimecode(Matrix origin, Matrix from, uint start, uint code)
        {
            if (code > Timecode)
            {
                throw new InvalidBranchException($"timecode exceeds animation stage timecode: {code} > {Timecode}");
            }
            var target = TargetLocation(origin);
            var translation = target - from;
            var scale = Timecode - start;
            var scaling = code / (float)scale;
            return from + translation * scaling;
        }
    }
}
