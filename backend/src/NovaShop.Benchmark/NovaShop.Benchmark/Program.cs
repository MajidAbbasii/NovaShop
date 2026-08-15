using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace NovaShop.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class ProductBenchmarks
{
    private readonly HttpClient _client = new();

    [Benchmark]
    public async Task GetProducts_10()
    {
        var response = await _client.GetAsync("http://localhost:5000/api/products?pageSize=10");
        await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task GetProducts_100()
    {
        var response = await _client.GetAsync("http://localhost:5000/api/products?pageSize=100");
        await response.Content.ReadAsStringAsync();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ProductBenchmarks>();
    }
}
