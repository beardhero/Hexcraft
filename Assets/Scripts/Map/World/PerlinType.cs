using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibNoise.Unity;
using LibNoise.Unity.Generator;

public static class PerlinType{
    public static string globalSeed = "seedTestSpeedTest";
    public static Perlin DefaultSurface(string seed = ""){  // "smoothish"
        Perlin output = new Perlin();
        output.Frequency = 0.001;
        output.Lacunarity = .0001;   // How quickly the frequency increases for each octave
        output.OctaveCount = 5;
        output.Persistence = 1.5;  // How quickly the amplitude diminishes for each octave
        output.Seed = BitConverter.ToInt32(SeedHandler.StringToBytes(seed==""?globalSeed:seed), 0);
        output.Quality = QualityMode.High;      // @Todo speed test other quality modes
        return output;
    }

    public static Perlin RoughSurface(string seed = ""){
        Perlin output = new Perlin();
        output.Frequency = 0.00003;
        output.Lacunarity = 2.2;   // How quickly the frequency increases for each octave
        output.OctaveCount = 6;
        output.Persistence = 1.05;  // How quickly the amplitude diminishes for each octave
        output.Seed = BitConverter.ToInt32(SeedHandler.StringToBytes(seed==""?globalSeed:seed), 0);
        output.Quality = QualityMode.High;      // @Todo speed test other quality modes
        return output;
    }

    // usused
    public static Perlin DefaultImpassable(string seed = ""){
        Perlin output = new Perlin();
        output.Frequency = 0.0002;
        output.Lacunarity = 1.6;
        output.OctaveCount = 6;
        output.Persistence = .8;
        output.Seed = BitConverter.ToInt32(SeedHandler.StringToBytes(seed==""?globalSeed:seed), 0);
        output.Quality = QualityMode.High;
        return output;
    }

    // unused
    public static Perlin DefaultBiome(string seed = ""){
        Perlin output = new Perlin();
        output.Frequency = .0002;
        output.Lacunarity = 1.4;
        output.OctaveCount = 6;
        output.Persistence = 1.05;
        output.Seed = BitConverter.ToInt32(SeedHandler.StringToBytes(seed==""?globalSeed:seed), 0);
        output.Quality = QualityMode.High;
        return output;
    }
}