using System;
using System.Collections.Generic;
using System.Linq;
using BaseTemplate.Interfaces;
using UnityEngine;

namespace BaseTemplate
{
    /// <summary>
    /// This class uses reflection methods and boxing because its main purpose is to be used
    /// only during startup or reload, so we can sacrifice a bit of performance.
    /// </summary>
    public class ControllerHolder : IControllerHolder, IGetController
    {
        private readonly Dictionary<Type, List<object>> _controllers = new();

        public void AddController<T>(T controller)
        {
            var implementedInterfaces = controller.GetType().GetInterfaces();
            
            foreach (var implementedInterface in implementedInterfaces)
            {
                _controllers.TryAdd(implementedInterface, new());
                _controllers[implementedInterface].Add(controller);
            }
        }
        
        public T GetController<T>()
        {
            _controllers.TryGetValue(typeof(T), out var controllersList);
            if (controllersList is { Count: > 0 })
                return (T)controllersList[0];
            Debug.LogError($"Controller of type {typeof(T)} wasn't found");
            return default;
        }

        public T[] GetControllers<T>()
        {
            _controllers.TryGetValue(typeof(T), out var controllersList);
            if (controllersList is { Count: > 0 })
                return controllersList.Cast<T>().ToArray();
            Debug.LogError($"Controllers of type {typeof(T)} wasn't found");
            return default;
        }
    }
}