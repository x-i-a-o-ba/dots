using UnityEngine;
using Unity.Entities;
public class FindTargetAuthoring : MonoBehaviour
{
    public float range;
    public Faction targetFaction;
    public class Baker : Baker<FindTargetAuthoring>
    {
        public override void Bake(FindTargetAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FindTarget{
                range = authoring.range,
                targetFaction = authoring.targetFaction,
            });
        }
    }



}


public struct FindTarget : IComponentData
{
    public float range;
    public Faction targetFaction;
}