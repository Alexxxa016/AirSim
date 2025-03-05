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
        cloud = FindObjectOfType<cloudScript>();
        var mainModule = ps.main;
        mainModule.maxParticles = numberOfParticles;

        particles = new Particle[numberOfParticles];
        for (int i = 0; i < numberOfParticles; i++)
        {
            particles[i] = new Particle();
            // Example initialization:
            particles[i].position = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f));
            particles[i].startLifetime = 20f;
            particles[i].remainingLifetime = particles[i].startLifetime;
            particles[i].velocity = Vector3.zero;
            particles[i].startSize3D = 0.1f * Vector3.one;
        }

        ps.SetParticles(particles, numberOfParticles);

        particleArray = new NativeArray<Particle>(particles.Length, Allocator.Persistent);
        positionalArray = new NativeArray<Vector3>(numberOfParticles, Allocator.Persistent);
    }

    void Update()
    {
        int numParticlesAlive = ps.GetParticles(particles);
        print(numParticlesAlive);
        // Copy particle data to NativeArray
        particleArray.CopyFrom(particles);
        positionalArray.CopyFrom(cloud.getpositions());
        // Create and schedule the job
        MyParticleJob job = new MyParticleJob
        {
            particleData = particleArray,
            GOPosData = positionalArray,
            time = Time.time
        };

        JobHandle handle = job.Schedule(numParticlesAlive, 64);
        handle.Complete();

        // Copy modified data back to the particle system
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
            // Example: update particle position based on global object positions.
            p.position = GOPosData[index];
            p.velocity = Vector3.zero;
            particleData[index] = p;
        }
    }

    void OnDisable()
    {
        particleArray.Dispose();
        positionalArray.Dispose();
    }
}
