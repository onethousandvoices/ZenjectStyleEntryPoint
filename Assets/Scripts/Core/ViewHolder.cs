using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaseTemplate
{
    public static class ViewHolder
    {
        private static readonly Dictionary<(Type type, int id), MonoBehaviour> _holder = new();

        public static void AddView(MonoBehaviour value, int id = 0)
        {
            var key = value.GetType();

            if (!_holder.ContainsKey((key, id)))
            {
                _holder.Add((key, id), value);
            }
            else
            {
                while (_holder.ContainsKey((key, id)))
                    id++;
                _holder.Add((key, id), value);
            }
        }

        public static T GetView<T>(int id = 0) where T : MonoBehaviour
        {
            var key = typeof(T);
            return _holder.ContainsKey((key, id))
                ? (T)_holder[(key, id)]
                : default;
        }

        public static void Reset()
            => _holder.Clear();
    }
}