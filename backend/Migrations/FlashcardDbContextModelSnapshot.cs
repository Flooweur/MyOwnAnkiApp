using System;
using FlashcardApi.Data;
using FlashcardApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace FlashcardApi.Migrations
{
    [DbContext(typeof(FlashcardDbContext))]
    partial class FlashcardDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("FlashcardApi.Models.Card", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Back")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("CurrentStep")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("DeckId")
                        .HasColumnType("int");

                    b.Property<double>("Difficulty")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("float")
                        .HasDefaultValue(5.0);

                    b.Property<DateTime?>("DueDate")
                        .HasColumnType("datetime2");

                    b.Property<double>("EaseFactor")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("float")
                        .HasDefaultValue(2.5);

                    b.Property<int>("ElapsedSeconds")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("Front")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("LapseCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<DateTime?>("LastReviewedAt")
                        .HasColumnType("datetime2");

                    b.Property<double>("Retrievability")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("float")
                        .HasDefaultValue(1.0);

                    b.Property<int>("ReviewCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int>("ScheduledSeconds")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<double>("Stability")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<int>("State")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.HasKey("Id");

                    b.HasIndex("DeckId");

                    b.HasIndex("DueDate");

                    b.HasIndex("State");

                    b.ToTable("Cards");
                });

            modelBuilder.Entity("FlashcardApi.Models.Deck", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("FsrsParameters")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("[]");

                    b.Property<string>("MediaDirectory")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.ToTable("Decks");
                });

            modelBuilder.Entity("FlashcardApi.Models.ReviewLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CardId")
                        .HasColumnType("int");

                    b.Property<double>("DifficultyAfter")
                        .HasColumnType("float");

                    b.Property<double>("DifficultyBefore")
                        .HasColumnType("float");

                    b.Property<int>("Grade")
                        .HasColumnType("int");

                    b.Property<double>("Retrievability")
                        .HasColumnType("float");

                    b.Property<DateTime>("ReviewedAt")
                        .HasColumnType("datetime2");

                    b.Property<double>("ScheduledInterval")
                        .HasColumnType("float");

                    b.Property<double>("StabilityAfter")
                        .HasColumnType("float");

                    b.Property<double>("StabilityBefore")
                        .HasColumnType("float");

                    b.Property<int>("StateAfter")
                        .HasColumnType("int");

                    b.Property<int>("StateBefore")
                        .HasColumnType("int");

                    b.Property<int?>("TimeToAnswer")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("CardId");

                    b.HasIndex("ReviewedAt");

                    b.ToTable("ReviewLogs");
                });

            modelBuilder.Entity("FlashcardApi.Models.Card", b =>
                {
                    b.HasOne("FlashcardApi.Models.Deck", "Deck")
                        .WithMany("Cards")
                        .HasForeignKey("DeckId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Deck");
                });

            modelBuilder.Entity("FlashcardApi.Models.ReviewLog", b =>
                {
                    b.HasOne("FlashcardApi.Models.Card", "Card")
                        .WithMany("ReviewLogs")
                        .HasForeignKey("CardId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Card");
                });

            modelBuilder.Entity("FlashcardApi.Models.Card", b =>
                {
                    b.Navigation("ReviewLogs");
                });

            modelBuilder.Entity("FlashcardApi.Models.Deck", b =>
                {
                    b.Navigation("Cards");
                });
#pragma warning restore 612, 618
        }
    }
}