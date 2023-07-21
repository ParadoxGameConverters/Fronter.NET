using System;
using System.Collections.Generic;

namespace Fronter.Models.Database;

public partial class Dlc
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string DirPath { get; set; } = null!;
}
