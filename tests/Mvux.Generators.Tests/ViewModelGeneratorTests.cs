using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using Luke.Mvux.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mvux.Generators.Tests;

public class ViewModelGeneratorTests
{
    [Fact]
    public void Generates_ViewModel_With_PrimaryConstructor_Parameters()
    {
        var source = SampleModelSource();

        var result = RunGenerator(source);

        Assert.Contains("public partial class WeatherViewModel", result.GeneratedSource);
        Assert.Contains("public WeatherViewModel(", result.GeneratedSource);
        Assert.Contains("WeatherService", result.GeneratedSource);
        Assert.Contains("_model = new WeatherModel(WeatherService);", result.GeneratedSource);
    }

    [Fact]
    public void Does_Not_Generate_Model_Instance_Constructor()
    {
        var source = SampleModelSource();

        var result = RunGenerator(source);

        Assert.DoesNotContain("WeatherViewModel(WeatherModel model)", result.GeneratedSource);
    }

    [Fact]
    public void Generated_ViewModel_Is_Not_Sealed()
    {
        var source = SampleModelSource();

        var result = RunGenerator(source);

        Assert.DoesNotContain("public sealed class WeatherViewModel", result.GeneratedSource);
        Assert.Contains("public partial class WeatherViewModel", result.GeneratedSource);
    }

    [Fact]
    public void Generates_State_Property_With_SetAsync_And_SetNoneAsync()
    {
        var source = SampleModelSource();

        var result = RunGenerator(source);

        Assert.Contains("public string? City", result.GeneratedSource);
        Assert.Contains("if (value != null) _ = _model.City.SetAsync(value, _cts.Token);", result.GeneratedSource);
        Assert.Contains("else _ = _model.City.SetNoneAsync(_cts.Token);", result.GeneratedSource);
        Assert.Contains("private async Task SubscribeCityAsync(CancellationToken ct)", result.GeneratedSource);
    }

    [Fact]
    public void Generates_Feed_Passthrough_Property()
    {
        var source = SampleModelSource();

        var result = RunGenerator(source);

        Assert.Contains("CurrentWeather => _model.CurrentWeather;", result.GeneratedSource);
    }

    [Fact]
    public void Generates_List_Feeds_As_ObservableCollections()
    {
        var source = SampleModelSource();

        var result = RunGenerator(source);

        Assert.Contains("private readonly global::Luke.Mvux.ObservableListFeedView<string> _favorites;", result.GeneratedSource);
        Assert.Contains("private readonly global::Luke.Mvux.ObservableListFeedView<string> _favoritesWithSelection;", result.GeneratedSource);
        Assert.Contains("public global::System.Collections.ObjectModel.ObservableCollection<string> Favorites => _favorites;", result.GeneratedSource);
        Assert.Contains("public global::System.Collections.ObjectModel.ObservableCollection<string> FavoritesWithSelection => _favoritesWithSelection;", result.GeneratedSource);
    }

    [Fact]
    public void Generates_Commands_For_Task_And_ValueTask_Methods()
    {
        var source = SampleModelSource();

        var result = RunGenerator(source);

        Assert.Contains("public ICommand AddFavorite { get; }", result.GeneratedSource);
        Assert.Contains("public ICommand Refresh { get; }", result.GeneratedSource);
        Assert.Contains("AddFavorite = new AsyncCommand(() => _model.AddFavorite(_cts.Token).AsTask());", result.GeneratedSource);
        Assert.Contains("Refresh = new AsyncCommand(() => _model.Refresh());", result.GeneratedSource);
    }

    [Fact]
    public void Ignores_Methods_That_Are_Not_Commands()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Luke.Mvux;

            namespace Demo;

            public partial record InvalidCommandsModel()
            {
                public Task WithParameter(string city) => Task.CompletedTask;
                public Task<int> ReturnsGenericTask() => Task.FromResult(1);
                public static Task StaticMethod() => Task.CompletedTask;
                private Task Hidden() => Task.CompletedTask;
            }
            """;

        var result = RunGenerator(source);

        Assert.Equal(string.Empty, result.GeneratedSource);
    }

    [Fact]
    public void Ignores_NonModel_Records()
    {
        var source = """
            namespace Demo;

            public partial record Weather(string City);
            """;

        var result = RunGenerator(source);

        Assert.Equal(string.Empty, result.GeneratedSource);
    }

    [Fact]
    public void Generated_ViewModel_Compiles()
    {
        var source = SampleModelSource();

        var result = RunGenerator(source);

        var errors = result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    private static (string GeneratedSource, ImmutableArray<Diagnostic> OutputDiagnostics) RunGenerator(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = GetReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ViewModelGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees;
        var generatedSource = generatedTrees.Length > 0
            ? generatedTrees[0].ToString()
            : string.Empty;

        var compileErrors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();
        var combinedDiagnostics = outputDiagnostics.AddRange(compileErrors);

        return (generatedSource, combinedDiagnostics);
    }

    private static MetadataReference[] GetReferences()
    {
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)
            ?.Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList() ?? [];

        tpa.Add(MetadataReference.CreateFromFile(typeof(Luke.Mvux.IFeed<>).Assembly.Location));

        return tpa.ToArray();
    }

    private static string SampleModelSource()
        => """
           using System.Collections.Generic;
           using System.Threading;
           using System.Threading.Tasks;
           using Luke.Mvux;

           namespace Demo;

           public interface IWeatherService
           {
               Task<WeatherInfo> GetWeatherAsync(string city, CancellationToken ct);
           }

           public record WeatherInfo(string City, double Temperature, string Condition);

           public partial record WeatherModel(IWeatherService WeatherService)
           {
               public IState<string> City => State.Value(this, () => "Seoul");

               public IFeed<WeatherInfo> CurrentWeather =>
                   City.SelectAsync((city, ct) => WeatherService.GetWeatherAsync(city, ct));

               public IListState<string> Favorites => ListState.Value(this, () => new List<string>());
               public IState<string> SelectedFavorite => State<string>.Empty(this);
               public IListFeed<string> FavoritesWithSelection => Favorites.Selection(SelectedFavorite);

               public ValueTask AddFavorite(CancellationToken ct) => ValueTask.CompletedTask;
               public Task Refresh() => Task.CompletedTask;
           }
           """;
}
