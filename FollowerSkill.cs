using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;
using SharpDX;

namespace Follower
{
    public class FollowerSkill
    {
        public FollowerSkill(bool enable, Keys hotkey, bool useAgainstMonsters, int cooldown, int priority,
            int position, int range)
        {
            Enable = enable;
            Hotkey = hotkey;
            UseAgainstMonsters = useAgainstMonsters;
            Cooldown = cooldown;
            Priority = priority;
            Position = position;
            Range = range;
            
        }

        [JsonProperty("enable_skill")] public bool Enable { get; set; }

        [JsonProperty("hotkey")]
        public Keys Hotkey { get; set; }

        [JsonProperty("use_against_monsters")] public bool UseAgainstMonsters { get; set; }

        [JsonProperty("cooldown")] public int Cooldown { get; set; }

        [JsonProperty("priority")] public int Priority { get; set; }

        [JsonProperty("position")] public int Position { get; set; }

        [JsonProperty("range")] public int Range { get; set; }
    }
}