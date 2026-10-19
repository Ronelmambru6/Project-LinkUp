using LinkUpPro.Domain.Entities;
using LinkUpPro.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Persistence.Configurations
{
    public class ShipConfiguration : IEntityTypeConfiguration<Ship>
    {
        public void Configure(EntityTypeBuilder<Ship> builder)
        {
            builder.ToTable("Ships", t =>
                t.HasCheckConstraint("CK_Ships_Tamano", "[Tamano] BETWEEN 2 AND 5"));

            builder.Property(s => s.JugadorId).HasMaxLength(450).IsRequired();

            builder.HasOne(s => s.Game)
                   .WithMany(g => g.Ships)
                   .HasForeignKey(s => s.GameId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(s => s.JugadorId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Para cargar los barcos de un jugador en una partida
            builder.HasIndex(s => new { s.GameId, s.JugadorId });
        }
    }
}
