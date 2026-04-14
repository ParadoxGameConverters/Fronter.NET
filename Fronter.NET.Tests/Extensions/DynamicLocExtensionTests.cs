using Avalonia;
using Avalonia.Data;
using Fronter.Extensions;
using Fronter.ValueConverters;
using System.Globalization;
using Xunit;

namespace Fronter.Tests.Extensions;

[Collection("Sequential")]
public class DynamicLocExtensionTests {
	[Fact]
	public void ProvideValue_ReturnsMultiBinding() {
		var ext = new DynamicLocExtension("SomePath");
		var result = ext.ProvideValue();
		Assert.NotNull(result);
		Assert.IsType<MultiBinding>(result);
	}

	[Fact]
	public void ProvideValue_ReturnedBindingHasTwoBindings() {
		var ext = new DynamicLocExtension("SomePath");
		var result = ext.ProvideValue();
		Assert.Equal(2, result.Bindings.Count);
	}

	[Fact]
	public void ProvideValue_FirstBindingUsesProvidedPath() {
		var ext = new DynamicLocExtension("TestPath");
		var result = ext.ProvideValue();
		var firstBinding = Assert.IsType<Binding>(result.Bindings[0]);
		Assert.Equal("TestPath", firstBinding.Path);
	}

	[Fact]
	public void ProvideValue_SecondBindingTracksCurrentLanguage() {
		var ext = new DynamicLocExtension("SomePath");
		var result = ext.ProvideValue();
		var secondBinding = Assert.IsType<Binding>(result.Bindings[1]);
		Assert.Equal("CurrentLanguage", secondBinding.Path);
		Assert.Same(TranslationSource.Instance, secondBinding.Source);
	}

	[Fact]
	public void ProvideValue_ConverterIsLocKeyToValueConverter() {
		var ext = new DynamicLocExtension("SomePath");
		var result = ext.ProvideValue();
		Assert.IsType<LocKeyToValueConverter>(result.Converter);
	}

	[Fact]
	public void Converter_ReturnsUnsetValueForNonStringInput() {
		var converter = new LocKeyToValueConverter();
		var result = converter.Convert([null], typeof(string), null, CultureInfo.InvariantCulture);
		Assert.Equal(AvaloniaProperty.UnsetValue, result);
	}

	[Fact]
	public void Converter_ReturnsEmptyStringForUnknownKey() {
		var converter = new LocKeyToValueConverter();
		var result = converter.Convert(["UNKNOWN_LOC_KEY_FOR_TESTS_XYZ"], typeof(string), null, CultureInfo.InvariantCulture);
		Assert.Equal(string.Empty, result);
	}
}
