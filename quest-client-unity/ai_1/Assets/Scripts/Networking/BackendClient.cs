using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestAssistant
{
    // Quest 客户端到后端的 WebSocket 连接。
    // 后台线程收发，主线程 (Update) 派发回调，避免在非主线程碰 Unity API。
    // Link 编辑器内跑：serverUrl 用 localhost 即可（编辑器与后端同机）。
    // 真机 APK：改成开发机的局域网 IP，如 ws://192.168.1.20:8000/ws
    public class BackendClient : MonoBehaviour
    {
        [Header("Connection")]
        public string serverUrl = "ws://localhost:8000/ws";
        public string sessionId = "quest_demo_001";
        [Tooltip("断线后最多重连次数；<0 表示无限重连")]
        public int maxReconnectAttempts = 5;
        public float reconnectDelaySeconds = 2f;

        public enum ConnState { Disconnected, Connecting, Connected }
        public ConnState State { get; private set; } = ConnState.Disconnected;

        // 以下事件都在主线程触发
        public event Action<AssistantResponse> OnResponse;
        public event Action<string> OnError;
        public event Action<ConnState> OnStateChanged;

        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<ConnState> _stateChanges = new ConcurrentQueue<ConnState>();

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts.Token);
        }

        private void OnDisable()
        {
            try { _cts?.Cancel(); } catch { }
            CloseSocketQuietly();
        }

        private async Task RunAsync(CancellationToken token)
        {
            int attempt = 0;
            while (!token.IsCancellationRequested)
            {
                SetState(ConnState.Connecting);
                _socket = new ClientWebSocket();
                bool connected = false;
                try
                {
                    await _socket.ConnectAsync(new Uri(serverUrl), token);
                    connected = true;
                    attempt = 0;
                    SetState(ConnState.Connected);
                }
                catch (Exception e)
                {
                    EnqueueError("connect failed: " + e.Message);
                }

                if (connected)
                    await ReceiveLoop(token); // 返回即视为断线

                CloseSocketQuietly();
                if (token.IsCancellationRequested) break;
                SetState(ConnState.Disconnected);

                attempt++;
                if (maxReconnectAttempts >= 0 && attempt > maxReconnectAttempts)
                {
                    EnqueueError("max reconnect attempts reached; giving up");
                    break;
                }
                try { await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token); }
                catch { break; }
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();
            while (!token.IsCancellationRequested && _socket != null && _socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                }
                catch (Exception e)
                {
                    EnqueueError("receive error: " + e.Message);
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Close) break;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (result.EndOfMessage)
                {
                    _incoming.Enqueue(sb.ToString());
                    sb.Clear();
                }
            }
        }

        // 主线程调用：只在“稳定选中”时发一次。
        public void SendPointing(string targetId, string label, Vector3 origin, Vector3 direction)
        {
            if (State != ConnState.Connected || _socket == null)
            {
                EnqueueError("not connected; pointing dropped");
                return;
            }
            var evt = new PointingEvent
            {
                session_id = sessionId,
                timestamp_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                target_id = targetId,
                target_label = label,
                ray_origin = new[] { origin.x, origin.y, origin.z },
                ray_direction = new[] { direction.x, direction.y, direction.z },
            };
            _ = SendRawAsync(JsonUtility.ToJson(evt));
        }

        private async Task SendRawAsync(string json)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception e)
            {
                EnqueueError("send error: " + e.Message);
            }
        }

        private void Update()
        {
            while (_stateChanges.TryDequeue(out var s))
                OnStateChanged?.Invoke(s);
            while (_incoming.TryDequeue(out var raw))
                DispatchMessage(raw);
        }

        private void DispatchMessage(string raw)
        {
            MessageEnvelope env;
            try { env = JsonUtility.FromJson<MessageEnvelope>(raw); }
            catch { OnError?.Invoke("bad json: " + raw); return; }

            if (env != null && env.type == "assistant_response")
                OnResponse?.Invoke(JsonUtility.FromJson<AssistantResponse>(raw));
            else if (env != null && env.type == "error")
                OnError?.Invoke(env.message);
            else
                OnError?.Invoke("unknown message: " + raw);
        }

        private void SetState(ConnState s)
        {
            State = s;
            _stateChanges.Enqueue(s); // 回调在 Update 主线程触发
        }

        // 后台线程的错误也通过 incoming 队列回到主线程派发
        private void EnqueueError(string msg)
        {
            Debug.LogWarning("[BackendClient] " + msg);
            _incoming.Enqueue("{\"type\":\"error\",\"message\":\"" + Escape(msg) + "\"}");
        }

        private static string Escape(string s)
        {
            return s == null ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private void CloseSocketQuietly()
        {
            try
            {
                if (_socket != null)
                {
                    if (_socket.State == WebSocketState.Open) _socket.Abort();
                    _socket.Dispose();
                }
            }
            catch { }
            _socket = null;
        }
    }
}
