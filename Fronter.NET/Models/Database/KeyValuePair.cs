using System;
using System.Collections.Generic;

namespace Fronter.Models.Database;

public partial class KeyValuePair
{
    public string Name { get; set; } = null!;

    public string? Value { get; set; }
}
