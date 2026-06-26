using UnityEngine;

namespace QuestSpatialAssistant.Interaction
{
    /// <summary>
    /// 挂在每个可指向的虚拟物体上。targetId 必须与后端 resolver 注册表一致
    /// （cup_01 / laptop_01 / bottle_01 / plant_01 / monitor_01）。
    /// 物体需要带一个 Collider 才能被射线命中。
    /// </summary>
    public class ObjectMetadata : MonoBehaviour
    {
        [Tooltip("与后端 resolver 注册表一致，例如 cup_01")]
        public string targetId;

        [Tooltip("人类可读标签，例如 cup")]
        public string objectLabel;

        [TextArea]
        [Tooltip("可选：本地描述（V0 后端不依赖它，仅备注用）")]
        public string description;
    }
}
