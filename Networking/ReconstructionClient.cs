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

        public event Action<ModelResult> OnModelResultReceived;


        private WebSocket websocket;
        private readonly Queue<ModelFragment> sendQueue = new();
        private bool isSending = false;
        private bool isConnected = false;
        private string clientRole;
        private string clientScene;

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

        public async void Connect(string role, string scene)
        {
            if (isConnected) return;

            websocket = new WebSocket(serverUrl);
            clientRole = role;
            clientScene = scene;


            websocket.OnOpen += async () =>
            {
                Debug.Log("WebSocket connected");
                isConnected = true;
                await ServerHandshake(clientRole, clientScene);
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
                Debug.Log($"Reeceived {bytes.Length} bytes from server");
                OnModelResultReceived?.Invoke(new ModelResult(bytes));
            };

            await websocket.Connect();
        }

        public async Task ServerHandshake(string role, string scene)
        {
            if (websocket != null && isConnected)
            {
                string handshakeMessage = $"{{\"role\":\"{role}\",\"scene\":\"{scene}\"}}";
                await websocket.SendText(handshakeMessage);
                Debug.Log("Handshake sent to server.");
            }
            else
            {
                Debug.LogWarning("Cannot send handshake: Not connected to server.");
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

        public async void Disconnect()
        {
            if (websocket != null && isConnected)
            {
                await websocket.Close();
                isConnected = false;
            }
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}