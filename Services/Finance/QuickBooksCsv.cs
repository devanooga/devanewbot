namespace devanewbot.Services.Finance;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public record QuickBooksRow(string Account, string Type, DateOnly Date, string Number, string Name, string Memo, string Split, decimal Amount);

// Reads a QuickBooks Desktop report exported to CSV. "Transaction Detail by Account" names each account in a
// section heading row; "Transaction List by Date" carries an Account column instead. Both are handled.
public static class QuickBooksCsv
{
    private static readonly string[] DateFormats = ["MM/dd/yyyy", "M/d/yyyy", "M/d/yy", "yyyy-MM-dd"];

    public static List<QuickBooksRow> Parse(string csv)
    {
        var lines = Records(csv).ToList();
        var headerIndex = lines.FindIndex(cells =>
            cells.Any(cell => Is(cell, "Type")) && cells.Any(cell => Is(cell, "Date")) && cells.Any(cell => Is(cell, "Amount")));
        if (headerIndex < 0)
        {
            throw new FormatException("That doesn't look like a QuickBooks transaction report: no Type, Date and Amount header row.");
        }

        var header = lines[headerIndex];
        int Column(string name) => Array.FindIndex(header, cell => Is(cell, name));
        var type = Column("Type");
        var date = Column("Date");
        var number = Column("Num");
        var name = Column("Name");
        var memo = Column("Memo");
        var account = Column("Account");
        var split = Column("Split");
        var amount = Column("Amount");

        var rows = new List<QuickBooksRow>();
        var section = "";
        foreach (var cells in lines.Skip(headerIndex + 1))
        {
            string Cell(int index) => index >= 0 && index < cells.Length ? cells[index].Trim() : "";

            if (Cell(type).Length == 0 || Cell(date).Length == 0)
            {
                var heading = cells.Take(Math.Max(type, 1)).Select(cell => cell.Trim()).FirstOrDefault(cell => cell.Length > 0);
                if (heading is not null && !heading.StartsWith("Total", StringComparison.OrdinalIgnoreCase))
                {
                    section = heading;
                }
                continue;
            }

            if (!DateOnly.TryParseExact(Cell(date), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                throw new FormatException($"Could not read the date \"{Cell(date)}\".");
            }

            rows.Add(new QuickBooksRow(
                account >= 0 ? Cell(account) : section,
                Cell(type),
                parsedDate,
                Cell(number),
                Cell(name),
                Cell(memo),
                Cell(split),
                Money(Cell(amount))));
        }

        return rows;
    }

    private static decimal Money(string value)
    {
        var negative = value.StartsWith('(') && value.EndsWith(')');
        var cleaned = value.Trim('(', ')').Replace("$", "").Replace(",", "");
        if (!decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new FormatException($"Could not read the amount \"{value}\".");
        }

        return negative ? -parsed : parsed;
    }

    private static bool Is(string cell, string name) => cell.Trim().Equals(name, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string[]> Records(string csv)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                {
                    i++;
                }
                cells.Add(cell.ToString());
                cell.Clear();
                yield return cells.ToArray();
                cells.Clear();
            }
            else
            {
                cell.Append(c);
            }
        }

        if (cell.Length > 0 || cells.Count > 0)
        {
            cells.Add(cell.ToString());
            yield return cells.ToArray();
        }
    }
}
