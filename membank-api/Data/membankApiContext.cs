using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace membankApi.Models
{
    public class membankApiContext : DbContext
    {
        public membankApiContext (DbContextOptions<membankApiContext> options)
            : base(options)
        {
        }

        public DbSet<membankApi.Models.MemeItemModel> MemeItemModel { get; set; }
    }
}
