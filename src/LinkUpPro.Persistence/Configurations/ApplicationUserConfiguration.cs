using LinkUpPro.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Persistence.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
            builder.Property(u => u.Apellido).HasMaxLength(100).IsRequired();
            builder.Property(u => u.FotoPerfil).HasMaxLength(300).IsRequired();
        }
    }
}
