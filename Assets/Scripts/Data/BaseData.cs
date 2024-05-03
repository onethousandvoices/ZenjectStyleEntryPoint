using UnityEngine;

namespace Data
{
    public abstract class BaseData : ScriptableObject
    {
        [field: SerializeField] public string ID { get; private set; }
    }
}