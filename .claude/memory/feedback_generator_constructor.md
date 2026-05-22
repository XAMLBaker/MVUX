---
name: feedback-generator-constructor
description: "ViewModel constructor must mirror Model's primary constructor params (MVUX pattern), not take Model instance"
metadata:
  type: feedback
---

Generator must NOT generate `WeatherViewModel(WeatherModel model)`. It must mirror the record's primary constructor: `WeatherViewModel(IWeatherService weatherService)` with `_model = new WeatherModel(weatherService)` internally.

**Why:** Uno MVUX pattern — the ViewModel IS the generated output of the Model. Usage should be `DataContext = new WeatherViewModel(new FakeWeatherService())`.

**How to apply:** Use `symbol.InstanceConstructors.FirstOrDefault(c => !c.IsImplicitlyDeclared && !(c.Parameters.Length == 1 && same type))` to get the record's primary constructor params. Generated class must NOT be `sealed` so platform adapters can inherit it.
