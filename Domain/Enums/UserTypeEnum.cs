namespace Domain.Enums;

public enum UserTypeEnum
{   
    // Main Types
    Admin = 1,
    User = 2,

    // User Subtypes (start from 100 range to avoid overlap)
    PetOwner = 101,
    PetBusiness = 102,
    ContentCreator = 103
}

