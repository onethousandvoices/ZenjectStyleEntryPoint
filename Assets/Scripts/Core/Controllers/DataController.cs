using System;
using System.Collections.Generic;
using System.Linq;
using BaseTemplate.Attributes;
using BaseTemplate.Interfaces;
using Data;
using UnityEngine;

namespace BaseTemplate.Controllers
{
    [Controller]
    public class DataController : IData, IInit
    {
        private readonly Dictionary<Type, List<BaseData>> _datas = new();

        public void Init()
        {
            var allData = Resources.LoadAll<BaseData>("Data");

            foreach (var baseData in allData)
            {
                var type = baseData.GetType();
                _datas.TryAdd(type, new());
                _datas[type].Add(baseData);
            }
        }
        
        public T[] GetDatasOfType<T>()
        {
            var type = typeof(T);
            _datas.TryGetValue(type, out var datas);
            if (datas != null)
                return datas.Cast<T>().ToArray();
            Debug.LogError($"Data wasn't found for type {type}");
            return default;
        }
    }
}