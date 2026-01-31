using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WeeklyHabitTracker.Data;

#nullable disable

namespace WeeklyHabitTracker.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260131120000_InitialMotivationalQuotes")]
partial class InitialMotivationalQuotes
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("WeeklyHabitTracker.Models.MotivationalQuote", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

            b.Property<string>("Author")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("Quote")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)");

            b.HasKey("Id");

            b.ToTable("MotivationalQuotes");
        });
    }
}
