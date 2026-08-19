using BenchmarkDotNet.Attributes;

namespace ProMvvm.Benchmarks;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public abstract class BenchmarkConfig;
