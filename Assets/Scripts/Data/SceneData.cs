﻿using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Data
{
    [CreateAssetMenu(fileName = "Scene", menuName = "Data/Scene")]
    public class SceneData : BaseData
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public Sprite Sprite { get; private set; }

        public void Callback()
            => SceneManager.LoadScene(ID);
    }
}