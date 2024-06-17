#define STOPWATCH
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BaseTemplate.Attributes;
using BaseTemplate.Interfaces;
using Controllers;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BaseTemplate
{
    public class EntryPoint : MonoBehaviour
    {
        private static EntryPoint _instance;
        private readonly Dictionary<Type, List<object>> _controllers = new();
        private ITick[] _ticks;
        
        private bool _isInit;
        private bool _isLoaded;

#if UNITY_EDITOR
        private Action<PauseState> _onPause;
#endif
        
        private void Awake()
        {
            if (_instance != null)
                Destroy(_instance.gameObject);
            _instance = this;
        }

        private void Start()
        {
#if STOPWATCH
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            CreateControllers();
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
                Debug.Log("Init completed");
                _isInit = true;
            }
            catch (Exception)
            {
                throw new($"Init failed at {current?.GetType().Name}");
            }
            
#if STOPWATCH
            stopwatch.Stop();
            Debug.LogError("*****************************************************");
            Debug.LogError($"EntryPoint start execution time: {stopwatch.ElapsedMilliseconds} ms");
            Debug.LogError("*****************************************************");
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

        /// <summary>
        /// These are two main methods for getting dependencies via [Inject] fields or via constructor of a class.
        /// Consider modifying methods if there are some new types of dependencies not declared below.
        /// </summary>
        /// <returns>Dependency or null if it's not found</returns>
        private object GetDependency(Type requested)
        {
            if (requested.IsSubclassOf(typeof(MonoBehaviour)))
                return typeof(ViewHolder).GetMethod("GetView")!.MakeGenericMethod(requested).Invoke(this, new object[] { 0 });
            if (_controllers.ContainsKey(requested))
                return _controllers[requested][0];
            return requested == typeof(MonoBehaviour) 
                ? this : null;
        }

        [UsedImplicitly]
        private T[] GetDependencies<T>()
        {
            var requested = typeof(T);
            if (requested.IsSubclassOf(typeof(MonoBehaviour)))
                return (T[])typeof(ViewHolder).GetMethod("GetAllViews")!.MakeGenericMethod(requested).Invoke(this, null);
            if (_controllers.ContainsKey(requested))
                return _controllers[requested].Cast<T>().ToArray();
            throw new($"Dependencies wasn't fount for {requested.Name}");
        }
        
        /// <summary>
        /// If class ControllersQueue doesn't exist and compiler throws an error
        /// please use Tools/DIContainer/Create Queue to recreate this class.
        /// </summary>
        private void CreateControllers()
        {
            var orderedTypes = ControllersQueue.CurrentQueue;
            var parameters = new List<object>();
            var delayedControllers = new Dictionary<Type, Type[]>();

            for (int i = 0; i < orderedTypes.Length; i++)
            {
                var type = orderedTypes[i];
                parameters.Clear();
                var ctorParameters = type.GetConstructors()[0].GetParameters();
                bool shouldSkip = false;

                foreach (var param in ctorParameters)
                {
                    var requestedType = GetDependency(param.ParameterType);
                    if (requestedType != null)
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
                {
                    Debug.LogError($"{type.Name} depends on itself via constructor");
                    return;
                }

                var others = delayedControllers.Where(keyValuePair => keyValuePair.Key != type);
                foreach (var other in others)
                {
                    var otherInterfaces = other.Key.GetInterfaces();
                    if (!interfaces.Any(i => other.Value.Contains(i)) || !otherInterfaces.Any(i => typeParameters.Contains(i)))
                        continue;
                    Debug.LogError($"{type.Name} has cross dependencies with {other.Key.Name}");
                    return;
                }

                var dependencies = typeParameters.Select(parameter =>
                {
                    var dependency = GetDependency(parameter);
                    if (dependency == null)
                        throw new($"Dependency wasn't found for parameter {parameter} of {type} constructor");
                    return dependency;
                }).ToArray();
                CreateController(type, dependencies);
            }
        }

        private object CreateController(Type type, object[] parameters)
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

            return controller;
        }

        private void InjectControllers()
        {
            foreach (var key in _controllers.Keys)
            foreach (var controller in _controllers[key])
                InjectControllerImpl(controller);
            InjectControllerImpl(this);
        }

        private void InjectControllerImpl(object controller)
        {
            var type = controller.GetType();

            while (true)
            {
                var fields = type!.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    if (!Attribute.IsDefined(field, typeof(InjectAttribute)))
                        continue;

                    if (field.FieldType.IsArray)
                    {
                        var dependencies = typeof(EntryPoint).GetMethod("GetDependencies", BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(field.FieldType.GetElementType())
                            .Invoke(this, null);
                        field.SetValue(controller, dependencies);
                    }
                    else
                    {
                        var dependency = GetDependency(field.FieldType);
                        if (dependency == null)
                            throw new($"Dependency wasn't found for {field.FieldType.Name} of {type.Name}");
                        field.SetValue(controller, dependency);
                    }
                }

                foreach (var property in properties)
                {
                    if (!Attribute.IsDefined(property, typeof(InjectAttribute)))
                        continue;

                    if (property.PropertyType.IsArray)
                    {
                        var dependencies = typeof(EntryPoint).GetMethod("GetDependencies", BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(property.PropertyType.GetElementType())
                            .Invoke(this, null);
                        property.SetValue(controller, dependencies);
                    }
                    else
                    {
                        var dependency = GetDependency(property.PropertyType);
                        if (dependency == null)
                            throw new($"Dependency wasn't found for {property.PropertyType.Name} of {type.Name}");
                        property.SetValue(controller, dependency);
                    }
                }

                if (type.BaseType == typeof(object) || type.BaseType == typeof(MonoBehaviour))
                    return;
                type = type.BaseType;
            }
        }
        
        /// <summary>
        /// Direct controller creation must be declared only in constructor of a class.
        /// Otherwise created controller won't get in the injection queue. 
        /// </summary>
        public static T DirectCreate<T>(object[] parameters)
            => (T)_instance.CreateController(typeof(T), parameters);

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void ValidateControllers()
        {
            const string entryPointPath = "Assets/Scripts/DIContainer/EntryPoint.cs";
            const string controllerQueuePath = "Assets/Scripts/DIContainer/ControllersQueue.cs";

            if (!File.Exists(entryPointPath))
            {
                Debug.LogError($"EntryPoint.cs wasn't found at {entryPointPath}");
                return;
            }

            var controllerQueueLines = File.ReadAllLines(controllerQueuePath).ToList();
            int queueStartIndex = 0;

            for (int i = 0; i < controllerQueueLines.Count; i++)
            {
                if (!controllerQueueLines[i].Contains("CurrentQueue"))
                    continue;
                queueStartIndex = i + 2;
                break;
            }

            while (!controllerQueueLines[queueStartIndex].Contains("};"))
                controllerQueueLines.RemoveAt(queueStartIndex);

            var controllers = FindControllers(out var namespaces);
            var queueDirectives = new List<string>(); 
            var directivesToAdd = new List<string>();
            var lineToInsert = 0;

            for (int i = 0; i < controllerQueueLines.Count; i++)
            {
                var line = controllerQueueLines[i];
                if (line.Contains("using ") || line.Contains("namespace "))
                    queueDirectives.Add(line.Split()[1].Replace(";", ""));
                else if (string.IsNullOrEmpty(line))
                    lineToInsert = i;
            }
            
            for (int i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];
                var controllerNamespace = namespaces[controller];
                if (!queueDirectives.Contains(controllerNamespace) && !directivesToAdd.Contains(controllerNamespace))
                    directivesToAdd.Add(controllerNamespace);
                controllerQueueLines.Insert(queueStartIndex + i, $"{new string(' ', 12)}typeof({controller}),");
            }

            for (int i = 0; i < directivesToAdd.Count; i++)
            {
                var current = $"using {directivesToAdd[i]};";
                controllerQueueLines.Insert(lineToInsert + i, current);
            }

            File.WriteAllLines(controllerQueuePath, controllerQueueLines);
        }

        private static string[] FindControllers(out Dictionary<string, string> namespaces)
        {
            namespaces = new();
            var scripts = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories);
            var controllers = new List<string>();
            var allStacks = new List<Stack<string>>();

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

                    var namespaceLine = lines.First(l => l.Contains("namespace"));
                    var namespaceSplit = namespaceLine.Split();
                    var controllerNamespace = namespaceSplit[Array.IndexOf(namespaceSplit, "namespace") + 1];
                    var classLine = lines[i + 1];
                    var split = classLine.Split();
                    var controller = split[Array.IndexOf(split, "class") + 1];
                    var toAdd = $"{controller}{mustBeAfter}";
                    if (string.IsNullOrEmpty(toAdd))
                        break;
                    namespaces.Add(controller, controllerNamespace);
                    controllers.Add(toAdd);
                    break;
                }
            }

            controllers = controllers.OrderBy(x => x).ToList();

            while (controllers.Count > 0)
            {
                var current = controllers[0];
                var stack = new Stack<string>();
                allStacks.Add(stack);
                
                while (true)
                {
                    controllers.Remove(current);
                    var split = current.Split('.');
                    var currentController = split[0];
                    stack.Push(currentController);
                    if (split.Length < 2)
                        break;
                    var dependsOn = split[1];

                    if (currentController == dependsOn)
                    {
                        Debug.LogError($"{split[0]} requested the same priority. MustBeAfter type should differ");
                        throw new();
                    }

                    var requestedController = controllers.FirstOrDefault(s => s.StartsWith(dependsOn));
                    if (requestedController == null)
                    {
                        if (allStacks.Any(s => s.Last() == dependsOn))
                            break;

                        var allStacksList = allStacks.SelectMany(s => s).ToList();
                        var musBeAfterIndex = allStacksList.IndexOf(dependsOn);
                        if (musBeAfterIndex >= 0)
                        { 
                            var mustBeAfter = allStacksList[musBeAfterIndex + 1];
                            Debug.LogError($"{currentController} breaking controllers order. It either shouldn't have MustBeAfter attribute or {mustBeAfter} MustBeAfter attribute should refer to {currentController}");
                            throw new();
                        }
                        Debug.LogError($"Controller of type {dependsOn} wasn't found for {currentController} MustBeAfter attribute");
                        throw new();
                    }

                    var requestedSplit = requestedController.Split('.');
                    if (requestedSplit.Length > 1 && requestedSplit[1] == currentController)
                    {
                        Debug.LogError($"Controller of type {currentController} requested to be after {dependsOn} and {requestedController.Split('.')[0]} requested to be after {requestedController.Split('.')[1]}");
                        throw new();
                    }

                    current = requestedController;
                }
            }
            
            return allStacks.SelectMany(s => s).ToArray();
        }
        
        [MenuItem("Tools/DIContainer/Create Queue")]
        private static void CreateControllersQueueClass()
        {
            const string path = "Assets/Scripts/DIContainer/ControllersQueue.cs";
            string[] template = 
            {
                "using System;",
                "",
                "namespace Controllers",
                "{",
                "    public static class ControllersQueue",
                "    {",
                "        /// <summary>",
                "        /// The list below shows call order of controllers",
                "        /// This part of the code is generated automatically every domain reload",
                "        /// </summary>",
                "        public static readonly Type[] CurrentQueue =",
                "        {",
                "        };",
                "    }",
                "}"
            };

            File.WriteAllLines(path, template);
            AssetDatabase.Refresh();
        }
#endif
    }
}
