using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Natomic.AngledLCDs
{
    [ProtoContract]
    class AnimationStep
    {
        [ProtoMember(1)]
        public string StageFrom;
        [ProtoMember(2)]
        public string StageTo;
        [ProtoMember(3)]
        public uint Ticks;

    }
}
