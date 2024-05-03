using System;
using System.Collections.Generic;
using Data;
using UnityEngine;
using UnityEngine.UI;

namespace BaseTemplate.Views.UI
{
    public class DataPresenterView : MonoBehaviour
    {
        [SerializeField] private DataView _dataViewPrefab;
        [SerializeField] private Button _closeShowcase;

        private readonly List<DataView> _pool = new();
        private readonly List<DataView> _activePrefabs = new();

        public void Init(Action onCloseShowcaseCallback)
            => _closeShowcase.onClick.AddListener(() => onCloseShowcaseCallback?.Invoke());

        public void SetData(BaseData[] datas)
        {
            ClearData();
            foreach (var baseData in datas)
                GetView().Init(baseData);
            _closeShowcase.gameObject.SetActive(true);
        }

        public void ShowItem(int index)
        {
            for (int i = 0; i < _activePrefabs.Count; i++)
                _activePrefabs[i].Show(i == index);
        }

        public void ShowCloseButton(bool state) 
            => _closeShowcase.gameObject.SetActive(state);
        
        /// <summary>
        /// Pool system is implemented here just for future.
        /// Significant performance loss is impossible at the moment.
        /// </summary>
        public void ClearData()
        {
            for (int i = 0; i < _activePrefabs.Count; i++)
            {
                var prefab = _activePrefabs[i];
                prefab.Show(false);
                _pool.Add(prefab);
                _activePrefabs[i] = null;
            }
            _activePrefabs.Clear();
        }

        private DataView GetView()
        {
            if (_pool.Count > 0)
            {
                var prefab = _pool[0];
                _pool.Remove(prefab);
                _activePrefabs.Add(prefab);
                return prefab;
            }

            var newPrefab = Instantiate(_dataViewPrefab, transform);
            _activePrefabs.Add(newPrefab);
            return _activePrefabs[^1];
        }
    }
}