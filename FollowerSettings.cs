using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;

namespace Follower
{
    public class FollowerSettings : ISettings
    {
        public FollowerSettings()
        {
            Enable = new ToggleNode(false);
            FollowerActivateKey = Keys.F2;
            ExtraDelay = new RangeNode<int>(0, 0, 200);
            MouseSpeed = new RangeNode<float>(1, 0, 30);
            LeaderName = new TextNode("");

            FlameDash = new ToggleNode(false);
            FlameDashKey = new HotkeyNode(Keys.Q);
            FlameDashCooldownMilliseconds = new RangeNode<int>(3500, 0, 10000);


            //PickupRange = new RangeNode<int>(600, 1, 1000);
            //ChestRange = new RangeNode<int>(500, 1, 1000);
            //GroundChests = new ToggleNode(false);
            //PickUpEverything = new ToggleNode(false);
            //LeftClickToggleNode = new ToggleNode(true);
            //OverrideItemPickup = new ToggleNode(false);
        }

        public TextNode LeaderName { get; set; }
        public ToggleNode Enable { get; set; }
        public HotkeyNode FollowerActivateKey { get; set; }
        public RangeNode<int> ExtraDelay { get; set; }
        public RangeNode<float> MouseSpeed { get; set; }

        [Menu("Flame Dash", 5)] public ToggleNode FlameDash { get; set; }

        [Menu("Flame Dash Key", 51, 5)] public HotkeyNode FlameDashKey { get; set; }

        [Menu("Flame Dash cooldown in milliseconds (default 3.5 seconds)", "", 52, 5)]
        public RangeNode<int> FlameDashCooldownMilliseconds { get; set; }

        //public RangeNode<int> PickupRange { get; set; }
        //public RangeNode<int> ChestRange { get; set; }
        //public EmptyNode AllOverridEmptyNode { get; set; }
        //public ToggleNode PickUpEverything { get; set; }
        //public ToggleNode GroundChests { get; set; }
        //public ToggleNode LeftClickToggleNode { get; set; }
        //public ToggleNode OverrideItemPickup { get; set; }
        //public ToggleNode ReturnMouseToBeforeClickPosition { get; set; } = new ToggleNode(true);
        //public RangeNode<int> TimeBeforeNewClick { get; set; } = new RangeNode<int>(500, 0, 1500);
    }
}
