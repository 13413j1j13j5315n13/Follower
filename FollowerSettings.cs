using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace Follower
{
    public class FollowerSettings : ISettings
    {
        public FollowerSettings()
        {
            Enable = new ToggleNode(false);
            PickUpKey = Keys.F1;
            PickupRange = new RangeNode<int>(600, 1, 1000);
            ChestRange = new RangeNode<int>(500, 1, 1000);
            ExtraDelay = new RangeNode<int>(0, 0, 200);
            GroundChests = new ToggleNode(false);
            PickUpEverything = new ToggleNode(false);
            LeftClickToggleNode = new ToggleNode(true);
            OverrideItemPickup = new ToggleNode(false);
            MouseSpeed = new RangeNode<float>(1, 0, 30);
            LeaderName = new TextNode();
        }

        public TextNode LeaderName { get; set; }

        public ToggleNode Enable { get; set; }
        public HotkeyNode PickUpKey { get; set; }
        public RangeNode<int> PickupRange { get; set; }
        public RangeNode<int> ChestRange { get; set; }
        public RangeNode<int> ExtraDelay { get; set; }
        public EmptyNode AllOverridEmptyNode { get; set; }
        public ToggleNode PickUpEverything { get; set; }
        public ToggleNode GroundChests { get; set; }
        public ToggleNode LeftClickToggleNode { get; set; }
        public ToggleNode OverrideItemPickup { get; set; }
        public RangeNode<float> MouseSpeed { get; set; }
        public ToggleNode ReturnMouseToBeforeClickPosition { get; set; } = new ToggleNode(true);
        public RangeNode<int> TimeBeforeNewClick { get; set; } = new RangeNode<int>(500, 0, 1500);
    }
}
