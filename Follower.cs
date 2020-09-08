using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Input = ExileCore.Input;

namespace Follower
{
    public class Follower : BaseSettingsPlugin<FollowerSettings>
    {
        private readonly List<Entity> _entities = new List<Entity>();
        private readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();
        private readonly Stopwatch _debugTimer = Stopwatch.StartNew();

        private Vector2 _clickWindowOffset;
        private WaitTime _workCoroutine;
        private uint _coroutineCounter;
        private bool _fullWork = true;
        private Coroutine _followerCoroutine;

        private WaitTime ToPick => new WaitTime(5);
        private WaitTime Wait3ms => new WaitTime(3);
        private WaitTime WaitForNextTry => new WaitTime(5);

        private WaitTime WaitBeforeClicking => new WaitTime(5000);

        public override bool Initialise()
        {
            _followerCoroutine = new Coroutine(MainWorkCoroutine(), this, "Follower");
            Core.ParallelRunner.Run(_followerCoroutine);
            _followerCoroutine.Pause();
            _debugTimer.Reset();
            Settings.MouseSpeed.OnValueChanged += (sender, f) => { Mouse.speedMouse = Settings.MouseSpeed.Value; };
            _workCoroutine = new WaitTime(Settings.ExtraDelay);
            Settings.ExtraDelay.OnValueChanged += (sender, i) => _workCoroutine = new WaitTime(i);
            return true;
        }

        private IEnumerator MainWorkCoroutine()
        {
            while (true)
            {
                //yield return FindItemToPick();
                yield return FollowPlayer();

                _coroutineCounter++;
                _followerCoroutine.UpdateTicks(_coroutineCounter);
                yield return _workCoroutine;
            }
        }


        public override Job Tick()
        {
            if (Input.GetKeyState(Keys.Escape)) _followerCoroutine.Pause();

            if (Input.GetKeyState(Settings.PickUpKey.Value))
            {
                _debugTimer.Restart();

                if (_followerCoroutine.IsDone)
                {
                    var firstOrDefault = Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(Follower));

                    if (firstOrDefault != null)
                        _followerCoroutine = firstOrDefault;
                }

                _followerCoroutine.Resume();
                _fullWork = false;
            }
            else
            {
                if (_fullWork)
                {
                    _followerCoroutine.Pause();
                    _debugTimer.Reset();
                }
            }

            if (_debugTimer.ElapsedMilliseconds > 2000)
            {
                _fullWork = true;
                LogMessage("Error pick it stop after time limit 2000 ms", 1);
                _debugTimer.Reset();
            }

            return null;
        }

        private IEnumerable FollowPlayer()
        {
            if (Settings.LeaderName.Value == "")
                yield break;

            IEnumerable<Entity> players  = GameController.Entities.Where(x => x.Type == EntityType.Player);
            Entity leaderPlayer = SelectLeaderPlayer(players);

            if (leaderPlayer == null)
                yield break;

            yield return TryToClickOnLeader(leaderPlayer);
        }

        private Entity SelectLeaderPlayer(IEnumerable<Entity> players)
        {
            string leaderName = Settings.LeaderName.Value;
            return players.First(x => x.GetComponent<Player>().PlayerName == leaderName);
        }

        private IEnumerator TryToClickOnLeader(Entity leaderPlayer)
        {
            //var testi = leaderPlayer.;
            //var centerOfItemLabel = pickItItem.LabelOnGround.Label.GetClientRectCache.Center;
            //var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;
            //var oldMousePosition = Mouse.GetCursorPositionVector();
            //_clickWindowOffset = rectangleOfGameWindow.TopLeft;
            //rectangleOfGameWindow.Inflate(-55, -55);
            //centerOfItemLabel.X += rectangleOfGameWindow.Left;
            //centerOfItemLabel.Y += rectangleOfGameWindow.Top;

            var worldCoords = leaderPlayer.Pos;
            Camera camera = GameController.Game.IngameState.Camera;
            var mobScreenCoords = camera.WorldToScreen(worldCoords);

            Mouse.MoveCursorToPosition(mobScreenCoords);

            yield break;
        }





        /**
             *    OLD CODE FROM HERE TO BE DELETED
             */

            public bool DoWePickThis(CustomItem itemEntity)
        {
            if (!itemEntity.IsValid)
                return false;

            return true;
        }

        private IEnumerator FindItemToPick()
        {
            if (!Input.GetKeyState(Settings.PickUpKey.Value) || !GameController.Window.IsForeground()) yield break;
            var window = GameController.Window.GetWindowRectangleTimeCache;
            var rect = new RectangleF(0, 0, window.Width, window.Height);
            var playerPos = GameController.Player.GridPos;

            /*            var currentLabelsDebugging = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.ToList();
                        currentLabelsDebugging = currentLabelsDebugging.Where(x => x.Address != 0).ToList();
                        currentLabelsDebugging = currentLabelsDebugging.Where(x => x.ItemOnGround?.Path != null).ToList();
                        currentLabelsDebugging = currentLabelsDebugging.Where(x => x.IsVisible).ToList();
                        currentLabelsDebugging = currentLabelsDebugging.Where(x => x.Label.GetClientRectCache.Center.PointInRectangle(rect)).ToList();
                        currentLabelsDebugging = currentLabelsDebugging.Where(x => x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds <= 0).ToList();*/

            List<CustomItem> currentLabels;
            currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                .Where(x => x.Address != 0 &&
                            x.ItemOnGround?.Path != null &&
                            x.IsVisible && x.Label.GetClientRectCache.Center.PointInRectangle(rect) &&
                            (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds <= 0))
                .Select(x => new CustomItem(x, GameController.Files, x.ItemOnGround.DistancePlayer))
                .OrderBy(x => x.Distance).ToList();


            GameController.Debug["PickIt"] = currentLabels;
            var pickUpThisItem = currentLabels.FirstOrDefault(x => DoWePickThis(x) && x.Distance < Settings.PickupRange);
            if (pickUpThisItem?.GroundItem != null) yield return TryToPickV2(pickUpThisItem);
            _fullWork = true;
        }

        private IEnumerator TryToPickV2(CustomItem pickItItem)
        {
            if (!pickItItem.IsValid)
            {
                _fullWork = true;
                LogMessage("PickItem is not valid.", 5, Color.Red);
                yield break;
            }

            var centerOfItemLabel = pickItItem.LabelOnGround.Label.GetClientRectCache.Center;
            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;
            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-55, -55);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;

            if (!rectangleOfGameWindow.Intersects(new RectangleF(centerOfItemLabel.X, centerOfItemLabel.Y, 3, 3)))
            {
                _fullWork = true;
                //LogMessage($"Label outside game window. Label: {centerOfItemLabel} Window: {rectangleOfGameWindow}", 5, Color.Red);
                yield break;
            }

            var tryCount = 0;

            while (!pickItItem.IsTargeted() && tryCount < 5)
            {
                var completeItemLabel = pickItItem.LabelOnGround?.Label;

                if (completeItemLabel == null)
                {
                    if (tryCount > 0)
                    {
                        LogMessage("Probably item already picked.", 3);
                        yield break;
                    }

                    LogError("Label for item not found.", 5);
                    yield break;
                }

                /*while (GameController.Player.GetComponent<Actor>().isMoving)
                {
                    yield return waitPlayerMove;
                }*/
                var clientRect = completeItemLabel.GetClientRect();

                var clientRectCenter = clientRect.Center;

                var vector2 = clientRectCenter + _clickWindowOffset;

                Mouse.MoveCursorToPosition(vector2);
                yield return Wait3ms;
                Mouse.MoveCursorToPosition(vector2);
                yield return Wait3ms;
                yield return Mouse.LeftClick();
                yield return ToPick;
                tryCount++;
            }

            if (pickItItem.IsTargeted())
                Input.Click(MouseButtons.Left);

            tryCount = 0;

            while (GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.FirstOrDefault(
                       x => x.Address == pickItItem.LabelOnGround.Address) != null && tryCount < 6)
            {
                tryCount++;
                yield return WaitForNextTry;
            }
        }
    }
}
