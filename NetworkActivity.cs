using System;
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
        public NetworkActivityObject(bool working, FollowerType followerType, string leaderName, bool useMovementSkill, Keys movementSkillKey)
        {
            Working = working;
            FollowerMode = followerType;
            LeaderName = leaderName;
            UseMovementSkill = useMovementSkill;
            MovementSkillKey = movementSkillKey;
        }

        [JsonProperty("working")]
        public bool Working { get; set; }

        [JsonProperty("follower_mode")]
        public FollowerType FollowerMode { get; set; }

        [JsonProperty("leader_name")]
        public string LeaderName { get; set; }

        [JsonProperty("use_movement_skill")]
        public bool UseMovementSkill { get; set; }

        [JsonProperty("movement_skill_key")]
        public Keys MovementSkillKey { get; set; }
    }

    public class NetworkHelper
    {

        private FollowerSettings _followerSettings;

        public NetworkHelper(FollowerSettings followerSettings)
        {
            _followerSettings = followerSettings;
        }

        public void MakeGetNetworkRequest(string url, int timeoutSeconds, Action<string, float> logMessageFunc, Action<string> callbackFunc)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeoutSeconds * 1000;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string reply = reader.ReadToEnd();

                callbackFunc(reply);

                //NetworkActivityObject networkActivityObject = JsonConvert.DeserializeObject<NetworkActivityObject>(reply);
                //ProcessNetworkActivityResponse(networkActivityObject);
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
                HttpListener list = (HttpListener)res.AsyncState;

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
                    _followerSettings.NetworkActivityPropagateMovementSkillKey.Value
                );

                // Construct a response.
                string responseString = JsonConvert.SerializeObject(networkActivityObject); ;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
            }
        }
    }

}
