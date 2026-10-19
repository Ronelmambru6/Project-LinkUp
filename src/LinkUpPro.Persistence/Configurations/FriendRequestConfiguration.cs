using LinkUpPro.Domain.Entities;
using LinkUpPro.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Persistence.Configurations
{
    public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
    {
        public void Configure(EntityTypeBuilder<FriendRequest> builder)
        {
            builder.ToTable("FriendRequests", t =>
                t.HasCheckConstraint("CK_FriendRequests_UsuariosDistintos", "[EmisorId] <> [ReceptorId]"));

            builder.Property(r => r.EmisorId).HasMaxLength(450).IsRequired();
            builder.Property(r => r.ReceptorId).HasMaxLength(450).IsRequired();

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(r => r.EmisorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(r => r.ReceptorId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Enunciado: no puede haber dos solicitudes "En espera" entre la misma pareja.
            // Este índice cubre la misma dirección (A→B dos veces). La dirección contraria
            // (B→A mientras A→B sigue pendiente) la valida el servicio de la #8 dentro de una
            // transacción, justo antes de guardar.
            builder.HasIndex(r => new { r.EmisorId, r.ReceptorId })
                   .IsUnique()
                   .HasFilter("[Estado] = 0"); // 0 = EnEspera

            // Para el contador del menú y el listado de pendientes recibidas
            builder.HasIndex(r => new { r.ReceptorId, r.Estado });
        }
    }
}
