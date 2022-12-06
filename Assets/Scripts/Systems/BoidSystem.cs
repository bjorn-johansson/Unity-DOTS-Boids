using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

partial class BoidSystem : SystemBase
{

    EntityQuery boidQuery;

    public const float alignmentBias = 1.5f;
    public const float cohesionBias = 1f;
    public const float separationBias = 0.75f;

    public const float centerRange = 40f;

    public const float alignmentRange = 5f;
    public const float cohesionRange = 6f;
    public const float separationRange = 0.5f; 

    public const float visionConeAngle = 70; //if an object is inside a boids visionCone, apply alignment cohesion and separation. otherwise ignore that boid.

    public const float speed = 1.5f;
    public const float maxVelocity = 6f;
    public const float minVelocity = 4f;
    public const float maxTurnAngle = 40f;

    protected override void OnCreate()
    {
        boidQuery = GetEntityQuery(typeof(LocalToWorldTransform), typeof(Boid));
    }

    protected override void OnUpdate()
    {
        var dt = SystemAPI.Time.DeltaTime;

        int boidCount = boidQuery.CalculateEntityCount();
        var nativeArray = CollectionHelper.CreateNativeArray<UniformScaleTransform, RewindableAllocator>(boidCount, ref World.UpdateAllocator); //https://docs.unity3d.com/Packages/com.unity.collections@2.1/api/Unity.Collections.RewindableAllocator.html

        //set native array
        Entities
            .WithAll<Boid>()
            .ForEach((int entityInQueryIndex, in LocalToWorldTransform localToWorld) =>
            {
                nativeArray[entityInQueryIndex] = localToWorld.Value;
            }).Schedule(); //doing this on the main thread to block the boids/write job from clashing with it and causing read/write race conditions. it is slower than i would like though.

        var boidTransforms = nativeArray.AsReadOnly();


        Entities
               .WithAll<Boid>()
               .ForEach((ref TransformAspect thisBoidTransform, ref URPMaterialPropertyBaseColor color) =>
               {
                   Vector3 velocity = Vector3.zero;
                   Vector3 position = thisBoidTransform.Position;

                   Vector3 visionConeForward = thisBoidTransform.Forward;

                   Vector3 cohesionAverage = Vector3.zero;
                   int cohesionCount = 0;

                   Vector3 separationVector = Vector3.zero;
                   int separationCount = 0;


                   Vector3 alignmentAverage = Vector3.zero;
                   int alignmentCount = 0;

                   foreach (UniformScaleTransform otherBoidTransform in boidTransforms)//inefficient, it would be much better to sort out which data is relevant before this job is executed using chunks, and avoid direct comparisons
                   {
                       Vector3 difference = (Vector3)otherBoidTransform.Position - position;
                       if (difference.magnitude == 0)
                           continue;


                       //cohesion
                       if (difference.magnitude <= cohesionRange)
                       {
                           if (math.abs(Vector3.Angle(visionConeForward, otherBoidTransform.Position)) <= visionConeAngle)
                           {
                               cohesionAverage += difference;
                               cohesionCount++;
                           }
                       }


                       //separation
                       if (difference.magnitude <= separationRange)
                       {
                           if (math.abs(Vector3.Angle(visionConeForward, otherBoidTransform.Position)) <= visionConeAngle)
                           {
                               separationVector += difference;
                               separationCount++;
                           }
                       }


                       //alignment
                       if (difference.magnitude <= alignmentRange)
                       {
                           if (math.abs(Vector3.Angle(visionConeForward, otherBoidTransform.Position)) <= visionConeAngle)
                           {
                               alignmentAverage += (Quaternion)otherBoidTransform.Rotation * Vector3.one; //add a normalized vector
                               alignmentCount++;
                           }
                       }
                   }


                   if (cohesionCount > 0)
                   {
                       cohesionAverage /= cohesionCount;
                       velocity += Vector3.Lerp(Vector3.zero, cohesionAverage, cohesionAverage.magnitude / cohesionRange) * cohesionBias;  //lerp goes from 0-1 * average cohesionrange
                   }


                   if (separationCount > 0)
                   {
                       separationVector /= separationCount;
                       Vector3 directionTowardsBoids = Vector3.Lerp(Vector3.zero, separationVector, (separationVector.magnitude / separationRange) * -1 + 1);
                       directionTowardsBoids.Normalize();
                       directionTowardsBoids.x = 1 / directionTowardsBoids.x - separationRange + 0.1f;
                       directionTowardsBoids.y = 1 / directionTowardsBoids.y - separationRange + 0.1f;
                       directionTowardsBoids.z = 1 / directionTowardsBoids.z - separationRange + 0.1f;
                       velocity -= directionTowardsBoids * separationBias;
                   }


                   if (alignmentCount > 0)
                   {
                       alignmentAverage.Normalize();
                       Vector3 thisDir = (Vector3)thisBoidTransform.Forward;
                       velocity += Vector3.Lerp(thisDir, alignmentAverage, Vector3.Angle(velocity, alignmentAverage) / 180) * alignmentBias;
                   }


                   //if too far away, turn towards center.

                   Vector3 distanceToOrigin = math.abs(position);
                   bool center = false;
                   
                   if (distanceToOrigin.x >= centerRange || distanceToOrigin.y >= centerRange || distanceToOrigin.z >= centerRange)
                   {
                       Vector3 dirToOrigin = position * -1f;
                       dirToOrigin.Normalize();

                       TransformAspect temp = thisBoidTransform;
                       temp.Rotation = Quaternion.LookRotation(dirToOrigin);
                       velocity = temp.Forward * velocity.magnitude;
                       center = true;
                   }

                   velocity = Vector3.ClampMagnitude(velocity, maxVelocity); //clamp to max
                   if (velocity == Vector3.zero)
                       velocity = (Vector3)thisBoidTransform.Forward * minVelocity;
                   else if (velocity.magnitude < minVelocity)
                       velocity = velocity.normalized * minVelocity; //floor to min
                   

                   thisBoidTransform.Rotation = Quaternion.RotateTowards(thisBoidTransform.Rotation, Quaternion.LookRotation(velocity, thisBoidTransform.Up), maxTurnAngle * dt);
                   thisBoidTransform.Position += speed * (float3)velocity.magnitude *  thisBoidTransform.Forward * dt;

                   Vector3 newcolor = (Vector3)thisBoidTransform.Forward;
                   newcolor.Normalize();
                   if (newcolor.x < 0.01f)
                       newcolor.x = 0.01f;
                   if (newcolor.y < 0.01f)
                       newcolor.y = 0.01f;
                   if (newcolor.z < 0.01f)
                       newcolor.z = 0.01f;

                   if (center)
                       color.Value = (Vector4)Color.red;
                   else
                       color.Value = new Vector4(newcolor.x, newcolor.y, 1f, 1f);

               }).ScheduleParallel();//done in parallel threads to speed it up. preferrably i'd avoid iterating over all other boids aswell.
                        
        nativeArray.Dispose();
    }

}
