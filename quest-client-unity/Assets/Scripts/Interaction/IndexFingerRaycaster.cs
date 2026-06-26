using QuestSpatialAssistant.Networking;
using UnityEngine;

namespace QuestSpatialAssistant.Interaction
{
    /// <summary>
    /// 从右手食指尖沿指向方向发射线，命中带 ObjectMetadata 的 Collider。
    /// 连续指向同一物体 ≥ dwellSeconds 后触发一次上报（去抖），并高亮命中物体。
    ///
    /// 关于食指来源（fingerTip）：
    ///   - 推荐把本脚本的 fingerTip 指到 Meta XR Interaction SDK 里右手食指指尖的 Transform
    ///     （OVRHand / OVRSkeleton 的 Hand_IndexTip 骨骼，或 Interaction SDK 的 HandJoint）。
    ///   - 调试阶段可临时拖一个空物体当作手指，先把射线-命中-上报链路在 Editor 里验证通。
    /// </summary>
    public class IndexFingerRaycaster : MonoBehaviour
    {
        [Header("射线来源")]
        [Tooltip("右手食指指尖 Transform；其 forward 作为指向方向")]
        public Transform fingerTip;

        [Tooltip("射线最大距离（米）")]
        public float maxDistance = 5f;

        [Tooltip("只命中这些层（默认 Everything）")]
        public LayerMask layerMask = ~0;

        [Header("去抖")]
        [Tooltip("连续指向同一物体多少秒后才上报")]
        public float dwellSeconds = 1.0f;

        [Header("依赖")]
        public BackendClient backendClient;

        [Tooltip("可选：UI 反馈（命中标签/结果显示）")]
        public ResponseDisplay display;

        [Header("高亮")]
        [Tooltip("命中时给物体临时换上的高亮材质；留空则用自发光着色")]
        public Material highlightMaterial;

        private ObjectMetadata _current;     // 当前持续指向的物体
        private float _dwellTimer;
        private bool _firedForCurrent;       // 本次停留是否已上报，避免重复发

        // 高亮还原
        private Renderer _highlightedRenderer;
        private Material[] _originalMaterials;

        private void Update()
        {
            if (fingerTip == null) return;

            ObjectMetadata hit = Raycast();

            if (hit != _current)
            {
                // 指向目标变了：重置计时与高亮
                ClearHighlight();
                _current = hit;
                _dwellTimer = 0f;
                _firedForCurrent = false;
                if (_current != null)
                {
                    ApplyHighlight(_current);
                    if (display != null) display.ShowDetecting(_current.objectLabel);
                }
            }
            else if (_current != null && !_firedForCurrent)
            {
                _dwellTimer += Time.deltaTime;
                if (_dwellTimer >= dwellSeconds)
                {
                    Fire(_current);
                    _firedForCurrent = true;
                }
            }
        }

        private ObjectMetadata Raycast()
        {
            if (Physics.Raycast(fingerTip.position, fingerTip.forward,
                    out RaycastHit hitInfo, maxDistance, layerMask))
            {
                return hitInfo.collider.GetComponentInParent<ObjectMetadata>();
            }
            return null;
        }

        private void Fire(ObjectMetadata meta)
        {
            Debug.Log($"[Raycaster] pointing confirmed: {meta.targetId}");
            if (backendClient != null)
            {
                backendClient.SendPointingEvent(
                    meta.targetId, meta.objectLabel,
                    fingerTip.position, fingerTip.forward);
            }
        }

        private void ApplyHighlight(ObjectMetadata meta)
        {
            var rend = meta.GetComponentInChildren<Renderer>();
            if (rend == null) return;
            _highlightedRenderer = rend;
            _originalMaterials = rend.sharedMaterials;

            if (highlightMaterial != null)
            {
                var mats = new Material[_originalMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = highlightMaterial;
                rend.sharedMaterials = mats;
            }
            else
            {
                // 没给高亮材质就开自发光（URP/Lit 支持 _EmissionColor）
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", Color.cyan * 0.6f);
            }
        }

        private void ClearHighlight()
        {
            if (_highlightedRenderer != null && _originalMaterials != null)
            {
                _highlightedRenderer.sharedMaterials = _originalMaterials;
            }
            _highlightedRenderer = null;
            _originalMaterials = null;
        }
    }
}
