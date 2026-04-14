using Avalonia.Controls;
using commonItems;
using Fronter.Models.Configuration.Options;
using Fronter.ViewModels;
using System.Linq;
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
		var vm = new MainWindowViewModel(new DataGrid()) {
			// simulate conversion in progress by disabling the convert button
			ConvertButtonEnabled = false
		};

		// invoke cancel helper directly rather than going through reactive pipeline
		vm.CancelConversion();

		Assert.True(vm.ConvertButtonEnabled);
	}

	[Fact]
	public void ThemeMenuItems_HasThreeEntries() {
		var vm = new MainWindowViewModel(new DataGrid());
		Assert.Equal(3, vm.ThemeMenuItems.Count());
	}

	[Fact]
	public void ThemeMenuItems_FirstEntryIsFollowSystem() {
		var vm = new MainWindowViewModel(new DataGrid());
		var first = vm.ThemeMenuItems.First();
		Assert.Equal("Default", first.CommandParameter);
	}

	[Fact]
	public void ThemeMenuItems_ContainsLightAndDark() {
		var vm = new MainWindowViewModel(new DataGrid());
		var ids = vm.ThemeMenuItems.Select(i => (string?)i.CommandParameter).ToList();
		Assert.Contains("Light", ids);
		Assert.Contains("Dark", ids);
	}
}