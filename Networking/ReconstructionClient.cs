using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;
using RTReconstruct.Core.Models;

namespace RTReconstruct.Networking
{
    public class ReconstructionClient : MonoBehaviour
    {
        public static ReconstructionClient Instance { get; private set; }

        public event Action<string> OnMessageReceived;


        private WebSocket websocket;
        private readonly Queue<ModelFragment> sendQueue = new();
        private bool isSending = false;
        private bool isConnected = false;

        [SerializeField]
        private string serverUrl = "wss://yourserver.com/socket";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async void Connect()
        {
            if (isConnected) return;

            websocket = new WebSocket(serverUrl);

            websocket.OnOpen += () =>
            {
                Debug.Log("WebSocket connected");
                isConnected = true;
                _ = ProcessSendQueueAsync(); // fire-and-forget
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError("WebSocket Error: " + e);
                isConnected = false;
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("WebSocket Closed");
                isConnected = false;
            };

            websocket.OnMessage += (bytes) =>
            {
                var message = Encoding.UTF8.GetString(bytes);
                OnMessageReceived?.Invoke(message);
            };

            await websocket.Connect();
        }

        public async void Disconnect()
        {
            if (websocket != null && isConnected)
            {
                await websocket.Close();
                isConnected = false;
            }
        }

        public void EnqueueFragment(ModelFragment fragment)
        {
            sendQueue.Enqueue(fragment);

            if (isConnected && !isSending)
            {
                _ = ProcessSendQueueAsync(); // Start sending if not already
            }
        }

        private async Task ProcessSendQueueAsync()
        {
            if (isSending || !isConnected) return;

            isSending = true;

            while (sendQueue.Count > 0 && isConnected)
            {
                ModelFragment fragment = sendQueue.Dequeue();
                byte[] data = fragment.Serialize();

                try
                {
                    await websocket.Send(data);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Failed to send fragment: " + ex.Message);
                    sendQueue.Enqueue(fragment); // Re-enqueue
                    await Task.Delay(100);       // Brief delay before retry
                    break; // Exit loop and try again later
                }

                await Task.Yield(); // Yield to allow Unity to remain responsive
            }

            isSending = false;
        }

        private void Update()
        {
    #if !UNITY_WEBGL || UNITY_EDITOR
            websocket?.DispatchMessageQueue(); // Required on non-WebGL platforms
    #endif
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}