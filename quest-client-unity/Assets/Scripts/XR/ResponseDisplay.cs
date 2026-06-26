using QuestSpatialAssistant.Networking;
using QuestSpatialAssistant.Voice;
using UnityEngine;
using TMPro;

namespace QuestSpatialAssistant.Interaction
{
    /// <summary>
    /// 把后端回应显示到一块世界空间 UI 上（TextMeshPro），并触发 TTS 播报。
    /// 订阅 BackendClient.OnResponse；命中时先显示 "xxx detected"。
    ///
    /// 放在 Interaction 命名空间方便 IndexFingerRaycaster 引用；UI 资产放 Scripts/XR 下。
    /// </summary>
    public class ResponseDisplay : MonoBehaviour
    {
        [Header("UI 文本（拖入 TextMeshPro - Text (UI) 或 3D Text）")]
        public TMP_Text titleText;
        public TMP_Text bodyText;

        [Header("依赖")]
        public BackendClient backendClient;
        public TtsPlayer tts;

        private void OnEnable()
        {
            if (backendClient != null) backendClient.OnResponse += HandleResponse;
        }

        private void OnDisable()
        {
            if (backendClient != null) backendClient.OnResponse -= HandleResponse;
        }

        /// <summary>命中物体、等待后端时的即时反馈。</summary>
        public void ShowDetecting(string label)
        {
            if (titleText != null) titleText.text = $"{label} detected";
            if (bodyText != null) bodyText.text = "...";
        }

        private void HandleResponse(AssistantResponse resp)
        {
            if (titleText != null) titleText.text = resp.display_title;
            if (bodyText != null) bodyText.text = resp.speech;
            if (tts != null && !string.IsNullOrEmpty(resp.speech)) tts.Speak(resp.speech);
        }
    }
}
