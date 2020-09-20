using System;
using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;
using Newtonsoft.Json;
using ExileCore;
using SharpDX;
using Color = SharpDX.Color;

namespace Follower
{
    public class FollowerSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        public TextNode LeaderName { get; set; } = new TextNode("");
        public HotkeyNode FollowerActivateKey { get; set; } = Keys.F2;
        public RangeNode<int> ExtraDelay { get; set; } = new RangeNode<int>(0, 0, 200);
        public RangeNode<float> MouseSpeed { get; set; } = new RangeNode<float>(1, 0, 30);

        [Menu("Use Movement Skill", 1)] public ToggleNode UseMovementSkill { get; set; } = new ToggleNode(false);

        [Menu("Movement Skill Key", 11, 1)] public HotkeyNode MovementSkillKey { get; set; } = new HotkeyNode(Keys.Q);

        [Menu("Movement Skill Cooldown", "In Milliseconds", 12, 1)]
        public RangeNode<int> MovementSkillCooldownMilliseconds { get; set; } = new RangeNode<int>(3500, 0, 10000);

        [Menu("Enable Network Communication", 2)]
        public ToggleNode EnableNetworkActivity { get; set; } = new ToggleNode(true);

        public ColorNode ActivityColor { get; set; } = Color.Red;

        [Menu("Sets as Follower", 21, 2)] public ToggleNode FollowerModeToggleFollower { get; set; } = new ToggleNode(false);
        [Menu("Follower Text Color", 22, 2)] public ColorNode FollowerTextColor { get; set; } = Color.Green;

        [Menu("Sets as Leader", 23, 2)] public ToggleNode FollowerModeToggleLeader { get; set; } = new ToggleNode(false);
        [Menu("Leader Text Color", 24, 2)] public ColorNode LeaderTextColor { get; set; } = Color.Red;

        [Menu("Activate Network Activity", 25, 2)]
        public HotkeyNode NetworkActivityActivateKey { get; set; } = Keys.F3;

        [Menu("URL", 26, 2)] public TextNode NetworkActivityUrl { get; set; } = new TextNode("");
        [Menu("Server Port", 27, 2)] public RangeNode<int> NetworkActivityServerPort { get; set; } = new RangeNode<int>(4412, 3000, 6000);

        [Menu("Leader Name to propagate", 28, 2)]
        public TextNode NetworkActivityPropagateLeaderName { get; set; } = new TextNode("");

        [Menu("Propagate usage of the movement skill", 29, 2)]
        public ToggleNode NetworkActivityPropagateUseMovementSkill { get; set; } = new ToggleNode(false);

        [Menu("Propagated movement skill key", 210, 2)]
        public HotkeyNode NetworkActivityPropagateMovementSkillKey { get; set; } = Keys.Q;

        [Menu("Propagated working of followers", 211, 2)]
        public ToggleNode NetworkActivityPropagateWorking { get; set; } = new ToggleNode(false);

        [Menu("Hotkey to stop/start followers", 212, 2)]
        public HotkeyNode NetworkActivityPropagateWorkingChangeKey { get; set; } = Keys.F4;

        [Menu("Change aggressiveness of followers", 213, 2)]
        public HotkeyNode NetworkActivityPropagateAggressivenessModeChangeKey { get; set; } = Keys.F5;

        [Menu("Enter instance command", 214, 2)]
        public HotkeyNode NetworkActivityPropagateEnterInstanceKey { get; set; } = Keys.F6;

        [Menu("Slave Skill 1", 3)] public SkillSettings SkillOne { get; set; } = new SkillSettings(1);
        [Menu("Slave Skill 2", 4)] public SkillSettings SkillTwo { get; set; } = new SkillSettings(2);
        [Menu("Slave Skill 3", 5)] public SkillSettings SkillThree { get; set; } = new SkillSettings(3);

        public FollowerAggressiveness PropagateFollowerAggressiveness { get; set; } = FollowerAggressiveness.Disabled;

        public long PropagateEnterInstance =
            ((DateTimeOffset)new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
    }

    public class SkillSettings : ISettings
    {
        public SkillSettings(int position)
        {
            Enable = new ToggleNode(false);
            SkillHotkey = Keys.W;
            UseAgainstMonsters = new ToggleNode(true);
            CooldownBetweenCasts = new TextNode(200.ToString());
            Priority = new RangeNode<int>(1, 1, 10);
            LastTimeUsed = DateTime.UtcNow;
            Position = new RangeNode<int>(position, 1, 3);
            Range = new RangeNode<int>(30, 1, 200);
        }

        [Menu("Enable this skill")] public ToggleNode Enable { get; set; }

        [Menu("Hotkey for this skill")] public HotkeyNode SkillHotkey { get; set; }

        [Menu("Use this skill against monsters")]
        public ToggleNode UseAgainstMonsters { get; set; }

        [Menu("Cooldown in MILLISECONDS",
            "Cooldown between casts in MILLISECONDS. Will break if you pass anything except the number")]
        public TextNode CooldownBetweenCasts { get; set; }

        public RangeNode<int> Priority { get; set; }
        public RangeNode<int> Position { get; set; }
        public RangeNode<int> Range { get; set; }

        public DateTime LastTimeUsed { get; set; }
    }
}