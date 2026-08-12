using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 悬停括饰：指针入内时给按钮文字加「 」引号框（唐引号），移出复原。
    /// 与 ColorBlock 的朱砂变色叠加，构成"形 + 色"双反馈。
    /// </summary>
    public sealed class HoverBrackets : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TMP_Text _label;
        private Button _button;
        private string _plain;

        public void Bind(TMP_Text label, Button button)
        {
            _label = label;
            _button = button;
            _plain = label.text;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_label != null && (_button == null || _button.interactable))
            {
                _label.text = "\u300c " + _plain + " \u300d";
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_label != null)
            {
                _label.text = _plain;
            }
        }
    }
}
