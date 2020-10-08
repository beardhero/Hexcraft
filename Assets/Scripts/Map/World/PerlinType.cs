using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibNoise.Unity;
using LibNoise.Unity.Generator;

public static class PerlinType{
    public static string globalSeed = "seedTestSpeedTest";
    public static Perlin DefaultSurface(string seed = "seed"){
        Perlin output = new Perlin();
        output.Frequency = 0.0001f;
        output.Lacunarity = 1.4f;   // How quickly the frequency increases for each octave
        output.OctaveCount = 6;
        output.Persistence = .9f; //.8  // How quickly the amplitude diminishes for each octave
        output.Seed = BitConverter.ToInt32(SeedHandler.StringToBytes(seed), 0);
        output.Quality = QualityMode.High;      // @Todo speed test other quality modes
        return output;
    }

    public static Perlin DefaultPlateType(string seed = "seed"){
        Perlin output = new Perlin();
        output.Frequency = 0.0001f;
        output.Lacunarity = 1.6f;
        output.OctaveCount = 6;
        output.Persistence = .8f;
        output.Seed = BitConverter.ToInt32(SeedHandler.StringToBytes(seed), 0);
        output.Quality = QualityMode.High;
        return output;
    }
}