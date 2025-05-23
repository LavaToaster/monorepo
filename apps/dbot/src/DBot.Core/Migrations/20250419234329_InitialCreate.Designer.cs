﻿// <auto-generated />
using System;
using DBot.Core.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DBot.Core.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250419234329_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.4");

            modelBuilder.Entity("DBot.Core.Data.Entities.AssettoServerEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("ApiUrl")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("LastChecked")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ApiUrl")
                        .IsUnique();

                    b.ToTable("AssettoServers");
                });

            modelBuilder.Entity("DBot.Core.Data.Entities.AssettoServerGuildEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("AssettoServerEntityId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AssettoServerEntityId");

                    b.HasIndex("GuildId", "AssettoServerEntityId")
                        .IsUnique();

                    b.ToTable("GuildConfigurations");
                });

            modelBuilder.Entity("DBot.Core.Data.Entities.AssettoServerMonitorEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("TEXT");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("ServerEntityId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ThumbnailUrl")
                        .IsRequired()
                        .HasMaxLength(1024)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ServerEntityId");

                    b.HasIndex("GuildId", "ChannelId", "MessageId")
                        .IsUnique();

                    b.ToTable("StatusMessages");
                });

            modelBuilder.Entity("DBot.Core.Data.Entities.AssettoServerGuildEntity", b =>
                {
                    b.HasOne("DBot.Core.Data.Entities.AssettoServerEntity", "ServerEntity")
                        .WithMany("GuildConfigurations")
                        .HasForeignKey("AssettoServerEntityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ServerEntity");
                });

            modelBuilder.Entity("DBot.Core.Data.Entities.AssettoServerMonitorEntity", b =>
                {
                    b.HasOne("DBot.Core.Data.Entities.AssettoServerEntity", "ServerEntity")
                        .WithMany("StatusMessages")
                        .HasForeignKey("ServerEntityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ServerEntity");
                });

            modelBuilder.Entity("DBot.Core.Data.Entities.AssettoServerEntity", b =>
                {
                    b.Navigation("GuildConfigurations");

                    b.Navigation("StatusMessages");
                });
#pragma warning restore 612, 618
        }
    }
}
