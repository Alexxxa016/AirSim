using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class particleScript : MonoBehaviour
{
    ParticleSystem ps;
    cloudScript cloud;
    private Particle[] particles;
    private NativeArray<Particle> particleArray;
    private NativeArray<Vector3> positionalArray;
    private int numberOfParticles = 6 * 6 * 6;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        cloud = cloudScript.Instance;
        var mainModule = ps.main;
        mainModule.maxParticles = numberOfParticles;

        particles = new Particle[numberOfParticles];
        for (int i = 0; i < numberOfParticles; i++)
        {
            particles[i] = new Particle();
            particles[i].position = new Vector3(Random.Range(-10f, 10f),
                                                Random.Range(-10f, 10f),
                                                Random.Range(-10f, 10f));
            particles[i].startLifetime = 200f;
            particles[i].remainingLifetime = particles[i].startLifetime;
            particles[i].velocity = Vector3.zero;
            particles[i].startSize3D = 0.2f * Vector3.one;
        }

        ps.SetParticles(particles, numberOfParticles);

        particleArray = new NativeArray<Particle>(particles.Length, Allocator.Persistent);
        positionalArray = new NativeArray<Vector3>(numberOfParticles, Allocator.Persistent);
    }

    void Update()
    {
        int numParticlesAlive = ps.GetParticles(particles);
        particleArray.CopyFrom(particles);
        positionalArray.CopyFrom(cloud.allPos);

        MyParticleJob job = new MyParticleJob
        {
            particleData = particleArray,
            GOPosData = positionalArray,
            time = Time.time
        };

        JobHandle handle = job.Schedule(numParticlesAlive, 64);
        handle.Complete();

        particleArray.CopyTo(particles);
        ps.SetParticles(particles, numParticlesAlive);
    }

    public struct MyParticleJob : IJobParallelFor
    {
        public NativeArray<Particle> particleData;
        public NativeArray<Vector3> GOPosData;
        public float time;

        public void Execute(int index)
        {
            Particle p = particleData[index];
            p.position = GOPosData[index];
            p.velocity = Vector3.zero; // Reset velocity for visualization.
            particleData[index] = p;
        }
    }

    void OnDisable()
    {
        if (particleArray.IsCreated) particleArray.Dispose();
        if (positionalArray.IsCreated) positionalArray.Dispose();
    }
}
