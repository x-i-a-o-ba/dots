using UnityEngine;
using Unity.Entities;
public class HealthAuthoring : MonoBehaviour
{
    public float healthAmount;

    public class Baker : Baker<HealthAuthoring>
    {
        public override void Bake(HealthAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Health{
                healthAmount = authoring.healthAmount,
            });
        }
    }
}


public struct Health : IComponentData
{
    public float healthAmount;
    
}