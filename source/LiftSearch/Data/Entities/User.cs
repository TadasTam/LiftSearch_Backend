using LiftSearch.Data.Entities.Enums;
using Microsoft.AspNetCore.Identity;

namespace LiftSearch.Data.Entities;

public class User : IdentityUser                                 
{
    public bool forceRelogin { get; set; }
  //  public string Id { get; set; }
    
}

