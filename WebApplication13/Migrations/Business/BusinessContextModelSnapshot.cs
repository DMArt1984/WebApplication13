﻿// <auto-generated />
using FactPortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace FactPortal.Migrations.Business
{
    [DbContext(typeof(BusinessContext))]
    partial class BusinessContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("FactPortal.Models.Alert", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("DT")
                        .HasColumnType("text");

                    b.Property<string>("Message")
                        .HasColumnType("text");

                    b.Property<int>("ServiceObjectId")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<string>("groupFilesId")
                        .HasColumnType("text");

                    b.Property<string>("myUserId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ServiceObjectId");

                    b.ToTable("Alerts");
                });

            modelBuilder.Entity("FactPortal.Models.Level", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("LinkId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Levels");
                });

            modelBuilder.Entity("FactPortal.Models.ObjectClaim", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<int>("ServiceObjectId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ServiceObjectId");

                    b.ToTable("Claims");
                });

            modelBuilder.Entity("FactPortal.Models.QueryColumn", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("element")
                        .HasColumnType("text");

                    b.Property<string>("group")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("RepColumn");
                });

            modelBuilder.Entity("FactPortal.Models.QueryCondition", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("IdColumn")
                        .HasColumnType("integer");

                    b.Property<string>("condition")
                        .HasColumnType("text");

                    b.Property<string>("value1")
                        .HasColumnType("text");

                    b.Property<string>("value2")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("RepCondition");
                });

            modelBuilder.Entity("FactPortal.Models.QueryFormula", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("AndOr")
                        .HasColumnType("text");

                    b.Property<int>("IdLeft")
                        .HasColumnType("integer");

                    b.Property<int>("IdRight")
                        .HasColumnType("integer");

                    b.Property<string>("typeLeft")
                        .HasColumnType("text");

                    b.Property<string>("typeRight")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("RepFormula");
                });

            modelBuilder.Entity("FactPortal.Models.QueryView", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("IdColumns")
                        .HasColumnType("text");

                    b.Property<int>("IdFormula")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("RepView");
                });

            modelBuilder.Entity("FactPortal.Models.ServiceObject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("ObjectCode")
                        .HasColumnType("text");

                    b.Property<string>("ObjectTitle")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ServiceObjects");
                });

            modelBuilder.Entity("FactPortal.Models.Step", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<int>("Index")
                        .HasColumnType("integer");

                    b.Property<int>("ServiceObjectId")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .HasColumnType("text");

                    b.Property<string>("groupFilesId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ServiceObjectId");

                    b.ToTable("Steps");
                });

            modelBuilder.Entity("FactPortal.Models.Work", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("ServiceObjectId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ServiceObjectId");

                    b.ToTable("Works");
                });

            modelBuilder.Entity("FactPortal.Models.WorkStep", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("DT_Start")
                        .HasColumnType("text");

                    b.Property<string>("DT_Stop")
                        .HasColumnType("text");

                    b.Property<int>("Index")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .HasColumnType("text");

                    b.Property<int>("WorkId")
                        .HasColumnType("integer");

                    b.Property<string>("groupFilesId")
                        .HasColumnType("text");

                    b.Property<string>("myUserId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("WorkSteps");
                });

            modelBuilder.Entity("FactPortal.Models.myDictionary", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Text")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Dic");
                });

            modelBuilder.Entity("FactPortal.Models.myEvent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Date")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("BEvents");
                });

            modelBuilder.Entity("FactPortal.Models.myFiles", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Path")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Files");
                });

            modelBuilder.Entity("FactPortal.Models.Alert", b =>
                {
                    b.HasOne("FactPortal.Models.ServiceObject", null)
                        .WithMany("Alerts")
                        .HasForeignKey("ServiceObjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FactPortal.Models.ObjectClaim", b =>
                {
                    b.HasOne("FactPortal.Models.ServiceObject", null)
                        .WithMany("Claims")
                        .HasForeignKey("ServiceObjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FactPortal.Models.Step", b =>
                {
                    b.HasOne("FactPortal.Models.ServiceObject", null)
                        .WithMany("Steps")
                        .HasForeignKey("ServiceObjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FactPortal.Models.Work", b =>
                {
                    b.HasOne("FactPortal.Models.ServiceObject", null)
                        .WithMany("Works")
                        .HasForeignKey("ServiceObjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
