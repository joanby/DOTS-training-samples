﻿using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class CollectStateSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem buffer;

    protected override void OnCreate()
    {
        buffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var collectDistance = GetSingleton<CollectDistance>().Value;
        var collectVelocity = GetSingleton<CollectVelocity>().Value;

        var translationContainer = GetComponentDataFromEntity<Translation>(true);
        var commonBuffer = buffer.CreateCommandBuffer().ToConcurrent();

        var handle = Entities.WithReadOnly(translationContainer).ForEach(
            (Entity entity, ref TargetVelocity targetVelocity, ref State state, in TargetEntity targetEntity) =>
            {
                if (state.Value == State.StateType.Collecting)
                {
                    targetVelocity.Value = collectVelocity;

                    var translation = translationContainer[entity];
                    var targetTranslation = translationContainer[targetEntity.Value];
                    var distance = math.distance(translation.Value, targetTranslation.Value);

                    if (distance <= collectDistance)
                    {
                        commonBuffer.AddComponent(0, targetEntity.Value, new Parent
                        {
                            Value = entity
                        });
                        
                        commonBuffer.AddComponent<LocalToParent>(0, targetEntity.Value);

                        state.Value = State.StateType.Dropping;
                    }
                }
            }).Schedule(inputDependencies);
        
        buffer.AddJobHandleForProducer(handle);
        return handle;
    }
}