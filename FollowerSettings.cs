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
        public FollowerSettings()
        {
            Enable = new ToggleNode(false);
            FollowerActivateKey = Keys.F2;
            FontHeight = new RangeNode<int>(20, 10, 70);
            ExtraDelay = new RangeNode<int>(0, 0, 200);
            MouseSpeed = new RangeNode<float>(1, 0, 30);
            LeaderName = new TextNode("");

            UseMovementSkill = new ToggleNode(false);
            MovementSkillKey = new HotkeyNode(Keys.Q);
            MovementSkillCooldownMilliseconds = new RangeNode<int>(3500, 0, 10000);

            EnableNetworkActivity = new ToggleNode(true);
            ActivityColor = Color.Red; // Leading, Following, No action
            FollowerTextColor = Color.Green;
            LeaderTextColor = Color.Red;
            FollowerModeToggleFollower = new ToggleNode(false);
            FollowerModeToggleLeader = new ToggleNode(false);
            NetworkActivityUrl = new TextNode("");
            NetworkActivityServerPort = new RangeNode<int>(4412, 3000, 6000);
            NetworkActivityPropagateLeaderName = new TextNode("");
            NetworkActivityPropagateUseMovementSkill = new ToggleNode(false);
            NetworkActivityPropagateMovementSkillKey = Keys.Q;
            NetworkActivityPropagateWorking = new ToggleNode(false);
            NetworkActivityActivateKey = Keys.F3;
            NetworkActivityPropagateWorkingChangeKey = Keys.F4;
            NetworkActivityPropagateAggressivenessModeChangeKey = Keys.F5;

            SkillOne = new SkillSettings(1);
            SkillTwo = new SkillSettings(2);
            SkillThree = new SkillSettings(3);

            PropagateFollowerAggressiveness = FollowerAggressiveness.Disabled;
        }

        public TextNode LeaderName { get; set; }
        public ToggleNode Enable { get; set; }
        public RangeNode<int> FontHeight { get; set; }
        public HotkeyNode FollowerActivateKey { get; set; }
        public RangeNode<int> ExtraDelay { get; set; }
        public RangeNode<float> MouseSpeed { get; set; }

        [Menu("Use Movement Skill", 1)] public ToggleNode UseMovementSkill { get; set; }

        [Menu("Movement Skill Key", 11, 1)] public HotkeyNode MovementSkillKey { get; set; }

        [Menu("Movement Skill Cooldown", "In Milliseconds", 12, 1)]
        public RangeNode<int> MovementSkillCooldownMilliseconds { get; set; }

        [Menu("Enable Network Communication", 2)]
        public ToggleNode EnableNetworkActivity { get; set; }

        public ColorNode ActivityColor { get; set; }

        [Menu("Sets as Follower", 21, 2)] public ToggleNode FollowerModeToggleFollower { get; set; }
        [Menu("Follower Text Color", 22, 2)] public ColorNode FollowerTextColor { get; set; }

        [Menu("Sets as Leader", 23, 2)] public ToggleNode FollowerModeToggleLeader { get; set; }
        [Menu("Leader Text Color", 24, 2)] public ColorNode LeaderTextColor { get; set; }

        [Menu("Activate Network Activity", 25, 2)]
        public HotkeyNode NetworkActivityActivateKey { get; set; }

        [Menu("URL", 26, 2)] public TextNode NetworkActivityUrl { get; set; }
        [Menu("Server Port", 27, 2)] public RangeNode<int> NetworkActivityServerPort { get; set; }

        [Menu("Leader Name to propagate", 28, 2)]
        public TextNode NetworkActivityPropagateLeaderName { get; set; }

        [Menu("Propagate usage of the movement skill", 29, 2)]
        public ToggleNode NetworkActivityPropagateUseMovementSkill { get; set; }

        [Menu("Propagated movement skill key", 210, 2)]
        public HotkeyNode NetworkActivityPropagateMovementSkillKey { get; set; }

        [Menu("Propagated working of followers", 211, 2)]
        public ToggleNode NetworkActivityPropagateWorking { get; set; }

        [Menu("Hotkey to stop/start followers", 212, 2)]
        public HotkeyNode NetworkActivityPropagateWorkingChangeKey { get; set; }

        [Menu("Change aggressiveness of followers", 213, 2)]
        public HotkeyNode NetworkActivityPropagateAggressivenessModeChangeKey { get; set; }

        [Menu("Slave Skill 1", 3)] public SkillSettings SkillOne { get; set; }
        [Menu("Slave Skill 2", 4)] public SkillSettings SkillTwo { get; set; }
        [Menu("Slave Skill 3", 5)] public SkillSettings SkillThree { get; set; }

        public FollowerAggressiveness PropagateFollowerAggressiveness { get; set; }
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