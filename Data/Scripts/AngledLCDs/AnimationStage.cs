﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRageMath;

namespace Natomic.AngledLCDs
{
    [ProtoContract]
    class AnimationStage
    {
        public static readonly string SectionName = "ADJLCD-ANI";
        [ProtoMember(1)]
        public float AzimuthDegs = 0f;
        [ProtoMember(2)]
        public float PitchDegs = 0f;
        [ProtoMember(3)]
        public float RollDegs = 0f;
        [ProtoMember(4)]
        public string Name = String.Empty;
        [ProtoMember(5)]
        private Vector3D offset_ = Vector3D.Zero;
        [ProtoMember(6)]
        public Vector3 RotationOriginOffset = Vector3.Zero;
        [ProtoMember(7)]
        public bool CustomRotOrigin = false;
        public double X
        {
            set { offset_.X = value; }
            get
            {
                return offset_.X;
            }
        }
        public double Y
        {
            set { offset_.Y = value; }
            get
            {
                return offset_.Y;
            }
        }
        public double Z
        {
            set
            {
                offset_.Z = value;
            }

            get
            {
                return offset_.Z;
            }
        }
        public float ForwardOffset => (float)X;
        public float LeftOffset => (float)Y;
        public float UpOffset => (float)Z;
        public Vector3D Offset
        {
            set { offset_ = value; }
            get
            {
                return offset_;
            }
        }



        private static void OriginRotate(ref Matrix by, Vector3D origin, Matrix translation)
        {
            by *= Matrix.CreateTranslation(-origin); // Move it to the origin
             
            by *= translation; // Pivot around 0, 0

            by *= Matrix.CreateTranslation(origin); // Move it back
 
        }
        public Matrix TargetLocation(Matrix origin)
        {
            var subpartLocalMatrix = origin;

            var angle_transform = Matrix.CreateFromAxisAngle(origin.Right, MathHelper.ToRadians(PitchDegs))
                                * Matrix.CreateFromAxisAngle(origin.Up, MathHelper.ToRadians(AzimuthDegs))
                                * Matrix.CreateFromAxisAngle(origin.Forward, MathHelper.ToRadians(RollDegs));
            //var translation = subpartLocalMatrix.Translation;
            //translation.Forw
            //subpartLocalMatrix.Translation += rotOffset;
            if (!CustomRotOrigin)
            {
                OriginRotate(ref subpartLocalMatrix, subpartLocalMatrix.Translation, angle_transform);
            }
            subpartLocalMatrix *= Matrix.CreateTranslation((Vector3D)subpartLocalMatrix.Forward * offset_.X);
            subpartLocalMatrix *= Matrix.CreateTranslation((Vector3D)subpartLocalMatrix.Left * LeftOffset);
            subpartLocalMatrix *= Matrix.CreateTranslation((Vector3D)subpartLocalMatrix.Up * UpOffset);
            if (CustomRotOrigin)
            {
                OriginRotate(ref subpartLocalMatrix, subpartLocalMatrix.Translation + RotationOriginOffset, angle_transform);
            }

            subpartLocalMatrix = Matrix.Normalize(subpartLocalMatrix);
            return subpartLocalMatrix;
        }
        public Matrix TargetLocationAtTimecode(Matrix origin, Matrix from, AnimationStep step, uint code)
        {
            if (code > step.Ticks)
            {
                throw new ArgumentException($"timecode exceeds animation stage timecode: {code} > {step.Ticks}");
            }
            var target = TargetLocation(origin);
            var translation = target - from;
            var scale = step.Ticks;
            var scaling = code / (float)scale;
            return from + translation * scaling;
        }
    }
}
