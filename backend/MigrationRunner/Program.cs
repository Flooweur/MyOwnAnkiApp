using Microsoft.EntityFrameworkCore;
using FlashcardApi.Data;

namespace MigrationRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = "Server=localhost,1433;Database=FlashcardDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";

            var optionsBuilder = new DbContextOptionsBuilder<FlashcardDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            using var context = new FlashcardDbContext(optionsBuilder.Options);

            try
            {
                Console.WriteLine("Applying database migrations...");
                context.Database.Migrate();
                Console.WriteLine("Migrations applied successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying migrations: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
