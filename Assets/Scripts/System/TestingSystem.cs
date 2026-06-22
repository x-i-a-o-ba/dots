
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
partial struct TestingSystem : ISystem
{
  
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int unitCount = 0;

        foreach (RefRO<Friendly> unit in SystemAPI.Query<RefRO<Friendly>>()){
            unitCount++;
        }

        Debug.Log("Friendly count: " + unitCount);
    }

  
}
