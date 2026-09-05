using Avalonia;
using Fronter.ValueConverters;
using System.Globalization;
using Xunit;

namespace Fronter.Tests.ValueConverters;

public class ValueConverterTests {
	[Fact]
	public void BooleanNegationConverter_InvertsBooleanValues() {
		var converter = BooleanNegationConverter.Instance;

		Assert.Equal(false, converter.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture));
		Assert.Equal(true, converter.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture));
		Assert.Same(AvaloniaProperty.UnsetValue, converter.Convert("not a bool", typeof(bool), null, CultureInfo.InvariantCulture));
		Assert.Equal(false, converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture));
		Assert.Equal(true, converter.ConvertBack(false, typeof(bool), null, CultureInfo.InvariantCulture));
		Assert.Same(AvaloniaProperty.UnsetValue, converter.ConvertBack("not a bool", typeof(bool), null, CultureInfo.InvariantCulture));
	}

	[Fact]
	public void EnumToBooleanConverter_UsesParameterForEqualityChecks() {
		var converter = EnumToBooleanConverter.Instance;

		Assert.Equal(true, converter.Convert(TestEnum.Second, typeof(bool), TestEnum.Second, CultureInfo.InvariantCulture));
		Assert.Equal(false, converter.Convert(TestEnum.First, typeof(bool), TestEnum.Second, CultureInfo.InvariantCulture));
		Assert.Same(AvaloniaProperty.UnsetValue, converter.Convert(null, typeof(bool), TestEnum.Second, CultureInfo.InvariantCulture));
		Assert.Equal(TestEnum.Second, converter.ConvertBack(true, typeof(TestEnum), TestEnum.Second, CultureInfo.InvariantCulture));
		Assert.Same(AvaloniaProperty.UnsetValue, converter.ConvertBack(false, typeof(TestEnum), TestEnum.Second, CultureInfo.InvariantCulture));
	}

	private enum TestEnum {
		First,
		Second,
	}
}
