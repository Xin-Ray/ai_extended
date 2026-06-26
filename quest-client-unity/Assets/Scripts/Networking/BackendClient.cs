using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestSpatialAssistant.Networking
{
    /// <summary>
    /// 与后端的 WebSocket 客户端。用 ClientWebSocket（Quest/Android 上可用）。
    /// 后台线程收消息，回主线程派发 OnResponse 事件。断线自动重连。
    /// </summary>
    public class BackendClient : MonoBehaviour
    {
        [Tooltip("后端地址，例如 ws://192.168.1.20:8000/ws —— 改成你开发机的内网 IP")]
        public string serverUrl = "ws://192.168.1.20:8000/ws";

        [Tooltip("断线后多少秒重连")]
        public float reconnectDelaySeconds = 2f;

        /// <summary>收到 assistant_response 时在主线程触发。</summary>
        public event Action<AssistantResponse> OnResponse;

        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
        private volatile bool _connected;

        public bool IsConnected => _connected;

        private void Start()
        {
            _cts = new CancellationTokenSource();
            _ = ConnectLoop(_cts.Token);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            try { _socket?.Dispose(); } catch { /* ignore */ }
        }

        private void Update()
        {
            // 在主线程派发收到的消息（Unity API 只能主线程调用）
            while (_incoming.TryDequeue(out var json))
            {
                AssistantResponse resp = null;
                try { resp = JsonUtility.FromJson<AssistantResponse>(json); }
                catch (Exception e) { Debug.LogWarning($"[BackendClient] bad json: {e.Message}"); }

                if (resp != null && resp.type == "assistant_response")
                    OnResponse?.Invoke(resp);
                else
                    Debug.Log($"[BackendClient] non-response msg: {json}");
            }
        }

        /// <summary>上报一次指向事件。</summary>
        public void SendPointingEvent(string targetId, string targetLabel,
            Vector3 rayOrigin, Vector3 rayDirection)
        {
            if (!_connected) { Debug.LogWarning("[BackendClient] not connected, drop event"); return; }

            var evt = new PointingEvent
            {
                session_id = "demo_001",
                timestamp_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                target_id = targetId,
                target_label = targetLabel,
                ray_origin = new[] { rayOrigin.x, rayOrigin.y, rayOrigin.z },
                ray_direction = new[] { rayDirection.x, rayDirection.y, rayDirection.z },
            };
            _ = SendText(JsonUtility.ToJson(evt));
        }

        private async Task SendText(string text)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                await _socket.SendAsync(new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackendClient] send failed: {e.Message}");
                _connected = false;
            }
        }

        private async Task ConnectLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _socket = new ClientWebSocket();
                    Debug.Log($"[BackendClient] connecting {serverUrl} ...");
                    await _socket.ConnectAsync(new Uri(serverUrl), token);
                    _connected = true;
                    Debug.Log("[BackendClient] connected");
                    await ReceiveLoop(token);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BackendClient] connect error: {e.Message}");
                }

                _connected = false;
                try { _socket?.Dispose(); } catch { /* ignore */ }
                if (token.IsCancellationRequested) break;
                await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();
            while (_socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                sb.Clear();
                do
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", token);
                        return;
                    }
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                _incoming.Enqueue(sb.ToString());
            }
        }
    }
}
