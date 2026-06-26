using UnityEngine;
using UnityEngine.UI;

namespace QuestAssistant
{
    // 把后端回应显示到世界空间 UI。直接订阅 BackendClient 的事件，
    // 在 Inspector 里只需把 client 和三个 Text 拖进来即可。
    public class ResponseDisplay : MonoBehaviour
    {
        public BackendClient client;
        public Text titleText;
        public Text bodyText;
        public Text statusText;

        private void OnEnable()
        {
            if (client == null) return;
            client.OnResponse += HandleResponse;
            client.OnError += HandleError;
            client.OnStateChanged += HandleState;
            HandleState(client.State);
        }

        private void OnDisable()
        {
            if (client == null) return;
            client.OnResponse -= HandleResponse;
            client.OnError -= HandleError;
            client.OnStateChanged -= HandleState;
        }

        private void HandleResponse(AssistantResponse r)
        {
            if (titleText != null) titleText.text = string.IsNullOrEmpty(r.display_title) ? r.target_id : r.display_title;
            if (bodyText != null) bodyText.text = r.speech;
        }

        private void HandleError(string message)
        {
            if (bodyText != null) bodyText.text = "暂时无法获取说明";
            Debug.LogWarning("[ResponseDisplay] backend error: " + message);
        }

        private void HandleState(BackendClient.ConnState s)
        {
            if (statusText == null) return;
            switch (s)
            {
                case BackendClient.ConnState.Connected: statusText.text = "● 已连接"; statusText.color = Color.green; break;
                case BackendClient.ConnState.Connecting: statusText.text = "… 连接中"; statusText.color = Color.yellow; break;
                default: statusText.text = "✕ 未连接"; statusText.color = Color.red; break;
            }
        }
    }
}
