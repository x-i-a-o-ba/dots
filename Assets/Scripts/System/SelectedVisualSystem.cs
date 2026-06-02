using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

partial struct SelectedVisualSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (RefRO<Selected> selected in SystemAPI.Query<RefRO<Selected>>().WithDisabled<Selected>())
        {
            RefRW<LocalTransform> visualTransform = SystemAPI.GetComponentRW<LocalTransform>(
                selected.ValueRO.visualEntity
            );
            visualTransform.ValueRW.Scale = 0f;
        }

        foreach (RefRO<Selected> selected in SystemAPI.Query<RefRO<Selected>>())
        {
            RefRW<LocalTransform> visualTransform = SystemAPI.GetComponentRW<LocalTransform>(
                selected.ValueRO.visualEntity
            );
            visualTransform.ValueRW.Scale = selected.ValueRO.showScale;
        }
    }
}
