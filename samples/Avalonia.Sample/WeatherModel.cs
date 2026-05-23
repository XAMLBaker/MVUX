using Lw.Mvux;

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
    public IState<string> City { get; } = State.Value("Seoul");

    public IFeed<WeatherInfo> CurrentWeather =>
        City.SelectAsync((city, ct) => WeatherService.GetWeatherAsync(city, ct));

    public IListState<string> Favorites { get; } = ListState.Value<string>(new List<string> { "Seoul" });

    public IState<string> SelectedFavorite { get; } = State.Empty<string>();

    // Favorites에 Selection 연결 — 리스트 갱신 시 없어진 항목은 선택 자동 초기화
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
}
