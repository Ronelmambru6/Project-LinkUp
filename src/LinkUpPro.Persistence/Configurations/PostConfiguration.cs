using LinkUpPro.Domain.Entities;
using LinkUpPro.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Persistence.Configurations
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.ToTable("Posts");

            builder.Property(p => p.AutorId).HasMaxLength(450).IsRequired();
            builder.Property(p => p.Contenido).HasMaxLength(1000).IsRequired();
            builder.Property(p => p.RutaImagen).HasMaxLength(300);
            builder.Property(p => p.UrlYouTube).HasMaxLength(500);

            // El autor es un usuario de Identity. Restrict: nunca se borra nada en cascada.
            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(p => p.AutorId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Para listar el Home y el feed de amigos ordenados por fecha
            builder.HasIndex(p => new { p.AutorId, p.FechaCreacion });
        }
    }
}
