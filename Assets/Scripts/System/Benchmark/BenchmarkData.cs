using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BoatAttack.Benchmark
{
    [CreateAssetMenu(fileName = "BenchmarkSettings", menuName = "Boat Attack/System/Benchmark Settings")]
    public class BenchmarkData : ScriptableObject
    {
        public FinishAction finishAction;
        public bool saveData;
        public List<BenchmarkSettings> benchmarks = new List<BenchmarkSettings>();
    }
}
