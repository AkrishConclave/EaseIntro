﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ease_intro_api.Data;

#nullable disable

namespace ease_intro_api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250402194649_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("ease_intro_api.Models.Meet", b =>
                {
                    b.Property<Guid>("Uid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Location")
                        .IsRequired()
                        .HasMaxLength(260)
                        .HasColumnType("varchar(260)");

                    b.Property<int>("StatusId")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(160)
                        .HasColumnType("varchar(160)");

                    b.HasKey("Uid");

                    b.HasIndex("StatusId");

                    b.ToTable("Meets");
                });

            modelBuilder.Entity("ease_intro_api.Models.MeetStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .HasMaxLength(120)
                        .HasColumnType("varchar(120)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(60)
                        .HasColumnType("varchar(60)");

                    b.HasKey("Id");

                    b.ToTable("MeetStatus");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Description = "Встреча запланирована",
                            Title = "Встреча запланирована"
                        },
                        new
                        {
                            Id = 2,
                            Description = "Встреча в процессе",
                            Title = "Встреча в процессе"
                        },
                        new
                        {
                            Id = 3,
                            Description = "Встреча завершена",
                            Title = "Встреча завершена"
                        },
                        new
                        {
                            Id = 4,
                            Description = "Встреча отменена",
                            Title = "Встреча отменена"
                        });
                });

            modelBuilder.Entity("ease_intro_api.Models.Meet", b =>
                {
                    b.HasOne("ease_intro_api.Models.MeetStatus", "Status")
                        .WithMany("Meets")
                        .HasForeignKey("StatusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Status");
                });

            modelBuilder.Entity("ease_intro_api.Models.MeetStatus", b =>
                {
                    b.Navigation("Meets");
                });
#pragma warning restore 612, 618
        }
    }
}
