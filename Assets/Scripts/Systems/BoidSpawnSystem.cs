using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[BurstCompile]
partial struct BoidSpawnSystem : ISystem
{

    public const int wantedBoidCount = 5000;
    private int boidCount;
    private float timepassed;
    private const float timer = 1f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        timepassed = 0f;
        boidCount = 0;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        timepassed += state.WorldUnmanaged.Time.DeltaTime;
        if (timepassed < timer)
            return;


        if (boidCount >= wantedBoidCount)
            return;

        // Creating an EntityCommandBuffer to defer the structural changes required by instantiation.
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        for (int i = 0; i < 100; i++)
        {
            float3 randomPosition = GetRandomPosition();

            float3 randomRotation = GetRandomRotation();

            LocalToWorldTransform localToWorld = new();
            localToWorld.Value.Position = randomPosition;
            localToWorld.Value.Rotation = quaternion.EulerXYZ(randomRotation);
            localToWorld.Value.Scale = 20f;

            var spawnBoidJob = new SpawnBoid
            {
                ECB = ecb,
                spawnPosition = localToWorld,
            };

            spawnBoidJob.Schedule();
            boidCount++;
            if (boidCount >= wantedBoidCount)
                return;
        }
    }

    [BurstCompile]
    partial struct SpawnBoid : IJobEntity
    {
        public EntityCommandBuffer ECB;
        public LocalToWorldTransform spawnPosition;

        void Execute(in BoidSpawnerAspect boidSpawnerAspect)
        {
            Entity instance = ECB.Instantiate(boidSpawnerAspect.boidPrefab);
            ECB.AddComponent(instance, new Boid
            {
            });
            ECB.SetComponent(instance, new LocalToWorldTransform
            {
                Value = spawnPosition.Value
            });
        }
    }

    public float3 GetRandomPosition()
    {
        float3 randomPosition;
        randomPosition.x = UnityEngine.Random.Range(-1f, 1f);
        randomPosition.y = UnityEngine.Random.Range(0f, 2f);
        randomPosition.z = UnityEngine.Random.Range(-1f, 1f);
        return randomPosition;
    }

    public float3 GetRandomRotation()
    {
        float3 randomRotation;
        randomRotation.x = UnityEngine.Random.Range(0f, 360f);
        randomRotation.y = UnityEngine.Random.Range(0f, 360f);
        randomRotation.z = UnityEngine.Random.Range(0f, 360f);
        return randomRotation;
    }


}
