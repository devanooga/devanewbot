namespace devanewbot.Api.v0.Models.Admin;

using System;
using System.Collections.Generic;

public class FinanceImportModel
{
    public string? Csv { get; set; }
    public List<string> ApplyKeys { get; set; } = [];
    public List<Guid> HideIds { get; set; } = [];
}

public class FinanceAccountModel
{
    public string? Name { get; set; }
    public DateOnly OpeningDate { get; set; }
    public decimal OpeningBalance { get; set; }
}

public class DonationNamedModel
{
    public bool NamedOnRequest { get; set; }
}

public class FinancePayeeModel
{
    public bool IsPublic { get; set; }
}
