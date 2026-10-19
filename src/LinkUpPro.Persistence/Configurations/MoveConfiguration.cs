using LinkUpPro.Domain.Entities;
using LinkUpPro.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Persistence.Configurations
{
    public class MoveConfiguration : IEntityTypeConfiguration<Move>
    {
        public void Configure(EntityTypeBuilder<Move> builder)
        {
            builder.ToTable("Moves");

            builder.Property(m => m.AtacanteId).HasMaxLength(450).IsRequired();

            builder.HasOne(m => m.Game)
                   .WithMany(g => g.Moves)
                   .HasForeignKey(m => m.GameId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(m => m.AtacanteId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Enunciado: un jugador no puede atacar dos veces la misma celda
            builder.HasIndex(m => new { m.GameId, m.AtacanteId, m.Fila, m.Columna }).IsUnique();
        }
    }
}
