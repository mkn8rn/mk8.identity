using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using mk8.identity.Infrastructure.Environment;

namespace mk8.identity.Infrastructure.Contexts
{
    public class IdentityContextFactory : IDesignTimeDbContextFactory<IdentityContext>
    {
        public IdentityContext CreateDbContext(string[] args)
        {
            var config = EnvironmentLoader.Load(isDevelopment: true);
            var optionsBuilder = new DbContextOptionsBuilder<IdentityContext>();
            optionsBuilder.UseNpgsql(config.ConnectionString);
            return new IdentityContext(optionsBuilder.Options);
        }
    }

    public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        public ApplicationContext CreateDbContext(string[] args)
        {
            var config = EnvironmentLoader.Load(isDevelopment: true);
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            optionsBuilder.UseNpgsql(config.ConnectionString);
            return new ApplicationContext(optionsBuilder.Options);
        }
    }
}
