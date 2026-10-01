using Form.Entities;
using Form.Interfaces;
using Form.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Form.Repositories;

public class RefreshTokenRepository(AppDbContext _context) : IRefreshTokenRepository
{
    public async Task<RefreshToken> AddAsync(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        // Load the RefreshToken row tracked (we need to mutate IsRevoked /
        // ReplacedByTokenId on it and then call SaveChangesAsync).
        // Do NOT include User.RoleAssignments here — loading a navigation with
        // Include() puts it into the change tracker, and a later SaveChangesAsync
        // will then issue UPDATE [UserRoleAssignments] for every role row it saw,
        // which hits 0 rows and throws DbUpdateConcurrencyException when the
        // in-memory FK values differ from what is stored in the database.
        //
        // The caller (RefreshTokenService.ValidateAndRotateAsync) needs the User
        // only to pass to CreateToken(); fetch it separately with AsNoTracking so
        // it is invisible to the change tracker entirely.
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token is null)
            return null;

        // Load User + RoleAssignments read-only — never enters the change tracker.
        token.User = await _context.Users
            .AsNoTracking()
            .Include(u => u.RoleAssignments)
            .FirstAsync(u => u.Id == token.UserId);

        return token;
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
            token.IsRevoked = true;

        await _context.SaveChangesAsync();
    }
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}