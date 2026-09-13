using Fronter;
using Fronter.Models.Configuration.Options;
using commonItems;
using System.IO;
using System.Text;
using Xunit;

namespace Fronter.Tests.Models;

public class RadioSelectorTests {
    [Fact]
    public void SetSelectedId_InvalidFallingBackToDefault_LogsAndSelectsDefault() {
        LoggingConfigurator.ConfigureLogging(useConsole: false);

        // create an empty selector and add options manually; first option will act as default
        var selector = new RadioSelector(new BufferedReader(string.Empty));
        selector.RadioOptions.Add(new ToggleableOption(new BufferedReader("name = foo"), 1));
        selector.RadioOptions.Add(new ToggleableOption(new BufferedReader("name = bar"), 2));

        // first option should be returned when falling back
        // (parser may not pre-select anything).

        selector.SetSelectedId(999); // id not present
        Assert.Equal("foo", selector.GetSelectedValue());

        using var fs = new FileStream("log.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs, Encoding.Default);
        var log = sr.ReadToEnd();
        Assert.Contains("Falling back to default 'foo' (id 1)", log);
    }

    [Fact]
    public void SetSelectedValue_InvalidFallingBackToDefault_LogsAndSelectsDefault() {
        LoggingConfigurator.ConfigureLogging(useConsole: false);

        var selector = new RadioSelector(new BufferedReader(string.Empty));
        selector.RadioOptions.Add(new ToggleableOption(new BufferedReader("name = alpha"), 1));
        selector.RadioOptions.Add(new ToggleableOption(new BufferedReader("name = beta"), 2));

        // rely on first option ('alpha') as fallback value rather than expecting the parser to pre-select it

        selector.SetSelectedValue("not_here");
        Assert.Equal("alpha", selector.GetSelectedValue());

        using var fs = new FileStream("log.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs, Encoding.Default);
        var log = sr.ReadToEnd();
        Assert.Contains("Falling back to default 'alpha' (id 1)", log);
    }
}