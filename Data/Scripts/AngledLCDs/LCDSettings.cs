using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;
using System.Linq;

namespace Natomic.AngledLCDs
{
    class LCDSettings
    {
        public List<AnimationStage> Stages;


        public static readonly Guid modStorageId = Guid.Parse("16f75c87-ba3d-4125-85e6-86620aea4b93");
        public const string INI_SEC_NAME = "AngledLCDs Save Data";
        private static MyIniKey AzimuthKey(string sec) => new MyIniKey(sec, "azimuth");
        private static MyIniKey PitchKey(string sec) => new MyIniKey(sec, "pitch");
        private static MyIniKey RollKey(string sec) => new MyIniKey(sec, "roll");
        private static MyIniKey OffsetKey(string sec) => new MyIniKey(sec, "offset");
        private static MyIniKey TimecodeKey(string sec) => new MyIniKey(sec, "timecode");
        private static string CreateStageSectName(int num) => AnimationStage.SectionName + ":" + num.ToString();

        public static LCDSettings LoadFrom(MyModStorageComponentBase store)
        {
            if (store == null)
            {
                throw new InvalidBranchException("tried to load from null store");
            }
            var binary = Convert.FromBase64String(store[modStorageId]);
            return MyAPIGateway.Utilities.SerializeFromBinary<LCDSettings>(binary);
        }

        public void SaveTo(MyModStorageComponentBase store)
        {
            if (store == null)
            {
                store = new MyModStorageComponent();
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
                store.Set(TimecodeKey(sect), stage.Timecode);
                ++n;
            }
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
            foreach (var section in sections_cache)
            {
                var animation = new AnimationStage
                {
                    AzimuthDegs = (float)store.Get(AzimuthKey(section)).ToDouble(),
                    PitchDegs = (float)store.Get(PitchKey(section)).ToDouble(),
                    RollDegs = (float)store.Get(RollKey(section)).ToDouble(),
                    Timecode =store.Get(TimecodeKey(section)).ToUInt32(),
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
