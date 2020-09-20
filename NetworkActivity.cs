using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;
using System.Windows.Forms;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace Follower
{
    public class NetworkActivityObject
    {
        public NetworkActivityObject(bool working, FollowerType followerType, string leaderName, bool useMovementSkill,
            Keys movementSkillKey, List<FollowerSkill> followerSkills,
            FollowerAggressiveness propagateFollowerAggressiveness, long propagateEnterInstance)
        {
            Working = working;
            FollowerMode = followerType;
            LeaderName = leaderName;
            UseMovementSkill = useMovementSkill;
            MovementSkillKey = movementSkillKey;
            LastChangeTimestamp = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeMilliseconds();
            FollowerSkills = followerSkills;
            PropagateFollowerAggressiveness = propagateFollowerAggressiveness;
            PropagateEnterInstance = propagateEnterInstance;
        }

        [JsonProperty("working")] public bool Working { get; set; }

        [JsonProperty("follower_mode")] public FollowerType FollowerMode { get; set; }

        [JsonProperty("leader_name")] public string LeaderName { get; set; }

        [JsonProperty("use_movement_skill")] public bool UseMovementSkill { get; set; }

        [JsonProperty("movement_skill_key")] public Keys MovementSkillKey { get; set; }

        [JsonProperty("last_change_timestamp")]
        public long LastChangeTimestamp { get; set; }

        [JsonProperty("propagate_follower_aggressiveness")]
        public FollowerAggressiveness PropagateFollowerAggressiveness { get; set; }


        [JsonProperty("enter_instance")]
        public long PropagateEnterInstance { get; set; }

        [JsonProperty("follower_skills")] public List<FollowerSkill> FollowerSkills { get; set; }
    }

    public class NetworkHelper
    {
        private readonly FollowerSettings _followerSettings;

        public NetworkHelper(FollowerSettings followerSettings)
        {
            _followerSettings = followerSettings;
        }

        public void MakeGetNetworkRequest(string url, int timeoutSeconds, Action<string, float> logMessageFunc,
            Action<string> callbackFunc)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeoutSeconds * 1000;
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string reply = reader.ReadToEnd();

                callbackFunc(reply);
            }
            else
            {
                logMessageFunc(
                    $" :::::::: Follower - tried to make a HTTP request to {url} but the return message was not successful",
                    1);
            }
        }

        public void MakeAsyncListen(HttpListener listener)
        {
            IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);

            void ListenerCallback(IAsyncResult res)
            {
                HttpListener list = (HttpListener) res.AsyncState;

                HttpListenerContext context;
                try
                {
                    context = list.EndGetContext(res);
                }
                catch (Exception e)
                {
                    // Probably listener was closed so everything is OK
                    return;
                }

                HttpListenerRequest req = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.

                NetworkActivityObject networkActivityObject = new NetworkActivityObject(
                    _followerSettings.NetworkActivityPropagateWorking.Value,
                    FollowerType.Follower,
                    _followerSettings.NetworkActivityPropagateLeaderName.Value,
                    _followerSettings.NetworkActivityPropagateUseMovementSkill.Value,
                    _followerSettings.NetworkActivityPropagateMovementSkillKey.Value,
                    GetFollowerSkills(),
                    _followerSettings.PropagateFollowerAggressiveness,
                    _followerSettings.PropagateEnterInstance
                );

                // Construct a response.
                string responseString = JsonConvert.SerializeObject(networkActivityObject);
                ;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
            }
        }

        private List<FollowerSkill> GetFollowerSkills()
        {
            return new List<FollowerSkill>()
            {
                new FollowerSkill(
                    _followerSettings.SkillOne.Enable.Value,
                    _followerSettings.SkillOne.SkillHotkey.Value,
                    _followerSettings.SkillOne.UseAgainstMonsters.Value,
                    Int32.Parse(_followerSettings.SkillOne.CooldownBetweenCasts.Value),
                    _followerSettings.SkillOne.Priority.Value,
                    _followerSettings.SkillOne.Position.Value,
                    _followerSettings.SkillOne.Range.Value
                ),
                new FollowerSkill(
                    _followerSettings.SkillTwo.Enable.Value,
                    _followerSettings.SkillTwo.SkillHotkey.Value,
                    _followerSettings.SkillTwo.UseAgainstMonsters.Value,
                    Int32.Parse(_followerSettings.SkillTwo.CooldownBetweenCasts.Value),
                    _followerSettings.SkillTwo.Priority.Value,
                    _followerSettings.SkillTwo.Position.Value,
                    _followerSettings.SkillTwo.Range.Value
                ),
                new FollowerSkill(
                    _followerSettings.SkillThree.Enable.Value,
                    _followerSettings.SkillThree.SkillHotkey.Value,
                    _followerSettings.SkillThree.UseAgainstMonsters.Value,
                    Int32.Parse(_followerSettings.SkillThree.CooldownBetweenCasts.Value),
                    _followerSettings.SkillThree.Priority.Value,
                    _followerSettings.SkillThree.Position.Value,
                    _followerSettings.SkillThree.Range.Value
                ),
            };
        }
    }
}
