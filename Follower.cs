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
using System.Net;
using System.Net.Http;
using SystemEncoding = System.Text.Encoding;
using Newtonsoft.Json;
using Input = ExileCore.Input;
using NumericsVector2 = System.Numerics.Vector2;

namespace Follower
{
    public class Follower : BaseSettingsPlugin<FollowerSettings>
    {
        private readonly Stopwatch _debugTimer = Stopwatch.StartNew();

        private static readonly HttpClient HttpClient = new HttpClient();
        HttpListener _httpListener = new HttpListener();

        private WaitTime _workCoroutine;

        private uint _coroutineCounter;

        //private bool _fullWork = true;
        private bool _followerShouldWork = false;
        private bool _networkCoroutineShouldWork = false;

        private Coroutine _followerCoroutine;
        private Coroutine _networkActivityCoroutine;
        private Coroutine _serverCoroutine;

        private bool _networkRequestFinished = true;
        //private bool _coroutinesFirstTime = true;


        private RectangleF _windowRectangle;
        private Size2F _windowSize;

        private NetworkHelper _networkHelper;

        private FollowerType _currentFollowerMode = FollowerType.Disabled;
        private FollowerAggressiveness _followerAggressivenessMode = FollowerAggressiveness.Disabled;
        private WaitTime Wait3ms => new WaitTime(3);

        private WaitTime Wait10ms => new WaitTime(10);
        //private WaitTime Wait3seconds => new WaitTime(3 * 1000);

        private DateTime _currentTime = DateTime.UtcNow;
        private DateTime _lastTimeMovementSkillUsed;
        private DateTime _lastTimeNetworkActivityPropagateWorkingChanged;
        private DateTime _lastTimeAggressivenessModePropagated;

        public override bool Initialise()
        {
            LogMessage("****** Initialise started", 1);

            _networkHelper = new NetworkHelper(Settings);

            _windowRectangle = GameController.Window.GetWindowRectangleReal();
            _windowSize = new Size2F(_windowRectangle.Width / 2560, _windowRectangle.Height / 1600);
            _lastTimeMovementSkillUsed = DateTime.UtcNow;
            _lastTimeNetworkActivityPropagateWorkingChanged = DateTime.UtcNow;
            _lastTimeAggressivenessModePropagated = DateTime.UtcNow;

            GameController.LeftPanel.WantUse(() => true);

            _serverCoroutine = new Coroutine(MainServerCoroutine(), this, "Follower Server");
            _followerCoroutine = new Coroutine(MainFollowerCoroutine(), this, "Follower");
            _networkActivityCoroutine =
                new Coroutine(MainNetworkActivityCoroutine(), this, "Follower Network Activity");

            Core.ParallelRunner.Run(_serverCoroutine);
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

        public override void OnUnload()
        {
            LogMessage("****** OnUnload called", 1);
            _httpListener.Stop();
            _httpListener.Close();
        }

        public override void Render()
        {
            //LogMessage("****** Render called", 1);

            int fontHeight = 40;

            var startDrawPoint = GameController.LeftPanel.StartDrawPoint;

            var (workingText, workingColor) = GetWorkingTextAndColor();
            NumericsVector2 firstLine =
                Graphics.DrawText(workingText, startDrawPoint, workingColor, fontHeight, FontAlign.Right);
            startDrawPoint.Y += firstLine.Y;

            if (Settings.EnableNetworkActivity.Value)
            {
                NumericsVector2 line = Graphics.DrawText("Network Activity is enabled", startDrawPoint, Color.Yellow,
                    fontHeight, FontAlign.Right);
                startDrawPoint.Y += line.Y;

                string naText = "Network coroutine is" + (_networkActivityCoroutine.Running ? "" : " NOT") + " running";
                line = Graphics.DrawText(naText, startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
                startDrawPoint.Y += line.Y;


                if (_httpListener.IsListening)
                {
                    line = Graphics.DrawText("Server is listening", startDrawPoint, Color.Yellow, fontHeight,
                        FontAlign.Right);
                    startDrawPoint.Y += line.Y;
                }

                var (text, color) = GetFollowerModeTextAndColor();
                line = Graphics.DrawText(text, startDrawPoint, color, fontHeight, FontAlign.Right);
                startDrawPoint.Y += line.Y;

                (text, color) = GetPropagateWorkingText();
                line = Graphics.DrawText(text, startDrawPoint, color, fontHeight, FontAlign.Right);
                startDrawPoint.Y += line.Y;

                (text, color) = GetPropagatedFollowersAggressivenessModeText();
                line = Graphics.DrawText(text, startDrawPoint, color, fontHeight, FontAlign.Right);
                startDrawPoint.Y += line.Y;

                (text, color) = GetFollowerAggressivenessModeText();
                line = Graphics.DrawText(text, startDrawPoint, color, fontHeight, FontAlign.Right);
                startDrawPoint.Y += line.Y;
            }
        }

        private (string, Color) GetWorkingTextAndColor()
        {
            if (_followerShouldWork)
            {
                return ("Following is working", Color.Red);
            }

            return ("Following disabled", Color.Yellow);
        }

        private (string, Color) GetPropagateWorkingText()
        {
            string prefix = "Telling other followers to follow: ";
            if (Settings.NetworkActivityPropagateWorking.Value)
            {
                return (prefix + "True", Color.Red);
            }

            return (prefix + "False", Color.Yellow);
        }

        private (string, Color) GetPropagatedFollowersAggressivenessModeText()
        {
            string prefix = "Propagating slaves aggressiveness: ";
            if (Settings.PropagateFollowerAggressiveness == FollowerAggressiveness.Aggressive)
            {
                return (prefix + "Aggressive", Color.Red);
            }
            else if (Settings.PropagateFollowerAggressiveness == FollowerAggressiveness.Passive)
            {
                return (prefix + "Passive", Color.Green);
            }

            return (prefix + "Disabled", Color.White);
        }

        private (string, Color) GetFollowerAggressivenessModeText()
        {
            string prefix = "Follower aggressiveness: ";
            if (_followerAggressivenessMode == FollowerAggressiveness.Aggressive)
            {
                return (prefix + "Aggressive", Color.Red);
            }
            else if (_followerAggressivenessMode == FollowerAggressiveness.Passive)
            {
                return (prefix + "Passive", Color.Green);
            }

            return (prefix + "Disabled", Color.White);
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
            }
            else if (_currentFollowerMode == FollowerType.Leader)
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
                if (!_networkCoroutineShouldWork)
                {
                    yield return Wait10ms;
                    continue;
                }

                if (_currentFollowerMode == FollowerType.Follower)
                {
                    _httpListener.Stop();
                    yield return DoFollowerNetworkActivityWork();
                    yield return new WaitTime(3 * 1000);
                    ; // Always wait before making a new request
                }
                else if (_currentFollowerMode == FollowerType.Leader)
                {
                    if (!_httpListener.IsListening)
                    {
                        _httpListener.Start();
                    }
                }

                yield return Wait10ms;
            }
        }

        private IEnumerator MainServerCoroutine()
        {
            LogMessage(" :::::::: MainServerCoroutine called", 1);

            var url = $"http://localhost:{Settings.NetworkActivityServerPort.Value}/";
            _httpListener.Prefixes.Add(url);

            //_httpListener.Start();
            //_httpListener.Stop();

            while (true)
            {
                if (_httpListener.IsListening)
                {
                    //LogMessage(" :::::::: Before GetContext", 1);

                    _networkHelper.MakeAsyncListen(_httpListener);
                }

                yield return new WaitTime(50);
            }
        }

        private IEnumerator MainFollowerCoroutine()
        {
            LogMessage("****** MainFollowerCoroutine started", 1);
            while (true)
            {
                if (_followerShouldWork)
                {
                    if (_followerAggressivenessMode == FollowerAggressiveness.Passive)
                    {
                        yield return DoFollow();
                    }
                    else if (_followerAggressivenessMode == FollowerAggressiveness.Aggressive)
                    {
                        yield return DoAttack();
                    }
                }

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
                _followerCoroutine.Pause();
                _networkActivityCoroutine.Pause();
                _followerShouldWork = false;
                _networkCoroutineShouldWork = false;
                if (_httpListener.IsListening)
                {
                    LogMessage($"****** Tick: stopping the listener ", 1);
                    _httpListener.Stop();
                }
            }

            ;

            if (Input.GetKeyState(Settings.NetworkActivityPropagateWorkingChangeKey.Value))
            {
                ChangeNetworkActivityPropagateWorking();
            }

            if (Input.GetKeyState(Settings.NetworkActivityPropagateAggressivenessModeChangeKey.Value))
            {
                ChangeAggressivenessModeToPropagate();
            }

            if (Input.GetKeyState(Settings.NetworkActivityActivateKey.Value))
            {
                // LogMessage($"****** Tick: resuming _networkActivityCoroutine", 1);
                _networkCoroutineShouldWork = true;
                _networkActivityCoroutine.Resume();
            }

            if (Input.GetKeyState(Settings.FollowerActivateKey.Value))
            {
                if (_networkActivityCoroutine.Running)
                {
                    LogMessage(
                        $"****** You tried to start the follower activity when network activity is running. Stop network activity first",
                        1);
                    LogMessage($"****** Skipping starting the follower activity ", 1);
                    return null;
                }

                _debugTimer.Restart();
                _followerCoroutine.Resume();
                _followerShouldWork = true;
            }

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

        private IEnumerator DoFollowerNetworkActivityWork()
        {
            LogMessage(" :::::::: DoFollowerNetworkActivityWork called", 1);

            int timeoutInSeconds = 3;

            string url = Settings.NetworkActivityUrl.Value;
            if (string.IsNullOrEmpty(url) || !_networkRequestFinished)
            {
                LogMessage(" :::::::: DoFollowerNetworkActivityWork BREAKING", 1);
                yield break;
            }

            _networkRequestFinished = false;

            HttpClient.Timeout = TimeSpan.FromSeconds(timeoutInSeconds); // Always set a short timeout
            try
            {
                LogMessage(" :::::::: Firing a request", 1);

                var callback = new Action<string>((reply) =>
                {
                    NetworkActivityObject networkActivityObject =
                        JsonConvert.DeserializeObject<NetworkActivityObject>(reply);
                    ProcessNetworkActivityResponse(networkActivityObject);
                });
                _networkHelper.MakeGetNetworkRequest(url, timeoutInSeconds, LogMessage, callback);
            }
            finally
            {
                //LogMessage(" :::::::: Network request finished", 1);
                _networkRequestFinished = true;
            }
        }

        private void ProcessNetworkActivityResponse(NetworkActivityObject activityObj)
        {
            if (activityObj == null)
            {
                return;
            }

            Settings.LeaderName.Value = activityObj.LeaderName;
            Settings.UseMovementSkill.Value = activityObj.UseMovementSkill;
            _currentFollowerMode = activityObj.FollowerMode;
            _followerAggressivenessMode = activityObj.PropagateFollowerAggressiveness;
            _followerShouldWork = activityObj.Working;

            if (activityObj.Working) _followerCoroutine.Resume();
            else _followerCoroutine.Pause();

            if (activityObj.FollowerSkills != null && activityObj.FollowerSkills.Any())
            {
                List<SkillSettings> settingsSkills = new List<SkillSettings>()
                {
                    Settings.SkillOne,
                    Settings.SkillTwo,
                    Settings.SkillThree
                };
                foreach (FollowerSkill skill in activityObj.FollowerSkills.OrderBy(o => o.Position))
                {
                    SkillSettings settingsSkill = settingsSkills[skill.Position - 1];

                    settingsSkill.Enable.Value = skill.Enable;
                    settingsSkill.SkillHotkey.Value = skill.Hotkey;
                    settingsSkill.UseAgainstMonsters.Value = skill.UseAgainstMonsters;
                    settingsSkill.CooldownBetweenCasts.Value = skill.Cooldown.ToString();
                    settingsSkill.Priority.Value = skill.Priority;
                    settingsSkill.Position.Value = skill.Position;
                    settingsSkill.Range.Value = skill.Range;
                }
            }

            return;
        }

        private IEnumerator DoAttack()
        {
            LogMessage(" ------ DoAttack called", 1);

            List<Entity> monsters = GameController.EntityListWrapper
                .ValidEntitiesByType[EntityType.Monster]
                .Where(o => o.IsAlive)
                .Where(o => o.IsHostile)
                .ToList();

            Entity leaderPlayer = FollowerHelpers.GetLeaderEntity(Settings.LeaderName, GameController.Entities);
            Entity player = leaderPlayer ?? GameController.Player;

            IEnumerator nextStep = null;
            
            try
            {
                if (monsters.Any())
                {
                    List<Entity> closestMonsters = monsters.OrderBy(o => FollowerHelpers.EntityDistance(o, player)).ToList();
                    Entity closestMonster = closestMonsters.First();
                    nextStep = AttackEntity(closestMonster);
                }
                else nextStep = DoFollow();
            }
            catch (ArgumentNullException e)
            {
                nextStep = DoFollow();
            }

            yield return nextStep;
        }

        private IEnumerator AttackEntity(Entity closestMonster)
        {
            LogMessage(" ------ AttackEntity called", 1);

            List<SkillSettings> skills = new List<SkillSettings>()
            {
                Settings.SkillOne,
                Settings.SkillTwo,
                Settings.SkillThree,
            };

            SkillSettings availableSkill = null;
            try
            {
                availableSkill = skills
                    .Where(o =>
                    {
                        long delta = GetDeltaInMilliseconds(o.LastTimeUsed);
                        return delta > Int32.Parse(o.CooldownBetweenCasts.Value);
                    })
                    .Where(o =>
                    {
                        var distance = FollowerHelpers.EntityDistance(closestMonster, GameController.Player) / 10;
                        LogMessage($"DISTANCE: {distance}");
                        LogMessage($"o.Range.Value: {o.Range.Value}");
                        return distance < o.Range.Value;
                    })
                    .OrderBy(o => o.Priority.Value)
                    .First();

                availableSkill.LastTimeUsed = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                LogError(" ------ Error during filtering an available skill");
            }

            if (availableSkill != null)
            {
                yield return HoverTo(closestMonster);
                Input.KeyPressRelease(availableSkill.SkillHotkey.Value);
            }
            else
            {
                yield return DoFollow();
            }

            yield break;
        }

        private IEnumerator DoFollow()
        {
            LogMessage(" :::::::: DoFollow called", 1);

            Entity leaderPlayer = FollowerHelpers.GetLeaderEntity(Settings.LeaderName, GameController.Entities);
            if (leaderPlayer == null) yield break;

            yield return TryToClickOnLeader(leaderPlayer);
        }

        private IEnumerator TryToClickOnLeader(Entity leaderPlayer)
        {
            //LogMessage(" ;;;;;;;;; TryToClickOnLeader called", 1);

            yield return HoverTo(leaderPlayer);

            if (CanUseMovementSkill())
            {
                _lastTimeMovementSkillUsed = DateTime.UtcNow;
                yield return Input.KeyPress(Settings.MovementSkillKey);
            }
            else
            {
                yield return Mouse.LeftClick();
            }

            yield return CanUseMovementSkill() ? Input.KeyPress(Settings.MovementSkillKey) : Mouse.LeftClick();
        }

        private IEnumerator HoverTo(Entity entity)
        {
            var worldCoords = entity.Pos;
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
        }

        private bool CanUseMovementSkill()
        {
            var deltaMilliseconds = GetDeltaInMilliseconds(_lastTimeMovementSkillUsed);

            return Settings.UseMovementSkill && deltaMilliseconds > Settings.MovementSkillCooldownMilliseconds.Value;
        }

        private void ChangeNetworkActivityPropagateWorking()
        {
            int timeoutMilliseconds = 2000;
            var deltaMilliseconds = GetDeltaInMilliseconds(_lastTimeNetworkActivityPropagateWorkingChanged);

            var value = Settings.NetworkActivityPropagateWorking.Value;

            if (deltaMilliseconds > timeoutMilliseconds)
            {
                _lastTimeNetworkActivityPropagateWorkingChanged = DateTime.UtcNow;
                Settings.NetworkActivityPropagateWorking.Value = !value;
            }
        }

        private void ChangeAggressivenessModeToPropagate()
        {
            int timeoutMilliseconds = 1000;
            long deltaMilliseconds = GetDeltaInMilliseconds(_lastTimeAggressivenessModePropagated);

            if (deltaMilliseconds > timeoutMilliseconds)
            {
                _lastTimeAggressivenessModePropagated = DateTime.UtcNow;

                if (Settings.PropagateFollowerAggressiveness == FollowerAggressiveness.Passive)
                    Settings.PropagateFollowerAggressiveness = FollowerAggressiveness.Aggressive;
                else if (Settings.PropagateFollowerAggressiveness == FollowerAggressiveness.Aggressive)
                    Settings.PropagateFollowerAggressiveness = FollowerAggressiveness.Passive;
                else
                    Settings.PropagateFollowerAggressiveness = FollowerAggressiveness.Passive;
            }
        }

        private long GetDeltaInMilliseconds(DateTime lastTime)
        {
            long currentMs = ((DateTimeOffset) _currentTime).ToUnixTimeMilliseconds();
            long lastTimeMs = ((DateTimeOffset) lastTime).ToUnixTimeMilliseconds();
            return currentMs - lastTimeMs;
        }
    }
}