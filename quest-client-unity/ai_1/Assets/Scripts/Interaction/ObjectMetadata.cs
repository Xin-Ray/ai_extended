using UnityEngine;

namespace QuestAssistant
{
    // 挂在每个可解释的虚拟物体上。targetId 必须与 shared/objects.json 里的 target_id 逐字一致。
    public class ObjectMetadata : MonoBehaviour
    {
        [Tooltip("必须与后端 shared/objects.json 的 target_id 完全一致，如 cup_01")]
        public string targetId;

        [Tooltip("人类可读标签，仅用于日志/调试，如 cup")]
        public string label;
    }
}
