using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BaseTemplate.Views
{
    public class InputView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private event Action<Vector2> _pointerDown;
        private event Action<Vector2> _pointerUp;

        private void Awake()
            => ViewHolder.AddView(this);
        
        public void SubscribeToPointerUp(Action<Vector2> callback)
            => _pointerUp += callback;
        public void SubscribeToPointerDown(Action<Vector2> callback) 
            => _pointerDown += callback;

        public void OnPointerDown(PointerEventData eventData)
            => _pointerDown?.Invoke(eventData.position);
        public void OnPointerUp(PointerEventData eventData)
            => _pointerUp?.Invoke(eventData.position);
    }
}