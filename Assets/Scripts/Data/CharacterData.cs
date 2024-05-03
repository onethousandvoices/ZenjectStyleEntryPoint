using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Character", menuName = "Data/Character")]
    public class CharacterData : BaseData
    {
        [field: SerializeField] public int Level { get; private set; }
        [field: SerializeField] public Sprite Avatar { get; private set; }
        [field: SerializeField] public GameObject Model { get; private set; }
    }
}