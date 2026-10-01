using Form.Entities;
using Form.Interfaces;
using Form.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Form.Repositories;

public class PasswordResetRepository(AppDbContext _context) : IPasswordResetRepository
{
    public async Task<PasswordResetToken> AddAsync(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task<PasswordResetToken?> GetByHashAsync(string tokenHash)
    {
        // Do NOT include t.User with tracking. This token is loaded, then
        // IsUsed is set to true and SaveChangesAsync is called. If User (with
        // its RoleAssignments) is in the change tracker at that point, EF will
        // emit UPDATE statements for those rows — hitting 0 rows and throwing
        // DbUpdateConcurrencyException. The User nav is not needed by any
        // caller of this method, so simply don't load it.
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    public async Task<PasswordResetToken?> GetLatestForUserAsync(Guid userId) =>
    await _context.PasswordResetTokens
        .Where(t => t.UserId == userId && !t.IsUsed)
        .OrderByDescending(t => t.CreatedAt)
        .FirstOrDefaultAsync();


}