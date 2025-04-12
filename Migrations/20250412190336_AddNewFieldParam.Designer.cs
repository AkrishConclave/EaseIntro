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
    [Migration("20250412190336_AddNewFieldParam")]
    partial class AddNewFieldParam
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

                    b.Property<bool>("AllowedPlusOne")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("LimitMembers")
                        .HasColumnType("int");

                    b.Property<string>("Location")
                        .IsRequired()
                        .HasMaxLength(260)
                        .HasColumnType("varchar(260)");

                    b.Property<int>("OwnerId")
                        .HasColumnType("int");

                    b.Property<int>("StatusId")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(160)
                        .HasColumnType("varchar(160)");

                    b.HasKey("Uid");

                    b.HasIndex("OwnerId");

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
                        },
                        new
                        {
                            Id = 5,
                            Description = "Открыто для регистрации",
                            Title = "Открыто для регистрации"
                        });
                });

            modelBuilder.Entity("ease_intro_api.Models.Member", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Companion")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<string>("Contact")
                        .IsRequired()
                        .HasMaxLength(160)
                        .HasColumnType("varchar(160)");

                    b.Property<Guid>("MeetGuid")
                        .HasColumnType("char(36)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<string>("QrCode")
                        .IsRequired()
                        .HasMaxLength(160)
                        .HasColumnType("varchar(160)");

                    b.Property<int>("Role")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("MeetGuid");

                    b.ToTable("Member");
                });

            modelBuilder.Entity("ease_intro_api.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("varchar(512)");

                    b.Property<string>("PublicContact")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("PublicName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(60)
                        .HasColumnType("varchar(60)");

                    b.Property<string>("UserEmail")
                        .IsRequired()
                        .HasMaxLength(160)
                        .HasColumnType("varchar(160)");

                    b.Property<int?>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ease_intro_api.Models.Meet", b =>
                {
                    b.HasOne("ease_intro_api.Models.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ease_intro_api.Models.MeetStatus", "Status")
                        .WithMany("Meets")
                        .HasForeignKey("StatusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");

                    b.Navigation("Status");
                });

            modelBuilder.Entity("ease_intro_api.Models.Member", b =>
                {
                    b.HasOne("ease_intro_api.Models.Meet", "Meet")
                        .WithMany("Members")
                        .HasForeignKey("MeetGuid")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Meet");
                });

            modelBuilder.Entity("ease_intro_api.Models.User", b =>
                {
                    b.HasOne("ease_intro_api.Models.User", null)
                        .WithMany("Users")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("ease_intro_api.Models.Meet", b =>
                {
                    b.Navigation("Members");
                });

            modelBuilder.Entity("ease_intro_api.Models.MeetStatus", b =>
                {
                    b.Navigation("Meets");
                });

            modelBuilder.Entity("ease_intro_api.Models.User", b =>
                {
                    b.Navigation("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
