namespace CliniSys.Infrastructure.Persistence.Seeds;

public interface IDataSeeder
{
    int Order { get; }
    Task SeedAsync();
}
