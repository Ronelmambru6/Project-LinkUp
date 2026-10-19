using LinkUpPro.Domain.Entities;
using LinkUpPro.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.Property(n => n.DestinatarioId).HasMaxLength(450).IsRequired();
            builder.Property(n => n.ActorId).HasMaxLength(450).IsRequired();
            builder.Property(n => n.Descripcion).HasMaxLength(300).IsRequired();

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(n => n.DestinatarioId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                   .WithMany()
                   .HasForeignKey(n => n.ActorId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Contenido relacionado. Como el borrado es lógico, el registro sigue existiendo
            // y la notificación puede mostrar "ya no se encuentra disponible".
            builder.HasOne<Post>()
                   .WithMany()
                   .HasForeignKey(n => n.PostId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Comment>()
                   .WithMany()
                   .HasForeignKey(n => n.CommentId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Para el contador de no leídas y el listado
            builder.HasIndex(n => new { n.DestinatarioId, n.Leida });
        }
    }
}
