using LinkUpPro.Domain.Entities;
using LinkUpPro.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Persistence.Configurations
{
    public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
    {
        public void Configure(EntityTypeBuilder<Friendship> builder)
        {
            builder.ToTable("Friendships", t =>
                t.HasCheckConstraint("CK_Friendships_UsuariosDistintos", "[UsuarioAId] <> [UsuarioBId]"));

            builder.Property(f => f.UsuarioAId).HasMaxLength(450).IsRequired();
            builder.Property(f => f.UsuarioBId).HasMaxLength(450).IsRequired();

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(f => f.UsuarioAId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(f => f.UsuarioBId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Enunciado: una sola relación por pareja, aunque se elimine y se reactive.
            // IMPORTANTE: el servicio debe guardar SIEMPRE la pareja en el mismo orden
            // (UsuarioAId = el Id menor según string.CompareOrdinal), si no este índice
            // no detecta la pareja invertida.
            builder.HasIndex(f => new { f.UsuarioAId, f.UsuarioBId }).IsUnique();
        }
    }
}
