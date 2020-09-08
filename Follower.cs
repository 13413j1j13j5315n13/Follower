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
        private readonly Stopwatch _debugTimer = Stopwatch.StartNew();

        private WaitTime _workCoroutine;
        private uint _coroutineCounter;
        private bool _fullWork = true;
        private Coroutine _followerCoroutine;

        private WaitTime Wait3ms => new WaitTime(3);

        private WaitTime Wait10ms => new WaitTime(10);

        private Random _rand;
        private DateTime _currentTime = DateTime.UtcNow;
        private DateTime _lastTimeCastFlameDash;

        public override bool Initialise()
        {
            LogMessage("****** Initialise started", 1);

            _rand = new Random();
            _lastTimeCastFlameDash = DateTime.UtcNow;

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
            LogMessage("****** MainWorkCoroutine started", 1);
            while (true)
            {
                yield return DoWork();

                _coroutineCounter++;
                _followerCoroutine.UpdateTicks(_coroutineCounter);

                yield return _workCoroutine;
            }
        }

        public override Job Tick()
        {
            _currentTime = DateTime.UtcNow;

            if (Input.GetKeyState(Keys.Escape)) _followerCoroutine.Pause();

            if (Input.GetKeyState(Settings.FollowerActivateKey.Value))
            {
                _debugTimer.Restart();

                if (_followerCoroutine.IsDone)
                {
                    var firstOrDefault = Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(Follower));

                    LogMessage($"****** firstOrDefault: {firstOrDefault}", 1);

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

            int errorElapsedSeconds = 1000 * 30;

            if (_debugTimer.ElapsedMilliseconds > errorElapsedSeconds)
            {
                _fullWork = true;
                LogMessage("errorElapsedSeconds has been elapsed. Turning off ", 1);
                _debugTimer.Reset();
            }

            return null;
        }

        private IEnumerator DoWork()
        {
            //LogMessage(" :::::::: DoWork called", 1);

            if (Settings.LeaderName == null || Settings.LeaderName.Value == "")
            {
                //LogMessage(" :::::::: Settings.LeaderName is null or empty!", 1);
                yield break;
            }

            IEnumerable<Entity> players = GameController.Entities.Where(x => x.Type == EntityType.Player);

            string leaderName = Settings.LeaderName.Value;
            Entity leaderPlayer = SelectLeaderPlayer(players, leaderName);

            //LogMessage($" :::::::: leaderPlayer {leaderPlayer}", 1);

            if (leaderPlayer == null)
            {
                //LogMessage($" :::::::: No leader player found", 1);
                yield break;
            }

            yield return TryToClickOnLeader(leaderPlayer);
            //yield break;
        }

        private Entity SelectLeaderPlayer(IEnumerable<Entity> players, string leaderName)
        {
            return players.First(x => x.GetComponent<Player>().PlayerName == leaderName);
        }

        private IEnumerator TryToClickOnLeader(Entity leaderPlayer)
        {
            //LogMessage(" ;;;;;;;;; TryToClickOnLeader called", 1);

            var worldCoords = leaderPlayer.Pos;
            Camera camera = GameController.Game.IngameState.Camera;
            var result = camera.WorldToScreen(worldCoords);

            var windowRectangle = GameController.Window.GetWindowRectangleReal();
            var windowSize = new Size2F(windowRectangle.Width / 2560, windowRectangle.Height / 1600);

            float scaledWidth = 50 * windowSize.Width;
            float scaledHeight = 10 * windowSize.Height;

            RectangleF aaa = new RectangleF(result.X - scaledWidth / 2f, result.Y - scaledHeight / 2f, scaledWidth,
                scaledHeight);

            Vector2 finalPos = new Vector2(aaa.X, aaa.Y);

            //LogMessage("Moving and clicking mouse.", 5, Color.Red);

            if (CanCastFlameDash())
            {
                LogMessage("Casting flame dash", 1);
                yield return CastFlameDash(finalPos);
            }
            else
            {
                LogMessage("Clicking on the leader", 1);
                yield return ClickOnLeader(finalPos);
            }
        }

        private bool CanCastFlameDash()
        {
            var currentMs = ((DateTimeOffset) _currentTime).ToUnixTimeMilliseconds();
            var lastTimeMs = ((DateTimeOffset) _lastTimeCastFlameDash).ToUnixTimeMilliseconds();
            var delta = currentMs - lastTimeMs;

            return Settings.FlameDash && delta > Settings.FlameDashCooldownMilliseconds.Value;
        }

        private IEnumerator CastFlameDash(Vector2 pos)
        {
            Mouse.MoveCursorToPosition(pos);
            yield return Wait3ms;
            Mouse.MoveCursorToPosition(pos);
            yield return Wait10ms;
            yield return Input.KeyPress(Settings.FlameDashKey);

            _lastTimeCastFlameDash = _currentTime;

            yield break;
        }

        private IEnumerator ClickOnLeader(Vector2 pos)
        {
            Mouse.MoveCursorToPosition(pos);
            yield return Wait3ms;
            Mouse.MoveCursorToPosition(pos);
            yield return Wait10ms;
            yield return Mouse.LeftClick();
        }
    }
}
