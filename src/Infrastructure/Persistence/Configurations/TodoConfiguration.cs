using Domain.Todos.Entities;
using Domain.Todos.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class TodoConfiguration : IEntityTypeConfiguration<Todo>
{
   public void Configure(EntityTypeBuilder<Todo> builder)
   {
      builder.ToTable("todos");

      builder.HasKey(t => t.Id);

      builder.Property(t => t.Id)
          .HasColumnName("id");

      builder.Property(t => t.Title)
          .HasColumnName("title")
          .HasMaxLength(100)
          .IsRequired()
          .HasConversion(
              title => title.Value,
              value => TodoTitle.Create(value).Value!);

      builder.Property(t => t.Status)
          .HasColumnName("status")
          .HasConversion<string>()
          .HasMaxLength(20)
          .IsRequired();

      builder.HasIndex(t => t.Status)
          .HasDatabaseName("ix_todos_status");

      builder.HasIndex(t => t.Created)
          .HasDatabaseName("ix_todos_created");
   }
}
