using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseTemplate.Controllers;
using BaseTemplate.Interfaces;
using UnityEditor;
using UnityEngine;

namespace BaseTemplate
{
    /// <summary>
    /// Single enter point of a project guarantees determined init order
    /// and also optimizing Unity's Update function
    /// </summary>
    public class Root : MonoBehaviour
    {
        private readonly ControllerHolder _holder = new();
        
        private IInit[] _inits;
        private ITick[] _ticks;
        
        /// <summary>
        /// Try catch blocks below guarantee further execution even if some classes throw an exception.
        /// Small performance drop in return for stability and soft lock protection (sort of)
        /// </summary>
        private void Start()
        {
            _holder.AddController(new InputController());
            _holder.AddController(new DataController());
            _holder.AddController(new UIController());
            
            var getControllers = _holder.GetControllers<IGetControllers>();
            for (int i = 0; i < getControllers?.Length; i++)
                getControllers[i].GetController(_holder);
            Debug.Log("Getting controllers completed");

            _inits = _holder.GetControllers<IInit>();
            _ticks = _holder.GetControllers<ITick>();
            
            try
            {
                for (int i = 0; i < _inits.Length; i++)
                    _inits[i].Init();
                Debug.Log("Init completed");
            }
            catch (Exception)
            {
                Debug.LogError("Init failed");
                throw;
            }
        }

        private void Update()
        {
            try
            {
                for (int i = 0; i < _ticks.Length; i++)
                    _ticks[i].Tick();
            }
            catch (Exception)
            {
                Debug.LogError("Tick loop fail");
                throw;
            }
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
    }
}