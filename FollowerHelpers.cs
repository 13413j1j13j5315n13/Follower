using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace Follower
{
    public class FollowerHelpers
    {
        public static float EntityDistance(Entity entity, Entity player)
        {
            var component = entity?.GetComponent<Render>();

            if (component == null)
                return 9999999f;

            var objectPosition = component.Pos;

            return Vector3.Distance(objectPosition, player.GetComponent<Render>().Pos);
        }

        public static Entity GetLeaderEntity(TextNode leaderNode, ICollection<Entity> entities)
        {
            if (leaderNode == null || leaderNode.Value == "")
            {
                return null;
            }

            IEnumerable<Entity> players = entities.Where(x => x.Type == EntityType.Player);
            return players.First(x => x.GetComponent<Player>().PlayerName == leaderNode.Value);
        }
    }
}