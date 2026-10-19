using LinkUpPro.Domain.Entities;
using LinkUpPro.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Persistence.Configurations
{
    public class GameConfiguration : IEntityTypeConfiguration<Game>
    {
        public void Configure(EntityTypeBuilder<Game> builder)
        {
            builder.ToTable("Games", t =>
                t.HasCheckConstraint("CK_Games_JugadoresDistintos", "[CreadorId] <> [OponenteId]"));

            builder.Property(g => g.CreadorId).HasMaxLength(450).IsRequired();
            builder.Property(g => g.OponenteId).HasMaxLength(450).IsRequired();
            builder.Property(g => g.TurnoUsuarioId).HasMaxLength(450);
            builder.Property(g => g.GanadorId).HasMaxLength(450);

            // Token de concurrencia: si dos jugadores guardan a la vez, el segundo
            // recibe DbUpdateConcurrencyException en lugar de pisar al primero.
            builder.Property(g => g.RowVersion).IsRowVersion();

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(g => g.CreadorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(g => g.OponenteId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Enunciado: una sola partida activa entre los mismos jugadores.
            // Cubre la misma dirección; la contraria la valida el servicio de la #14.
            builder.HasIndex(g => new { g.CreadorId, g.OponenteId })
                   .IsUnique()
                   .HasFilter("[Estado] <> 2"); // 2 = Finalizada
        }
    }
}
