using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;
using RTReconstruct.Core.Models;
using RTReconstruct.Core.Models.Interfaces;
using Newtonsoft.Json;

namespace RTReconstruct.Networking
{
    public class ReconstructionClient : MonoBehaviour
    {
        public static ReconstructionClient Instance { get; private set; }

        public event Action<ModelResult> OnModelResultReceived;

        public List<string> AvailableModels = new List<string>();

        private WebSocket websocket;
        private readonly LinkedList<IFragment> sendQueue = new();
        private bool isSending = false;
        private bool isConnected = false;
        private bool receivedHandshake = false;
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

                if (!receivedHandshake)
                {
                    var message = Encoding.UTF8.GetString(bytes);
                    AvailableModels = JsonConvert.DeserializeObject<List<string>>(message);
                    receivedHandshake = true;
                }
                else
                {
                    OnModelResultReceived?.Invoke(new ModelResult(bytes));
                }    
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

        public void EnqueueFragment(IFragment fragment)
        {
            if (fragment is TransformFragment)
            {
                if (sendQueue.Count > 0 && sendQueue.First.Value is TransformFragment)
                {
                    sendQueue.RemoveFirst();
                }
                sendQueue.AddFirst(fragment);
            }
            else
            {
                sendQueue.AddLast(fragment);
            }

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
                IFragment fragment = sendQueue.First.Value;
                sendQueue.RemoveFirst();

                try
                {
                    // Offload serialization + send to background thread
                    await Task.Run(async () =>
                    {
                        byte[] data = fragment.Serialize();
                        await websocket.Send(data);
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Failed to send fragment: " + ex.Message);
                    sendQueue.AddLast(fragment);
                    await Task.Delay(100);
                    break;
                }

                await Task.Yield(); // Keeps Unity responsive
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

        public bool ConnectionStatus()
        {
            return isConnected;
        }
    }
}