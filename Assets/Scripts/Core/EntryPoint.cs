using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using BaseTemplate.Attributes;
using BaseTemplate.Controllers;
using BaseTemplate.Interfaces;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BaseTemplate
{
    public class EntryPoint : MonoBehaviour
    {
        private readonly Dictionary<Type, List<object>> _controllers = new();
        private ITick[] _ticks;
        private bool _isInit;

        private void Start()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            FindAndCacheControllers();
            InjectControllers();

            _ticks = GetControllers<ITick>();
            var inits = GetControllers<IInit>();
            
            IInit current = null;
            try
            {
                for (int i = 0; i < inits.Length; i++)
                {
                    current = inits[i];
                    current.Init();
                }
                Debug.Log("Init completed");
                _isInit = true;
            }
            catch (Exception)
            {
                throw new($"Init failed at {current?.GetType().Name}");
            }
            
            stopwatch.Stop();
            Debug.Log($"EntryPoint start execution time: {stopwatch.ElapsedMilliseconds} ms");
        }

        private void Update()
        {
            if (!_isInit)
                return;
            
            ITick current = null;
            try
            {
                for (int i = 0; i < _ticks.Length; i++)
                {
                    current = _ticks[i];
                    current.Tick();
                }
            }
            catch (Exception)
            {
                throw new($"Tick loop fail at {current?.GetType().Name}");
            }
        }

        private T[] GetControllers<T>()
        {
            _controllers.TryGetValue(typeof(T), out var controllersList);
            if (controllersList is { Count: > 0 })
                return controllersList.Cast<T>().ToArray();
            Debug.LogError($"Controllers of type {typeof(T)} wasn't found");
            return default;
        }
        
        private void FindAndCacheControllers()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name.Contains("Assembly-CSharp"));
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (!Attribute.IsDefined(type, typeof(ControllerAttribute)))
                    continue;
#if STRESSTEST
                if (type == typeof(TestController))
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        var controller1 = Activator.CreateInstance(type);
                        var implementedInterfaces1 = type.GetInterfaces();
            
                        foreach (var implementedInterface in implementedInterfaces1)
                        {
                            _controllers.TryAdd(implementedInterface, new());
                            _controllers[implementedInterface].Add(controller1);
                        }
                    }
                }
#endif

                var controller = Activator.CreateInstance(type);
                var implementedInterfaces = type.GetInterfaces();
            
                foreach (var implementedInterface in implementedInterfaces)
                {
                    _controllers.TryAdd(implementedInterface, new());
                    _controllers[implementedInterface].Add(controller);
                }
            }
        }

        private void InjectControllers()
        {
            var keys = _controllers.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var controllers = _controllers[key];
                foreach (var controller in controllers)
                {
                    var fields = controller.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
                    foreach (var field in fields)
                    {
                        if (!Attribute.IsDefined(field, typeof(InjectAttribute)))
                            continue;
                        var requested = field.FieldType;
                        var isView = requested.IsSubclassOf(typeof(MonoBehaviour));
                        if (isView)
                        {
                            var obj = ViewHolder.GetView(requested);
                            field.SetValue(controller, obj);
                        }
                        else
                        {
                            var obj = _controllers.ContainsKey(requested) 
                                ? _controllers[requested][0] 
                                : throw new($"Controllers doesn't contain requested type {requested.Name} for {controller.GetType().Name}");
                            field.SetValue(controller, obj);
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            //required for example workability
            var paths = Directory.GetFiles("Assets/Scenes");
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (string path in paths)
            {
                var correctPath = path.Replace(@"\", "/");
                if (correctPath.Contains("meta") || scenes.Any(s => s.path == correctPath))
                    continue;
                
                var newScene = new EditorBuildSettingsScene
                {
                    path = correctPath,
                    enabled = true
                };

                scenes.Add(newScene);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
#endif
    }
}