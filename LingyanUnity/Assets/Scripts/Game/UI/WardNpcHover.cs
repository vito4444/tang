using Lingyan.Game.World3D;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 坊景内鼠标悬停 NPC：射线拾取立标（标记根名 Npc_&lt;id&gt;），
    /// 弹出好感明细浮签（规格第六节的悬停面板）；点击进入互动屏。
    /// 仅坊景屏启用（由 GameController 控制 enabled）。
    /// </summary>
    public sealed class WardNpcHover : MonoBehaviour
    {
        private GameController _c;
        private RectTransform _uiRoot;
        private string _hoverNpcId;
        private RectTransform _tip;

        public void Bind(GameController c, RectTransform uiRoot)
        {
            _c = c;
            _uiRoot = uiRoot;
        }

        private void Update()
        {
            if (_c == null || _c.MainCamera == null || _uiRoot == null)
            {
                return;
            }

            string hit = ProbeNpc();
            if (hit != _hoverNpcId)
            {
                _hoverNpcId = hit;
                if (_tip != null)
                {
                    Destroy(_tip.gameObject);
                    _tip = null;
                }
                if (_hoverNpcId != null && _c.ActiveSave != null)
                {
                    _tip = NpcPanelRenderer.BuildHoverTip(_c, _uiRoot, _hoverNpcId);
                }
            }

            if (_hoverNpcId != null && Input.GetMouseButton(0))
            {
                string id = _hoverNpcId;
                ClearTip();
                NpcScreen.Reset();
                _c.GoNpc(id);
            }
        }

        private void OnDisable()
        {
            ClearTip();
        }

        private void ClearTip()
        {
            _hoverNpcId = null;
            if (_tip != null)
            {
                Destroy(_tip.gameObject);
                _tip = null;
            }
        }

        /// <summary>射线拾取：命中体向上找 Npc_ 前缀根，返回 npcId。</summary>
        private string ProbeNpc()
        {
            Ray ray = _c.MainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 300f))
            {
                return null;
            }
            Transform node = hit.collider != null ? hit.collider.transform : null;
            while (node != null)
            {
                if (node.name.StartsWith("Npc_"))
                {
                    return node.name.Substring(4);
                }
                node = node.parent;
            }
            return null;
        }
    }
}
