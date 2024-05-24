#define STRESSTEST
#define STOPWATCH
#define EXTRALOGGING
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BaseTemplate.Attributes;
using BaseTemplate.Controllers;
using BaseTemplate.Interfaces;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BaseTemplate
{
    public static class ControllersQueueDisplay
    {
        /// <summary>
        /// The list below shows call order of controllers
        /// This part of the code is generated automatically
        /// </summary>
        public static readonly Type[] CurrentQueue =
        {
            typeof(DataController),
            typeof(InputController),
            typeof(TestController),
            typeof(TestInject),
            typeof(UIController),
        };
    }

    public class EntryPoint : MonoBehaviour
    {
        private readonly Dictionary<Type, List<object>> _controllers = new();
        private ITick[] _ticks;
        private bool _isInit;

        private void Start()
        {
#if STOPWATCH
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

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
                    Debug.LogError(current.GetType().Name);
                }

#if EXTRALOGGING
                Debug.Log("Init completed");
#endif
                _isInit = true;
            }
            catch (Exception)
            {
                throw new($"Init failed at {current?.GetType().Name}");
            }

#if STOPWATCH
            stopwatch.Stop();
            Debug.Log($"EntryPoint start execution time: {stopwatch.ElapsedMilliseconds} ms");
#endif
#if EXTRALOGGING
            Debug.Log($"Controllers count {inits.Length}");
#endif
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
        
        private object GetDependency(Type requested)
        {
            var isView = requested.IsSubclassOf(typeof(MonoBehaviour));
            if (isView)
                return ViewHolder.GetView(requested);
            if (_controllers.ContainsKey(requested))
                return _controllers[requested][0];
            throw new($"Dependency wasn't fount for {requested.Name}");
        }

        private void FindAndCacheControllers()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name.Contains("Assembly-CSharp"));
            var types = assembly.GetTypes();
            var orderedTypes = types.Where(type => Attribute.IsDefined(type, typeof(ControllerAttribute))).ToList();
            var alreadyOrderedTypes = new List<Type>();
            var parameters = new List<object>();
            var delayedControllers = new Dictionary<Type, Type[]>();

            for (int i = 0; i < orderedTypes.Count; i++)
            {
                var currentController = orderedTypes[i];
                if (alreadyOrderedTypes.Contains(currentController))
                    continue;

                if (currentController.GetCustomAttribute(typeof(MustBeAfterAttribute)) is not MustBeAfterAttribute mustBeAfterAttribute)
                    continue;

                var mustBeAfter = orderedTypes.IndexOf(mustBeAfterAttribute!.InitAfter);

                if (mustBeAfterAttribute.InitAfter == currentController)
                {
                    Debug.LogError($"{currentController.Name} requested the same priority. MustBeAfter type should differ");
                    continue;
                }

                orderedTypes.Insert(mustBeAfter + 1, currentController);
                orderedTypes.RemoveAt(i + 1);
                alreadyOrderedTypes.Add(currentController);
            }

            for (int i = 0; i < orderedTypes.Count; i++)
            {
                var type = orderedTypes[i];
#if STRESSTEST
                if (type == typeof(TestController))
                {
                    for (int j = 0; j < 10000; j++)
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
                parameters.Clear();
                var ctorParameters = type.GetConstructors()[0].GetParameters();
                bool shouldSkip = false;

                foreach (var param in ctorParameters)
                {
                    var requestedType = param.ParameterType;
                    if (requestedType.IsSubclassOf(typeof(MonoBehaviour)) || _controllers.ContainsKey(requestedType))
                    {
                        parameters.Add(requestedType);
                        continue;
                    }
                    delayedControllers.Add(type, ctorParameters.Select(p => p.ParameterType).ToArray());
                    shouldSkip = true;
                }

                if (shouldSkip)
                    continue;

                CreateController(type, parameters.ToArray());
            }

            foreach (var (type, value) in delayedControllers)
            {
                var interfaces = type.GetInterfaces();
                var typeParameters = value;

                if (interfaces.Any(i => typeParameters.Contains(i)))
                    throw new($"{type.Name} depends on itself via constructor");

                var others = delayedControllers.Where(kvp => kvp.Key != type);
                foreach (var other in others)
                {
                    var otherInterfaces = other.Key.GetInterfaces();
                    if (interfaces.Any(i => other.Value.Contains(i)) && otherInterfaces.Any(i => typeParameters.Contains(i)))
                        throw new($"{type.Name} has cross dependencies with {other.Key.Name}");
                }

                CreateController(type, typeParameters.Select(GetDependency).ToArray());
            }
        }

        private void CreateController(Type type, object[] parameters)
        {
            var controller = parameters.Length > 0
                ? Activator.CreateInstance(type, parameters)
                : Activator.CreateInstance(type);
            var implementedInterfaces = type.GetInterfaces();

            foreach (var implementedInterface in implementedInterfaces)
            {
                _controllers.TryAdd(implementedInterface, new());
                _controllers[implementedInterface].Add(controller);
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
                        field.SetValue(controller, GetDependency(field.FieldType));
                    }
                }
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void UpdateQueue()
        {
            const string entryPointPath = "Assets/Scripts/Core/EntryPoint.cs";

            if (!File.Exists(entryPointPath))
                throw new($"EntryPoint.cs wasn't found at {entryPointPath}");

            var entryPointLines = File.ReadAllLines(entryPointPath).ToList();
            int helperClassQueueStartIndex = 0;

            for (int i = 0; i < entryPointLines.Count; i++)
            {
                if (!entryPointLines[i].Contains("CurrentQueue"))
                    continue;
                helperClassQueueStartIndex = i + 2;
                break;
            }

            while (!entryPointLines[helperClassQueueStartIndex].Contains("};"))
                entryPointLines.RemoveAt(helperClassQueueStartIndex);

            var controllers = FindControllers();

            for (int i = 0; i < controllers.Length; i++)
                entryPointLines.Insert(helperClassQueueStartIndex + i, $"{new string(' ', 12)}typeof({controllers[i]}),");

            File.WriteAllLines(entryPointPath, entryPointLines);
        }

        private static string[] FindControllers()
        {
            var scripts = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories);
            var controllers = new List<string>();
            var alreadyOrderedTypes = new List<string>();

            foreach (var script in scripts)
            {
                var lines = File.ReadAllLines(script);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].Contains("[Controller") || lines[i].Contains("Attribute"))
                        continue;

                    var mustBeAfter = string.Empty;

                    if (lines[i].Contains("MustBeAfter"))
                        mustBeAfter = $".{lines[i].Split("typeof")[^1].Replace("(", "").Replace("))]", "")}";

                    var classLine = lines[i + 1];
                    var split = classLine.Split();
                    controllers.Add($"{split[Array.IndexOf(split, "class") + 1]}{mustBeAfter}");
                    break;
                }
            }

            controllers = controllers.OrderBy(x => x).ToList();

            for (int i = 0; i < controllers.Count; i++)
            {
                var currentController = controllers[i];
                if (alreadyOrderedTypes.Contains(currentController))
                    continue;

                var split = currentController.Split('.');
                if (split.Length < 2)
                    continue;

                currentController = split[0];

                if (currentController == split[1])
                    throw new($"{split[0]} requested the same priority. MustBeAfter type should differ");

                var mustBeAfter = controllers.FindIndex(s => s.StartsWith(split[1]) && !s.Contains(currentController));

                if (mustBeAfter == -1)
                {
                    var requestedController = controllers.First(s => s.StartsWith(split[1]));
                    if (requestedController.Split('.')[1] == currentController)
                        throw new($"Controller of type {split[0]} requested to be after {split[1]} and {requestedController.Split('.')[0]} requested to be after {requestedController.Split('.')[1]}");
                    throw new("Something went wrong resolving dependencies");
                }

                if (mustBeAfter + 1 == i)
                {
                    controllers[i] = split[0];
                    continue;
                }

                controllers.Insert(mustBeAfter + 1, split[0]);
                controllers.RemoveAt(i + 1);
                alreadyOrderedTypes.Add(currentController);
            }

            return controllers.ToArray();
        }

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
