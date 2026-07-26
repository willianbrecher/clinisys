namespace CliniSys.Infrastructure.Persistence.Seeds;

public class DatabaseSeeder(IEnumerable<IDataSeeder> seeders)
{
    public async Task SeedAsync()
    {
        foreach (var seeder in seeders.OrderBy(s => s.Order))
            await seeder.SeedAsync();
    }
}
