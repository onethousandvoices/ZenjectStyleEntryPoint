using BaseTemplate.Attributes;
using BaseTemplate.Enums;
using BaseTemplate.Interfaces;
using BaseTemplate.Views.UI;
using Data;
using UnityEngine;

namespace BaseTemplate.Controllers
{
    [Controller, MustBeAfter(typeof(DataController))]
    public class UIController : IInit
    {
        [Inject] private readonly IData _data;
        [Inject] private readonly IInput _input;
        [Inject] private readonly MainCanvasView _mainCanvasView;

        private BaseData[] _currentData;
        private int _pointer;
        
        public void Init()
        {
            _mainCanvasView.Init(
                _mainCanvasView.ShowSideButtons, 
                Showcase<SceneData>, 
                Showcase<CharacterData>);
            
            _input.SubscribeTo(InputType.Left, DecrementPointer);
            _input.SubscribeTo(InputType.Right, IncrementPointer);
        }

        private void DecrementPointer()
        {
            if (_currentData == null)
                return;
            _pointer--;
            _pointer = Mathf.Clamp(_pointer, 0, _currentData.Length - 1);
            _mainCanvasView.ShowItem(_pointer);
        }

        private void IncrementPointer()
        {
            if (_currentData == null)
                return;
            _pointer++;
            _pointer = Mathf.Clamp(_pointer, 0, _currentData.Length - 1);
            _mainCanvasView.ShowItem(_pointer);
        }
        
        private void Showcase<T>()
        {
            _pointer = 0;
            _mainCanvasView.HideAll();
            _currentData = _data.GetDatasOfType<T>() as BaseData[];
            _mainCanvasView.StartShowcase(_currentData);
            _mainCanvasView.ShowItem(_pointer);
        }
    }
}