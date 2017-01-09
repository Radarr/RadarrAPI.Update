using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RadarrAPI.Database.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20170109110902_AddUpdatePackage")]
    partial class AddUpdatePackage
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752");

            modelBuilder.Entity("RadarrAPI.Database.Models.UpdateChanges", b =>
                {
                    b.Property<int>("UpdateChangesId");

                    b.Property<string>("FixedStr");

                    b.Property<string>("NewStr");

                    b.HasKey("UpdateChangesId");

                    b.ToTable("UpdateChanges");
                });

            modelBuilder.Entity("RadarrAPI.Database.Models.UpdatePackage", b =>
                {
                    b.Property<int>("UpdatePackageId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Branch");

                    b.Property<string>("Filename");

                    b.Property<string>("Hash");

                    b.Property<DateTime>("ReleaseDate");

                    b.Property<string>("Url");

                    b.Property<string>("VersionStr");

                    b.HasKey("UpdatePackageId");

                    b.ToTable("UpdatePackages");
                });

            modelBuilder.Entity("RadarrAPI.Database.Models.UpdateChanges", b =>
                {
                    b.HasOne("RadarrAPI.Database.Models.UpdatePackage", "UpdatePackage")
                        .WithOne("Changes")
                        .HasForeignKey("RadarrAPI.Database.Models.UpdateChanges", "UpdateChangesId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
