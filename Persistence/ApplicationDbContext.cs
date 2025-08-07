using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options) { }


    public DbSet<User> Users { get; set; }
    public DbSet<UserLogin> UserLogins { get; set; }
    public DbSet<UserPet> UserPets { get; set; }
    public DbSet<PetType> PetTypes { get; set; }
    public DbSet<PetBreed> PetBreeds { get; set; }
    public DbSet<PetFood> PetFoods{ get; set; }
    public DbSet<PetColor> PetColors { get; set; }
    public DbSet<UserPetColor> UserPetColors { get; set; }
    public DbSet<UserPetMixColor> UserPetMixColors { get; set; }
    public DbSet<UserPetOtherBreed> UserPetOtherBreeds { get; set; }
    public DbSet<UserOtp> UserOtps { get; set; }
    public DbSet<UserType> UserTypes { get; set; }
    public DbSet<ContentCreatorProfile> ContentCreatorProfiles { get; set; }
    public DbSet<PetOwnerProfile> PetOwnerProfiles { get; set; }
    public DbSet<PetBusinessProfile> PetBusinessProfiles { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {

        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new Configurations.UserConfiguration());
    }
}
