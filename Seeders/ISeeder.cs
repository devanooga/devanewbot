namespace devanewbot.Seeders;

using System.Threading;
using System.Threading.Tasks;

public interface ISeeder
{
    Task Seed(CancellationToken cancellationToken = default);
}
