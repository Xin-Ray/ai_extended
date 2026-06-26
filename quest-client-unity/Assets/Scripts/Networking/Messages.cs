using System;
using UnityEngine;

namespace QuestSpatialAssistant.Networking
{
    /// <summary>
    /// 与 shared/schemas 对齐的可序列化消息。用 Unity 自带 JsonUtility 收发。
    /// 注意：JsonUtility 不会序列化 null，可选字段留空即可。
    /// </summary>

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
        public string avatar_intent;
        public string tts_mode;
    }
}
