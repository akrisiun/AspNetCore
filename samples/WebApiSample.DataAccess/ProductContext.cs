using Microsoft.EntityFrameworkCore;
using WebApiSample.DataAccess.Models;

namespace WebApiSample.DataAccess
{
    public class ProductContext : DbContext
    {
        public ProductContext() : this(new DbContextOptions<ProductContext>())
        { }

        public ProductContext(DbContextOptions<ProductContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
           options.UseInMemoryDatabase("SpatialFriends");

            // var conn = @"Data Source=localhost,1433;Initial Catalog=TEST1;
            //    Persist Security Info=True;User ID=test;Password=test;";
            // options.UseSqlServer(
            //    conn);

            // ;Database=SpatialFriends;ConnectRetryCount=0");
            // b => b.UseNetTopologySuite());
        }

        public DbSet<Product> Products { get; set; }
    }
}
