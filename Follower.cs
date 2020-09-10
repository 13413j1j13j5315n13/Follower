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

        private WaitTime Wait3ms => new WaitTime(3);

        private WaitTime Wait10ms => new WaitTime(10);
        //private WaitTime Wait3seconds => new WaitTime(3 * 1000);

        private DateTime _currentTime = DateTime.UtcNow;
        private DateTime _lastTimeMovementSkillUsed;
        private DateTime _lastTimeNetworkActivityPropagateWorkingChanged;

        public override bool Initialise()
        {
            LogMessage("****** Initialise started", 1);

            _networkHelper = new NetworkHelper(Settings);

            _windowRectangle = GameController.Window.GetWindowRectangleReal();
            _windowSize = new Size2F(_windowRectangle.Width / 2560, _windowRectangle.Height / 1600);
            _lastTimeMovementSkillUsed = DateTime.UtcNow;
            _lastTimeNetworkActivityPropagateWorkingChanged = DateTime.UtcNow;

            GameController.LeftPanel.WantUse(() => true);

            _serverCoroutine = new Coroutine(MainServerCoroutine(), this, "Follower Server");
            _followerCoroutine = new Coroutine(MainWorkCoroutine(), this, "Follower");
            _networkActivityCoroutine = new Coroutine(MainNetworkActivityCoroutine(), this, "Follower Network Activity");

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
            NumericsVector2 firstLine = Graphics.DrawText(workingText, startDrawPoint, workingColor, fontHeight, FontAlign.Right);
            startDrawPoint.Y += firstLine.Y;

            if (Settings.EnableNetworkActivity.Value)
            {
                NumericsVector2 line = Graphics.DrawText("Network Activity is enabled", startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
                startDrawPoint.Y += line.Y;

                string naText = "Network coroutine is" + (_networkActivityCoroutine.Running ? "" : " NOT") + " running";
                line = Graphics.DrawText(naText, startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
                startDrawPoint.Y += line.Y;


                if (_httpListener.IsListening)
                {
                    line = Graphics.DrawText("Server is listening", startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
                    startDrawPoint.Y += line.Y;
                }

                var (text, color) = GetFollowerModeTextAndColor();
                line = Graphics.DrawText(text, startDrawPoint, color, fontHeight, FontAlign.Right);
                startDrawPoint.Y += line.Y;

                (text, color) = GetPropagateWorkingText();
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
                    //LogMessage("****** MainNetworkActivityCoroutine inside FollowerType.Follower", 1);
                    _httpListener.Stop();
                    yield return DoFollowerNetworkActivityWork();
                    yield return new WaitTime(3 * 1000); ; // Always wait before making a new request
                }
                else if (_currentFollowerMode == FollowerType.Leader)
                {
                    //LogMessage("****** MainNetworkActivityCoroutine inside FollowerType.Leader", 1);
                    //LogMessage($"****** _httpListener.IsListening {_httpListener.IsListening}", 1);
                    if (!_httpListener.IsListening)
                    {
                        _httpListener.Start();
                    }
                }
                //else
                //{
                //    LogMessage("****** MainNetworkActivityCoroutine inside else", 1);
                //    _httpListener.Stop();
                //}

                //LogMessage("****** MainNetworkActivityCoroutine after everything", 1);

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

                    //IAsyncResult result = _httpListener.BeginGetContext(new AsyncCallback(ListenerCallback), _httpListener);

                    //void ListenerCallback(IAsyncResult res)
                    //{
                    //    HttpListener list = (HttpListener)res.AsyncState;

                    //    HttpListenerContext context;
                    //    try
                    //    {
                    //        context = list.EndGetContext(res);
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        // Probably listener was closed so everything is OK
                    //        return;
                    //    }

                    //    HttpListenerRequest req = context.Request;
                    //    // Obtain a response object.
                    //    HttpListenerResponse response = context.Response;
                    //    // Construct a response.
                    //    NetworkActivityObject networkActivityObject = new NetworkActivityObject(
                    //        Settings.NetworkActivityPropagateWorking.Value,
                    //        FollowerType.Follower,
                    //        Settings.NetworkActivityPropagateLeaderName.Value,
                    //        Settings.NetworkActivityPropagateUseMovementSkill.Value,
                    //        Settings.NetworkActivityPropagateMovementSkillKey.Value
                    //    );
                    //    // Construct a response.
                    //    string responseString = JsonConvert.SerializeObject(networkActivityObject); ;
                    //    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    //    // Get a response stream and write the response to it.
                    //    response.ContentLength64 = buffer.Length;
                    //    System.IO.Stream output = response.OutputStream;
                    //    output.Write(buffer, 0, buffer.Length);
                    //    // You must close the output stream.
                    //    output.Close();
                    //}
                }

                yield return new WaitTime(50);
            }
        }

        private IEnumerator MainWorkCoroutine()
        {
            LogMessage("****** MainWorkCoroutine started", 1);
            while (true)
            {
                if (_followerShouldWork)
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

            //if (_coroutinesFirstTime)
            //{
            //    _followerCoroutine.Pause();
            //    _networkActivityCoroutine.Pause();
            //    _coroutinesFirstTime = false;
            //}

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
            };

            if (Input.GetKeyState(Settings.NetworkActivityPropagateWorkingChangeKey.Value))
            {
                ChangeNetworkActivityPropagateWorking();
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
                    LogMessage($"****** You tried to start the follower activity when network activity is running. Stop network activity first", 1);
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
            //LogMessage(" :::::::: ProcessNetworkActivityResponse called", 1);
            //LogMessage($" :::::::: activityObj {activityObj}", 1);
            if (activityObj == null)
            {
                return;
            }

            Settings.LeaderName.Value = activityObj.LeaderName;
            Settings.UseMovementSkill.Value = activityObj.UseMovementSkill;
            _currentFollowerMode = activityObj.FollowerMode;
            _followerShouldWork = activityObj.Working;

            if (activityObj.Working)
            {
                LogMessage(" :::::::: Resuming follower coroutine because activityObj.Working was true", 1);
                _followerCoroutine.Resume();
            }
            else
            {
                _followerCoroutine.Pause();
            }

            return;
        }

        private IEnumerator DoWork()
        {
            //LogMessage(" :::::::: DoWork called", 1);

            if (Settings.LeaderName == null || Settings.LeaderName.Value == "")
            {
                yield break;
            }

            IEnumerable<Entity> players = GameController.Entities.Where(x => x.Type == EntityType.Player);

            string leaderName = Settings.LeaderName.Value;
            Entity leaderPlayer = SelectLeaderPlayer(players, leaderName);

            if (leaderPlayer == null)
            {
                yield break;
            }

            yield return TryToClickOnLeader(leaderPlayer);
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

        private bool CanUseMovementSkill()
        {
            var currentMs = ((DateTimeOffset)_currentTime).ToUnixTimeMilliseconds();
            var lastTimeMs = ((DateTimeOffset)_lastTimeMovementSkillUsed).ToUnixTimeMilliseconds();
            var deltaMilliseconds = currentMs - lastTimeMs;

            return Settings.UseMovementSkill && deltaMilliseconds > Settings.MovementSkillCooldownMilliseconds.Value;
        }

        private void ChangeNetworkActivityPropagateWorking()
        {
            int timeoutMilliseconds = 2000;
            var currentMs = ((DateTimeOffset)_currentTime).ToUnixTimeMilliseconds();
            var lastTimeMs = ((DateTimeOffset)_lastTimeNetworkActivityPropagateWorkingChanged).ToUnixTimeMilliseconds();
            var deltaMilliseconds = currentMs - lastTimeMs;
            var value = Settings.NetworkActivityPropagateWorking.Value;

            if (deltaMilliseconds > timeoutMilliseconds)
            {
                _lastTimeNetworkActivityPropagateWorkingChanged = DateTime.UtcNow;
                Settings.NetworkActivityPropagateWorking.Value = !value;
            }
        }
    }
}
