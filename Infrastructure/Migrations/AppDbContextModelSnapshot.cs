﻿// <auto-generated />
using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("AppCore.Entities.OrderLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<decimal>("Amount")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<int>("BaseCurrency")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("Exchange")
                        .HasColumnType("int");

                    b.Property<int>("PairsTradingLogId")
                        .HasColumnType("int");

                    b.Property<decimal>("Price")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<int>("QuoteCurrency")
                        .HasColumnType("int");

                    b.Property<decimal>("Total")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("PairsTradingLogId");

                    b.ToTable("OrderLogs");
                });

            modelBuilder.Entity("AppCore.Entities.PairsTradingLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("CryptoCurrency")
                        .HasColumnType("int");

                    b.Property<int>("Currency")
                        .HasColumnType("int");

                    b.Property<decimal?>("LastZ")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<decimal?>("Ratio")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<bool?>("ShortBinance")
                        .HasColumnType("bit");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("PairsTradingLogs");
                });

            modelBuilder.Entity("AppCore.Entities.RatioCheckLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("DifferenceInPercents")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<int>("FirstExchange")
                        .HasColumnType("int");

                    b.Property<decimal>("FirstExchangePrice")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<int>("SecondExchange")
                        .HasColumnType("int");

                    b.Property<decimal>("SecondExchangePrice")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.HasKey("Id");

                    b.ToTable("RatioCheckLogs");
                });

            modelBuilder.Entity("AppCore.Entities.OrderLog", b =>
                {
                    b.HasOne("AppCore.Entities.PairsTradingLog", "PairsTradingLog")
                        .WithMany("OrderLogs")
                        .HasForeignKey("PairsTradingLogId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PairsTradingLog");
                });

            modelBuilder.Entity("AppCore.Entities.PairsTradingLog", b =>
                {
                    b.Navigation("OrderLogs");
                });
#pragma warning restore 612, 618
        }
    }
}
