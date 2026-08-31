using UnityEngine;

namespace VRTraining
{
    [DisallowMultipleComponent]
    public sealed class InspectableItem : MonoBehaviour
    {
        [SerializeField]
        private EquipmentId equipmentId;

        public EquipmentId Id => equipmentId;
    }
}