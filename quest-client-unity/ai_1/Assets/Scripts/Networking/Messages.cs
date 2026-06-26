using System;

namespace QuestAssistant
{
    // 与 shared/schemas + docs/protocol.md 对齐的线上消息结构。
    // 用 Unity JsonUtility 序列化，字段名必须与后端逐字一致（snake_case）。

    [Serializable]
    public class PointingEvent
    {
        public string type = "pointing_event";
        public string session_id;
        public long timestamp_ms;
        public string target_id;
        public string target_label;
        public float[] ray_origin;
        public float[] ray_direction;
    }

    [Serializable]
    public class AssistantResponse
    {
        public string type;
        public string target_id;
        public string speech;
        public string display_title;
        public string display_subtitle;
        public string status;
        public string avatar_intent;
        public string tts_mode;
    }

    // 仅用于先读出 type 字段，判断这条消息是 assistant_response 还是 error。
    [Serializable]
    public class MessageEnvelope
    {
        public string type;
        public string message; // error 消息体
    }
}
