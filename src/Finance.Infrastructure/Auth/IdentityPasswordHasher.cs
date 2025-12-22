using Finance.Application.Abstractions;
using Finance.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Finance.Infrastructure.Auth;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
  private readonly PasswordHasher<User> _hasher = new();

  public string HashPassword(User user, string password) => _hasher.HashPassword(user, password);

  public bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
    => _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword) is PasswordVerificationResult.Success
      or PasswordVerificationResult.SuccessRehashNeeded;
}

