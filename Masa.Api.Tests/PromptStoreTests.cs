using Masa.Api.Packs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Masa.Api.Tests;

public sealed class PromptStoreTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("masa-prompts-").FullName;

    private sealed class FakeEnv(string root) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Masa.Api";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private PromptStore CreateStore()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Llm:PromptPath"] = "." })
            .Build();
        return new PromptStore(config, new FakeEnv(_dir));
    }

    private void WriteFiles(string version)
    {
        File.WriteAllText(Path.Combine(_dir, "studypack.md"), $"version: {version}\nTalimat.");
        File.WriteAllText(Path.Combine(_dir, "schema.json"), """{ "type": "object" }""");
        File.WriteAllText(Path.Combine(_dir, "examples.json"), """
            [
              { "sendWithRequest": true, "id": 1 },
              { "sendWithRequest": false, "id": 2 },
              { "sendWithRequest": true, "id": 3 },
              { "sendWithRequest": true, "id": 4 }
            ]
            """);
    }

    [Fact]
    public async Task Version_follows_file_without_code_change()
    {
        var store = CreateStore();
        WriteFiles("v1");
        Assert.Equal("v1", (await store.LoadAsync()).Version);
        WriteFiles("v2");
        Assert.Equal("v2", (await store.LoadAsync()).Version);
    }

    [Fact]
    public async Task At_most_two_flagged_examples_are_sent()
    {
        WriteFiles("v3");
        var bundle = await CreateStore().LoadAsync();
        Assert.Equal([1, 3], bundle.Examples.Select(e => e["id"]!.GetValue<int>()));
        Assert.All(bundle.Examples, e => Assert.Null(e["sendWithRequest"]));
        Assert.Equal("Talimat.", bundle.Instruction);
    }

    [Theory]
    [InlineData("v3")]
    [InlineData("  version:  v3 ")]
    public void Version_line_must_start_with_prefix(string line)
    {
        if (line == "v3") Assert.Throws<InvalidOperationException>(() => PromptStore.ParseVersion(line));
        else Assert.Equal("v3", PromptStore.ParseVersion(line));
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
