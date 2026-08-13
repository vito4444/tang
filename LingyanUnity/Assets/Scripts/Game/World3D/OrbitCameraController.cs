using UnityEngine;

namespace Lingyan.Game.World3D
{
    /// <summary>
    /// 轨道相机：绕焦点环视（右键拖动）、滚轮远近、方向键/WASD 平移焦点。
    /// 阶段 8 接手柄映射时在此扩展。
    /// </summary>
    public sealed class OrbitCameraController : MonoBehaviour
    {
        public Vector3 Target = new Vector3(0f, 1.5f, -4f);

        /// <summary>绕 y 角（度）。</summary>
        public float Yaw = 170f;

        /// <summary>俯角（度），限 12–70。</summary>
        public float Pitch = 38f;

        public float Distance = 30f;

        public const float MinPitch = 12f;
        public const float MaxPitch = 70f;
        public const float MinDistance = 10f;
        public const float MaxDistance = 46f;

        private void Update()
        {
            if (Input.GetMouseButton(1))
            {
                Yaw += Input.GetAxis("Mouse X") * 3.2f;
                Pitch = Mathf.Clamp(Pitch - Input.GetAxis("Mouse Y") * 2.6f, MinPitch, MaxPitch);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                Distance = Mathf.Clamp(Distance - scroll * 9f, MinDistance, MaxDistance);
            }

            float panX = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                       - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float panZ = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                       - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            if (panX != 0f || panZ != 0f)
            {
                Quaternion yawOnly = Quaternion.Euler(0f, Yaw, 0f);
                Vector3 move = yawOnly * new Vector3(panX, 0f, panZ) * (Time.deltaTime * 12f);
                Target += move;
                Target = new Vector3(
                    Mathf.Clamp(Target.x, -34f, 34f), Target.y, Mathf.Clamp(Target.z, -34f, 30f));
            }

            ApplyTransform();
        }

        /// <summary>立即摆位（截图与初始化用，不等 Update）。</summary>
        public void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -Distance);
            transform.position = Target + offset;
            transform.rotation = rotation;
        }
    }
}
