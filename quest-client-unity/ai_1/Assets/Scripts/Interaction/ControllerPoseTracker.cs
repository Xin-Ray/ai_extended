using UnityEngine;
using UnityEngine.XR;

namespace QuestAssistant
{
    // 把本物体的 local 姿态每帧对齐到指定手的控制器（用 XR Input 设备读取，不依赖 InputAction 资产）。
    // 挂在 XR Origin/Camera Offset 下的一个空物体上，PointingController 的 rayTransform 指它即可。
    // 设备无效（手柄没拿起/没追踪）时保持上一姿态，避免射线乱跳。
    public class ControllerPoseTracker : MonoBehaviour
    {
        public XRNode node = XRNode.RightHand;

        [Tooltip("可选：没追踪到设备时关掉激光等子物体")]
        public GameObject[] showWhileTracked;

        private bool _lastValid;

        private void Update()
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            bool valid = device.isValid;

            if (valid)
            {
                if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                    transform.localPosition = pos;
                if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                    transform.localRotation = rot;
            }

            if (valid != _lastValid)
            {
                _lastValid = valid;
                if (showWhileTracked != null)
                    foreach (var go in showWhileTracked)
                        if (go != null) go.SetActive(valid);
            }
        }
    }
}
