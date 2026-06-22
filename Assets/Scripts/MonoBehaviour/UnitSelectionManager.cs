using System;
using Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MonoBehaviour ↔ ECS 桥接：在主线程读取输入，向所有带 UnitMover 的 Entity 写入目标位置。
/// DOTS 中 System 不宜直接读 Input，常见做法是由 MonoBehaviour 写 Component，System 读 Component。
/// </summary>
public class UnitSelectionManager : MonoSingleton<UnitSelectionManager>
{

    public UnityAction<Vector2> OnSelectionStart;
    public UnityAction<Vector2> OnSelectionEnd;

    public Vector2 selectStartPosition;


    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectStartPosition = Input.mousePosition;
            OnSelectionStart?.Invoke(selectStartPosition);
        }


        if (Input.GetMouseButtonUp(0))
        {
            Vector2 selectionEndPosition = Input.mousePosition;

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, Unit>().WithPresent<Selected>().Build(entityManager);

            NativeArray<Entity> entitieyArray = entityQuery.ToEntityArray(Allocator.Temp);
            NativeArray<Selected> selectedArray = entityQuery.ToComponentDataArray<Selected>(Allocator.Temp);
            //取消选中所有单位
            for (int i = 0; i < entitieyArray.Length; i++)
            {
                entityManager.SetComponentEnabled<Selected>(entitieyArray[i], false);
                Selected selected = selectedArray[i];
                selected.onDeselected = true;
                entityManager.SetComponentData(entitieyArray[i], selected);
            }


            //选中单位
            Rect selectionAreaRect = GetSlectionAreaRect();
            float selectionAreaSize = selectionAreaRect.width * selectionAreaRect.height;
            float multipleSelectionSizeMin = 40f;
            bool isMultipleSelection = selectionAreaSize > multipleSelectionSizeMin;
            // Debug.Log("selectionAreaSize: " + selectionAreaSize + " isMultipleSelection: " + isMultipleSelection);
            if (isMultipleSelection)
            {
                //多选
                entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, Unit>().WithPresent<Selected>().Build(entityManager);

                entitieyArray = entityQuery.ToEntityArray(Allocator.Temp);

                NativeArray<LocalTransform> localTransformArray = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
                for (int i = 0; i < entitieyArray.Length; i++)
                {
                    LocalTransform unitLocalTransform = localTransformArray[i];
                    Vector2 unitScreenPosition = Camera.main.WorldToScreenPoint(unitLocalTransform.Position);
                    if (selectionAreaRect.Contains(unitScreenPosition))
                    {
                        entityManager.SetComponentEnabled<Selected>(entitieyArray[i], true);
                        Selected selected = entityManager.GetComponentData<Selected>(entitieyArray[i]);
                        selected.onSelected = true;
                        entityManager.SetComponentData(entitieyArray[i], selected);
                    }
                }



            }
            else
            {
                //单选,用物理射线检测
                //获取物理世界单例
                entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
                //获取物理世界单例组件
                PhysicsWorldSingleton physicsWorldSingleton = entityQuery.GetSingleton<PhysicsWorldSingleton>();
                //获取物理世界碰撞世界
                CollisionWorld collisionWorld = physicsWorldSingleton.PhysicsWorld.CollisionWorld;
                UnityEngine.Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

                //声明层级
                int unitsLayer = 6;
                //声明射线输入，并设置数据
                RaycastInput raycastInput = new RaycastInput
                {
                    Start = cameraRay.GetPoint(0f),
                    End = cameraRay.GetPoint(9999f),
                    //碰撞过滤器
                    Filter = new CollisionFilter
                    {
                        //属于所有层级
                        BelongsTo = ~0u,
                        //碰撞层级
                        CollidesWith = 1u << unitsLayer,
                        //组索引，0表示默认组，1表示第1组，2表示第2组，以此类推
                        GroupIndex = 0,
                    },
                };
                if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit raycastHit))
                {
                    if (entityManager.HasComponent<Unit>(raycastHit.Entity))
                    {
                        entityManager.SetComponentEnabled<Selected>(raycastHit.Entity, true);
                        Selected selected = entityManager.GetComponentData<Selected>(raycastHit.Entity);
                        selected.onSelected = true;
                        entityManager.SetComponentData(raycastHit.Entity, selected);
                    }
                }


            }


            OnSelectionEnd?.Invoke(selectionEndPosition);
        }



        //右键选点
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePosition = MouseWorldPositon.Instance.GetPosition();

            // DefaultGameObjectInjectionWorld：默认 ECS World，SubScene 烘焙出的实体在此运行。
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // EntityQueryBuilder：声明查询条件（此处为拥有 UnitMover 的所有实体）。
            // Allocator.Temp 分配的 Native 容器须在本帧内 Dispose，此处 using 隐式释放。
            EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitMover, Selected>().Build(entityManager);

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            NativeArray<UnitMover> unitMovers = query.ToComponentDataArray<UnitMover>(Allocator.Temp);
            NativeArray<float3> movePositionArray = GenerateMovePositionArray(mousePosition, entities.Length);

            for (int i = 0; i < entities.Length; i++)
            {
                UnitMover unitMover = unitMovers[i];
                //设置目标位置
                unitMover.targetPosition = movePositionArray[i];
                unitMovers[i] = unitMover;
            }

            // 将修改后的组件数组批量写回 Query 匹配的 Entity（等价于逐 Entity SetComponentData）。
            query.CopyFromComponentDataArray(unitMovers);
        }
    }

    public Rect GetSlectionAreaRect()
    {
        Vector2 selectionEndMousePosition = Input.mousePosition;

        Vector2 lowerLeft = new Vector2(
            Mathf.Min(selectStartPosition.x, selectionEndMousePosition.x),
            Mathf.Min(selectStartPosition.y, selectionEndMousePosition.y)
        );
        Vector2 upperRight = new Vector2(
            Mathf.Max(selectStartPosition.x, selectionEndMousePosition.x),
            Mathf.Max(selectStartPosition.y, selectionEndMousePosition.y)
        );
        return new Rect(
            lowerLeft.x,
            lowerLeft.y,
            upperRight.x - lowerLeft.x,
            upperRight.y - lowerLeft.y
        );
    }

    private NativeArray<float3> GenerateMovePositionArray(float3 targetPosition, int positionCount)
    {
        NativeArray<float3> positionArray = new NativeArray<float3>(positionCount, Allocator.Temp);
        //如果位置数量小于等于0，则返回空数组
        if (positionCount <= 0)
        {
            return positionArray;
        }

        //第一个位置就是目标位置
        positionArray[0] = targetPosition;

        if (positionCount == 1)
        {
            return positionArray;
        }

        //计算每个环的半径
        float ringSize = 2.2f;
        int ring = 0;
        int positionIndex = 1;
        while (positionIndex < positionCount)
        {
            int ringPositionCount = 3 + ring * 2;

            for (int i = 0; i < ringPositionCount; i++)
            {
                float angle = i * 2 * math.PI / ringPositionCount;
                float3 ringVector = math.rotate(quaternion.RotateY(angle), new float3(ringSize * (ring + 1), 0, 1));
                float3 ringPosition = targetPosition + ringVector;
                positionArray[positionIndex] = ringPosition;
                positionIndex++;
                if (positionIndex >= positionCount)
                {
                    break;
                }
            }
            ring++;
        }

        return positionArray;
    }
}
