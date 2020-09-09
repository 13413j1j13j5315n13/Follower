using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;
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
            NetworkActivityActivateKey = Keys.F3;
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

        [Menu("Sets as Follower", 21, 2)]
        public ToggleNode FollowerModeToggleFollower { get; set; }
        [Menu("Follower Text Color", 22, 2)]
        public ColorNode FollowerTextColor { get; set; }

        [Menu("Sets as Leader", 23, 2)]
        public ToggleNode FollowerModeToggleLeader { get; set; }
        [Menu("Leader Text Color", 24, 2)]
        public ColorNode LeaderTextColor { get; set; }
        public HotkeyNode NetworkActivityActivateKey { get; set; }
    }

    public enum FollowerType
    {
        Follower,
        Leader,
        Disabled
    }
}
