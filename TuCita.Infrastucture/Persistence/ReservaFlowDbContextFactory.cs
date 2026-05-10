using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TuCita.Infrastucture.Persistence;

public sealed class ReservaFlowDbContextFactory : IDesignTimeDbContextFactory<ReservaFlowDbContext>
{
    public ReservaFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TUCITA_DESIGNTIME_CONNECTION")
            ?? "Server=localhost\\SQLEXPRESS01;Database=TuCita;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";

        var options = new DbContextOptionsBuilder<ReservaFlowDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ReservaFlowDbContext(options);
    }
}
