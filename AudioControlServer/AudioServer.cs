using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;

namespace AudioControlServer
{
    
    class AudioServer
    {
        public enum MediaRequest
        {
            VOLUME_UP,
            VOLUME_DOWN,
            MUTE_TOGGLE,
            VOLUME_SET,
            MEDIA_NEXT , 
            MEDIA_PREVIOUS,
            MEDIA_STOP , 
            MEDIA_PLAY 
        }
        public delegate void MediaRequestHandler(MediaRequest request, int value = 0);
        public event MediaRequestHandler OnMedia;
        private int mListenPort = 0;
        private string mKey; 
        private Task mTask;
        CancellationTokenSource mCancellationSource = new CancellationTokenSource();
        CancellationToken mCancellationToken;
        private UdpClient mUdpClient;
        public AudioServer(int port,string key)
        {
            this.mListenPort = port;
            mUdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            mCancellationToken = mCancellationSource.Token;
            mKey = key;
        }
        private void sendEvent(MediaRequest request, int value = 0)
        {
            OnMedia?.Invoke(request, value);
        }
        public void StartServer()
        {
            mUdpClient.AllowNatTraversal(true);
            mUdpClient.EnableBroadcast = true;
            mTask = new Task(async () => {
                while (!mCancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var UdpResult = await mUdpClient.ReceiveAsync();
                        byte[] buffer = UdpResult.Buffer;
                        string data = System.Text.Encoding.UTF8.GetString(buffer);
                        dynamic mData = JsonConvert.DeserializeObject(data);
                        string key = mData.Key;
                        string action = mData.Action;
                        int Volume = mData.Volume;
                        if (mKey != key)
                        {

                            byte[] err_msg = Encoding.UTF8.GetBytes("INVALID_KEY");
                            await mUdpClient.SendAsync(err_msg, err_msg.Length, UdpResult.RemoteEndPoint);
                            Console.WriteLine($"INVALID KEY FROM : {UdpResult.RemoteEndPoint.ToString()}");

                            continue;
                        }
                        if (action == "VOLUME_UP")
                        {
                            sendEvent(MediaRequest.VOLUME_UP);
                        }
                        else if (action == "VOLUME_DOWN")
                        {
                            sendEvent(MediaRequest.VOLUME_DOWN);
                        }
                        else if (action == "SET_VOLUME")
                        {
                            sendEvent(MediaRequest.VOLUME_SET, Volume);
                        }
                        else if (action == "MUTE_TOGGLE")
                        {
                            sendEvent(MediaRequest.MUTE_TOGGLE);
                        }


                        else if (action == "MEDIA_NEXT")
                        {
                            sendEvent(MediaRequest.MEDIA_NEXT);
                        }
                        else if (action == "MEDIA_PREVIUS")
                        {
                            sendEvent(MediaRequest.MEDIA_PREVIOUS);
                        }
                        else if (action == "MEDIA_STOP")
                        {
                            sendEvent(MediaRequest.MEDIA_STOP);
                        }
                        else if (action == "MEDIA_PLAY")
                        {
                            sendEvent(MediaRequest.MEDIA_PLAY);
                        }
                        byte[] msg = Encoding.UTF8.GetBytes("OK");
                        await mUdpClient.SendAsync(msg, msg.Length, UdpResult.RemoteEndPoint);
                    }catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                mUdpClient.Close();
                Console.WriteLine("Thread Exited");
            },mCancellationToken);
            mTask.Start();
        }
        public void StopServer()
        {
            mCancellationSource.Cancel(); 
        }
    }
}
