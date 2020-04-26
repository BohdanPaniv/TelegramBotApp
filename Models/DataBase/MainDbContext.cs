using MySql.Data.EntityFramework;
using System.Data.Common;
using System.Data.Entity;

namespace TelegramBotApp.Models.DataBase
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class MainDbContext : DbContext
    {
        public MainDbContext(DbConnection existingConnection, bool contextOwnsConnection): base(existingConnection, contextOwnsConnection)
        {

        }
        public DbSet<AdminRights> AllAdmins { get; set; }
        public DbSet<LocationPoints> AllPoints { get; set; }
        public DbSet<OutputLocations> AllRecord { get; set; }
    }
}