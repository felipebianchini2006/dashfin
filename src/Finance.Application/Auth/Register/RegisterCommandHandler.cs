using Finance.Application.Abstractions;
using Finance.Application.Auth.Models;
using Finance.Application.Categories.Seeding;
using Finance.Application.Common;
using Finance.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Auth.Register;

internal sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<UserProfileDto>>
{
  private readonly IAppDbContext _db;
  private readonly IPasswordHasher _passwordHasher;

  public RegisterCommandHandler(IAppDbContext db, IPasswordHasher passwordHasher)
  {
    _db = db;
    _passwordHasher = passwordHasher;
  }

  public async Task<Result<UserProfileDto>> Handle(RegisterCommand request, CancellationToken ct)
  {
    var exists = await _db.Users.AnyAsync(u => u.Email == request.Email, ct);
    if (exists)
      return Result.Fail<UserProfileDto>(Error.Conflict("Email already registered."));

    var user = new User
    {
      Id = Guid.NewGuid(),
      Email = request.Email,
      Timezone = "America/Sao_Paulo",
      Currency = "BRL",
      DisplayPreferences = new UserDisplayPreferences()
    };
    user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

    _db.Users.Add(user);

    foreach (var name in DefaultCategories.Names)
    {
      _db.Categories.Add(new Category
      {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        Name = name
      });
    }

    await _db.SaveChangesAsync(ct);

    return Result.Ok(ToProfile(user));
  }

  private static UserProfileDto ToProfile(User user) =>
    new(
      user.Email,
      user.Timezone,
      user.Currency,
      new UserDisplayPreferencesDto(user.DisplayPreferences.Theme, user.DisplayPreferences.CompactMode));
}
