using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Mavis.VisualStyles
{
    public sealed class VisualStyleCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action entered, exited;
        public void OnPointerEnter(PointerEventData data) => entered?.Invoke();
        public void OnPointerExit(PointerEventData data) => exited?.Invoke();
    }
}
