using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;
using System.Linq;
using ProtoBuf;

namespace Natomic.AngledLCDs
{
    [ProtoContract]
    class LCDSettings
    {
        [ProtoContract]
        public class AnimationChain
        {
            private List<AnimationStep> steps_cache_;
            [ProtoMember(1)]
            public List<AnimationStep> Steps
            {
                get
                {
                    if (steps_cache_ == null)
                    {
                        steps_cache_ = new List<AnimationStep>();
                    }
                    return steps_cache_;
                } set { steps_cache_ = value;  }
            }
            public override string ToString()
            {
                string output = "";
                foreach(var item in Steps)
                {
                    output += item.ToString();
                }
                return output;
            }
        };
        private List<AnimationStage> stages_cache_;
        private List<AnimationChain> steps_cache_;
        [ProtoMember(2)]
        public List<AnimationStage> Stages { get
            {
                if (stages_cache_ == null)
                {
                    stages_cache_ = new List<AnimationStage>();
                }
                return stages_cache_;
            } set { stages_cache_ = value; }
        }
        [ProtoMember(3)]
        public List<AnimationChain> Steps { get
            {
                if (steps_cache_ == null)
                {
                    steps_cache_ = new List<AnimationChain>();
                }
                return steps_cache_;
            } set { steps_cache_ = value; }
        }
        [ProtoMember(4)]
        public int ActiveStage = 0;

        [ProtoMember(5)]
        public int SelectedStep = 0;

        public static readonly Guid modStorageId = Guid.Parse("16f75c87-ba3d-4125-85e6-86620aea4b93");
        public const string INI_SEC_NAME = "AngledLCDs Save Data";
        private static MyIniKey AzimuthKey(string sec) => new MyIniKey(sec, "azimuth");
        private static MyIniKey PitchKey(string sec) => new MyIniKey(sec, "pitch");
        private static MyIniKey RollKey(string sec) => new MyIniKey(sec, "roll");
        private static MyIniKey OffsetKey(string sec) => new MyIniKey(sec, "offset");
        private static MyIniKey NameKey(string sec) => new MyIniKey(sec, "name");
        private static string CreateStageSectName(int num) => AnimationStage.SectionName + ":" + num.ToString();

        public static LCDSettings LoadFrom(MyModStorageComponentBase store)
        {
            if (store == null)
            {
                throw new ArgumentException("tried to load from null store");
            }
            var binary = Convert.FromBase64String(store[modStorageId]);
            return MyAPIGateway.Utilities.SerializeFromBinary<LCDSettings>(binary);
        }

        public void SaveTo(IMyFunctionalBlock b)
        {
            var store = b.Storage;
            if (store == null)
            {
                store = new MyModStorageComponent();
                b.Storage = store;
            }
            var binary = MyAPIGateway.Utilities.SerializeToBinary(this);
            store[modStorageId] = Convert.ToBase64String(binary);

        }
        public void SaveTo(MyIni store)
        {
            var n = 0;
            foreach (var stage in Stages)
            {
                var sect = CreateStageSectName(n);
                store.AddSection(sect);
                store.Set(AzimuthKey(sect), stage.AzimuthDegs);
                store.Set(PitchKey(sect), stage.PitchDegs);
                store.Set(RollKey(sect), stage.RollDegs);
                store.Set(OffsetKey(sect), stage.Offset.ToString());
                store.Set(NameKey(sect), stage.Name);
                ++n;
            }
            store.AddSection(INI_SEC_NAME);
        }
        public static List<string> ReloadSectionsCache(MyIni store, List<string> cache)
        {
            cache.Clear();
            cache.Add(INI_SEC_NAME);
            store.GetSections(cache);
            cache.RemoveAll(s => s != INI_SEC_NAME && !s.StartsWith(AnimationStage.SectionName));
            return cache;
        }
        public static LCDSettings LoadFrom(MyIni store, List<string> sections_cache)
        {
            sections_cache = ReloadSectionsCache(store, sections_cache);
            var settings = new LCDSettings();
            if (sections_cache.Count > 1)
            {
                sections_cache.Remove(INI_SEC_NAME);
            }
            foreach (var section in sections_cache)
            {
                var animation = new AnimationStage
                {
                    AzimuthDegs = (float)store.Get(AzimuthKey(section)).ToDouble(),
                    PitchDegs = (float)store.Get(PitchKey(section)).ToDouble(),
                    RollDegs = (float)store.Get(RollKey(section)).ToDouble(),
                    Name = store.Get(NameKey(section)).ToString(),
                };

                Vector3D offset;
                Vector3D.TryParse(store.Get(OffsetKey(section)).ToString(), out offset);
                animation.Offset = offset;
                settings.Stages.Add(animation);
            }
            return settings;
        }
    }
}
