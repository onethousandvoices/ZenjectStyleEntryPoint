using System;
using BaseTemplate.Attributes;
using BaseTemplate.Enums;
using BaseTemplate.Interfaces;
using BaseTemplate.Views;
using JetBrains.Annotations;
using UnityEngine;

namespace BaseTemplate.Controllers
{
    [Controller, UsedImplicitly]
    public class InputController : IInput, IInit, ITick
    {
        private event Action _leftEvent;
        private event Action _rightEvent;
        private event Action _upEvent;
        private event Action _downEvent;

        private Vector2 _mouseInput;

        private const int MOVE_TOLERANCE = 50;

        public void Init()
        {
            //only for debugging
            SubscribeTo(InputType.Left, () => Debug.Log("Left"));
            SubscribeTo(InputType.Right, () => Debug.Log("Right"));
            SubscribeTo(InputType.Up, () => Debug.Log("Up"));
            SubscribeTo(InputType.Down, () => Debug.Log("Down"));

            var inputView = ViewHolder.GetView<InputView>();
            inputView.SubscribeToPointerDown(value => _mouseInput = value);
            inputView.SubscribeToPointerUp(SwipeHandler);
        }
        
        public void Tick()
        {
            if (Input.GetKeyUp(KeyCode.LeftArrow))
                _leftEvent?.Invoke();
            else if (Input.GetKeyUp(KeyCode.RightArrow))
                _rightEvent?.Invoke();
            else if (Input.GetKeyUp(KeyCode.UpArrow))
                _upEvent?.Invoke();
            else if (Input.GetKeyUp(KeyCode.DownArrow))
                _downEvent?.Invoke();
        }

        private void SwipeHandler(Vector2 currentPosition)
        {
            switch (Mathf.Abs(currentPosition.x - _mouseInput.x))
            {
                case >= MOVE_TOLERANCE when currentPosition.x <= _mouseInput.x:
                    _rightEvent?.Invoke();
                    break;
                case >= MOVE_TOLERANCE when currentPosition.x >= _mouseInput.x:
                    _leftEvent?.Invoke();
                    break;
                default:
                {
                    switch (Mathf.Abs(currentPosition.y - _mouseInput.y))
                    {
                        case >= MOVE_TOLERANCE when currentPosition.y <= _mouseInput.y:
                            _upEvent?.Invoke();
                            break;
                        case >= MOVE_TOLERANCE when currentPosition.y >= _mouseInput.y:
                            _downEvent?.Invoke();
                            break;
                    }
                    break;
                }
            }

            _mouseInput = Vector2.zero;
        }

        public void SubscribeTo(InputType type, Action callback)
        {
            switch (type)
            {
                case InputType.Left:
                    _leftEvent += callback;
                    break;
                case InputType.Right:
                    _rightEvent += callback;
                    break;
                case InputType.Up:
                    _upEvent += callback;
                    break;
                case InputType.Down:
                    _downEvent += callback;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void UnsubscribeFrom(InputType type, Action callback)
        {
            switch (type)
            {
                case InputType.Left:
                    _leftEvent -= callback;
                    break;
                case InputType.Right:
                    _rightEvent -= callback;
                    break;
                case InputType.Up:
                    _upEvent -= callback;
                    break;
                case InputType.Down:
                    _downEvent -= callback;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}