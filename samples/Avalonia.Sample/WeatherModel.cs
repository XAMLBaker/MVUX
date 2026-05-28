using Luke.Mvux;

namespace Avalonia.Sample;

public record WeatherInfo(string City, double Temperature, string Condition);

public interface IWeatherService
{
    Task<WeatherInfo> GetWeatherAsync(string city, CancellationToken ct);
}

public class FakeWeatherService : IWeatherService
{
    public async Task<WeatherInfo> GetWeatherAsync(string city, CancellationToken ct)
    {
        await Task.Delay(1500, ct);
        return new WeatherInfo(city, 23.5, "Sunny");
    }
}

public partial record WeatherModel(IWeatherService WeatherService)
{
    public IState<string> City => State.Value(this, () => "Seoul");

    public IFeed<WeatherInfo> CurrentWeather =>
        City.SelectAsync((city, ct) => WeatherService.GetWeatherAsync(city, ct));

    public IListState<string> Favorites => ListState.Value(this, () => new List<string> { "Seoul" });

    public IState<string> SelectedFavorite => State<string>.Empty(this);

    public IListFeed<string> FavoritesWithSelection => Favorites.Selection(SelectedFavorite);

    public async ValueTask AddFavorite(CancellationToken ct)
    {
        var city = await City;
        if (city != null) await Favorites.AddAsync(city, ct);
    }

    public async ValueTask GoToSelected(CancellationToken ct)
    {
        var selected = await SelectedFavorite;
        if (selected != null) await City.SetAsync(selected, ct);
    }

    public async ValueTask AddFavoriteFromParameter(string city, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(city))
            await Favorites.AddAsync(city, ct);
    }

    public ValueTask UseCityFeedValue(string city)
    {
        _ = city;
        return ValueTask.CompletedTask;
    }
}
