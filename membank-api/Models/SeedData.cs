using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace membankApi.Models
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new membankApiContext(
                serviceProvider.GetRequiredService<DbContextOptions<membankApiContext>>()))
            {
                // Look for any movies.
                if (context.MemeItemModel.Count() > 0)
                {
                    return;   // DB has been seeded
                }

                context.MemeItemModel.AddRange(
                    new MemeItemModel
                    {
                        Title = "Is Mayo an Instrument?",
                        Url = "https://i.kym-cdn.com/photos/images/original/001/371/723/be6.jpg",
                        Tags = "spongebob",
                        Uploaded = "07-10-18 4:20T18:25:43.511Z",
                        Width = "768",
                        Height = "432"
                    }


                );
                context.SaveChanges();
            }
        }
    }
}
