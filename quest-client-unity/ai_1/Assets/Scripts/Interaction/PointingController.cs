using UnityEngine;
using UnityEngine.XR;

namespace QuestAssistant
{
    // 从 rayTransform（控制器/手）发一条 Physics 射线，命中带 ObjectMetadata 的物体。
    // 稳定选中策略：同一物体连续命中 dwell 秒才确认选中；确认后进入 cooldown，避免每帧狂发请求。
    // V0 先用控制器：把 rayTransform 指到 XR Origin 右手控制器即可；以后换手部追踪只需改这个 Transform。
    public class PointingController : MonoBehaviour
    {
        [Header("Refs")]
        public Transform rayTransform;
        public BackendClient client;
        [Tooltip("控制器没追踪到时回退的射线源（一般指 Main Camera 头部注视）")]
        public Transform fallbackRayTransform;
        [Tooltip("用于判断控制器是否在追踪的手节点")]
        public XRNode controllerNode = XRNode.RightHand;

        [Header("Raycast")]
        public float maxDistance = 10f;
        public LayerMask hitMask = ~0;

        [Header("Stable select")]
        [Tooltip("同一物体连续命中多少秒才算选中")]
        public float dwellSeconds = 0.35f;
        [Tooltip("一次选中后多少秒内不再对同一物体重复发送")]
        public float cooldownSeconds = 1.5f;

        [Header("Feedback")]
        public Color highlightColor = new Color(1f, 0.8f, 0.2f);
        public LineRenderer rayLine; // 可选：调试射线

        private ObjectMetadata _hover;
        private float _hoverStart;
        private ObjectMetadata _selected;
        private float _lastSendTime = -999f;

        private Renderer _highlighted;
        private MaterialPropertyBlock _mpb;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor"); // URP Lit

        private void Awake() => _mpb = new MaterialPropertyBlock();

        private void Update()
        {
            // 控制器在追踪就用控制器，否则回退到注视（头显里手柄没拿起也能用）。
            Transform src = rayTransform;
            var device = InputDevices.GetDeviceAtXRNode(controllerNode);
            if ((!device.isValid || rayTransform == null) && fallbackRayTransform != null)
                src = fallbackRayTransform;
            if (src == null) return;

            Vector3 origin = src.position;
            Vector3 dir = src.forward;

            bool hit = Physics.Raycast(origin, dir, out RaycastHit info, maxDistance, hitMask);
            UpdateRayLine(origin, hit ? info.point : origin + dir * maxDistance, hit);

            ObjectMetadata om = null;
            if (hit) om = info.collider.GetComponentInParent<ObjectMetadata>();

            if (om == null)
            {
                _hover = null;
                return;
            }

            if (om != _hover)
            {
                _hover = om;
                _hoverStart = Time.time;
                return;
            }

            // 同一物体持续命中
            bool dwelled = Time.time - _hoverStart >= dwellSeconds;
            bool isNewTarget = om != _selected;
            bool offCooldown = Time.time - _lastSendTime >= cooldownSeconds;

            if (dwelled && (isNewTarget || offCooldown))
            {
                Select(om, origin, dir);
            }
        }

        private void Select(ObjectMetadata om, Vector3 origin, Vector3 dir)
        {
            _selected = om;
            _lastSendTime = Time.time;
            Highlight(om);
            if (client != null && !string.IsNullOrEmpty(om.targetId))
                client.SendPointing(om.targetId, om.label, origin, dir);
            else
                Debug.LogWarning("[PointingController] no client or empty targetId on " + om.name);
        }

        private void Highlight(ObjectMetadata om)
        {
            var r = om.GetComponentInChildren<Renderer>();
            if (_highlighted != null && _highlighted != r)
            {
                _highlighted.SetPropertyBlock(null); // 清掉旧高亮
            }
            if (r != null)
            {
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(ColorId, highlightColor);
                r.SetPropertyBlock(_mpb);
                _highlighted = r;
            }
        }

        private void UpdateRayLine(Vector3 a, Vector3 b, bool hit)
        {
            Debug.DrawLine(a, b, hit ? Color.green : Color.gray);
            if (rayLine == null) return;
            rayLine.positionCount = 2;
            rayLine.SetPosition(0, a);
            rayLine.SetPosition(1, b);
        }
    }
}
