using Avalonia.Controls;
using commonItems;
using Fronter.Models.Configuration.Options;
using Fronter.ViewModels;
using System.Reactive;
using Xunit;

namespace Fronter.Tests.ViewModels;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class MainWindowViewModelTests {
	public MainWindowViewModelTests() {
		LoggingConfigurator.ConfigureLogging();
	}

	[Fact]
	public void OptionsTabVisibleEqualsFalseWhenNoOptions() {
		var vm = new MainWindowViewModel(new DataGrid());
		vm.Options.Items.Clear();
		Assert.Empty(vm.Options.Items);
		Assert.False(vm.OptionsTabVisible);
	}

	[Fact]
	public void OptionsTabVisibleEqualsTrueWhenNoOptions() {
		var vm = new MainWindowViewModel(new DataGrid());
		vm.Options.Items.Add(new Option(new BufferedReader(), 420));
		Assert.NotEmpty(vm.Options.Items);
		Assert.True(vm.OptionsTabVisible);
	}


	[Fact]
	public void CancelCommand_enables_convert_button() {
		var vm = new MainWindowViewModel(new DataGrid());
		// simulate conversion in progress by disabling the convert button
		vm.ConvertButtonEnabled = false;

		// invoke cancel helper directly rather than going through reactive pipeline
		vm.CancelConversion();

		Assert.True(vm.ConvertButtonEnabled);
	}
}