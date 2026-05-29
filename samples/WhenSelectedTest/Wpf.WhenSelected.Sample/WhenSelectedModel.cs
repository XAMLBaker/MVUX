using Luke.Mvux;

namespace Wpf.WhenSelected.Sample;

public partial record WhenSelectedModel()
{
    public IState<string> SelectedCity => State<string>.Empty(this);

    public IListFeed<string> Cities
        => ListFeed
                .Value(["Seoul", "Busan", "Incheon", "Daegu", "Jeju"])
                .WhenSelected(SelectedCity);

    public ValueTask SelectBusan(CancellationToken ct)
        => SelectedCity.SetAsync("Busan", ct);

    public ValueTask ClearSelection(CancellationToken ct)
        => SelectedCity.SetNoneAsync(ct);
}
