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
using Newtonsoft.Json;
using Input = ExileCore.Input;
using NumericsVector2 = System.Numerics.Vector2;

namespace Follower
{
    public class Follower : BaseSettingsPlugin<FollowerSettings>
    {
        private readonly Stopwatch _debugTimer = Stopwatch.StartNew();

        private WaitTime _workCoroutine;
        private uint _coroutineCounter;
        private bool _fullWork = true;
        private bool _followerIsWorking = false;
        private Coroutine _followerCoroutine;
        private Coroutine _networkActivityCoroutine;

        private RectangleF _windowRectangle;
        private Size2F _windowSize;

        private FollowerType _currentFollowerMode = FollowerType.Disabled;

        private WaitTime Wait3ms => new WaitTime(3);

        private WaitTime Wait10ms => new WaitTime(10);

        private DateTime _currentTime = DateTime.UtcNow;
        private DateTime _lastTimeMovementSkillUsed;

        public override bool Initialise()
        {
            LogMessage("****** Initialise started", 1);

            _windowRectangle = GameController.Window.GetWindowRectangleReal();
            _windowSize = new Size2F(_windowRectangle.Width / 2560, _windowRectangle.Height / 1600);
            _lastTimeMovementSkillUsed = DateTime.UtcNow;

            GameController.LeftPanel.WantUse(() => true);

            _followerCoroutine = new Coroutine(MainWorkCoroutine(), this, "Follower");
            _networkActivityCoroutine = new Coroutine(MainNetworkActivityCoroutine(), this, "Follower Network Activity");

            Core.ParallelRunner.Run(_followerCoroutine);
            Core.ParallelRunner.Run(_networkActivityCoroutine);

            _followerCoroutine.Pause();
            _networkActivityCoroutine.Pause();

            _debugTimer.Reset();

            Settings.MouseSpeed.OnValueChanged += (sender, f) => { Mouse.speedMouse = Settings.MouseSpeed.Value; };
            _workCoroutine = new WaitTime(Settings.ExtraDelay);
            Settings.ExtraDelay.OnValueChanged += (sender, i) => _workCoroutine = new WaitTime(i);

            return true;
        }

        public override void Render()
        {
            //LogMessage("****** Render called", 1);

            int fontHeight = 40;

            var startDrawPoint = GameController.LeftPanel.StartDrawPoint;

            var (workingText, workingColor) = GetWorkingTextAndColor();
            NumericsVector2 firstLine = Graphics.DrawText(workingText, startDrawPoint, workingColor, fontHeight, FontAlign.Right);
            startDrawPoint.Y += firstLine.Y;

            if (Settings.EnableNetworkActivity.Value)
            {
                var (text, color) = GetFollowerModeTextAndColor();
                NumericsVector2 lastLine = Graphics.DrawText(text, startDrawPoint, color, fontHeight, FontAlign.Right);

                startDrawPoint.Y += lastLine.Y;
            }
        }

        private (string, Color) GetWorkingTextAndColor()
        {
            if (_followerIsWorking)
            {
                return ("Following is working", Color.Red);
            }
            return ("Following disabled", Color.Yellow);
        }

        private (string, Color) GetFollowerModeTextAndColor()
        {
            string prefix = "Follower Mode: ";
            string text = prefix + "Disabled";
            Color color = Color.Yellow;

            if (_currentFollowerMode == FollowerType.Follower)
            {
                text = prefix + "Follower";
                color = Settings.FollowerTextColor.Value;
            } else if (_currentFollowerMode == FollowerType.Leader)
            {
                text = prefix + "Leader";
                color = Settings.LeaderTextColor.Value;
            }

            return (text, color);
        }

        private IEnumerator MainNetworkActivityCoroutine()
        {
            LogMessage("****** MainNetworkActivityCoroutine started", 1);
            while (true)
            {
                LogMessage("****** Inside MainNetworkActivityCoroutine while true", 1);
                yield return DoNetworkActivityWork();
            }
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
            SetFollowerModeValues();
            _currentTime = DateTime.UtcNow;

            if (Input.GetKeyState(Keys.Escape))
            {
                _followerIsWorking = false;
                _followerCoroutine.Pause();
                _networkActivityCoroutine.Pause();
            };

            if (Input.GetKeyState(Settings.NetworkActivityActivateKey.Value))
            {
                _networkActivityCoroutine.Resume();
            }

            if (Input.GetKeyState(Settings.FollowerActivateKey.Value))
            {
                _debugTimer.Restart();
                _followerIsWorking = true;

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
            //else
            //{
            //    if (_fullWork)
            //    {
            //        _followerCoroutine.Pause();
            //        _debugTimer.Reset();
            //    }
            //}

            return null;
        }

        private void SetFollowerModeValues()
        {
            if (Settings.FollowerModeToggleFollower.Value)
            {
                _currentFollowerMode = FollowerType.Follower;
            }
            else if (Settings.FollowerModeToggleLeader.Value)
            {
                _currentFollowerMode = FollowerType.Leader;
            }
            else
            {
                _currentFollowerMode = FollowerType.Disabled;
            }
        }

        private IEnumerator DoNetworkActivityWork()
        {
            LogMessage(" :::::::: DoNetworkActivityWork called", 1);
            yield break;
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

            float scaledWidth = 50 * _windowSize.Width;
            float scaledHeight = 10 * _windowSize.Height;

            RectangleF aaa = new RectangleF(result.X - scaledWidth / 2f, result.Y - scaledHeight / 2f, scaledWidth,
                scaledHeight);

            Vector2 finalPos = new Vector2(aaa.X, aaa.Y);

            //LogMessage("Moving and clicking mouse.", 5, Color.Red);

            Mouse.MoveCursorToPosition(finalPos);
            yield return Wait3ms;
            Mouse.MoveCursorToPosition(finalPos);
            yield return Wait10ms;
            yield return CanUseMovementSkill() ? Input.KeyPress(Settings.MovementSkillKey) : Mouse.LeftClick();
        }

        private bool CanUseMovementSkill()
        {
            var currentMs = ((DateTimeOffset) _currentTime).ToUnixTimeMilliseconds();
            var lastTimeMs = ((DateTimeOffset) _lastTimeMovementSkillUsed).ToUnixTimeMilliseconds();
            var delta = currentMs - lastTimeMs;

            return Settings.UseMovementSkill && delta > Settings.MovementSkillCooldownMilliseconds.Value;
        }
    }
}
