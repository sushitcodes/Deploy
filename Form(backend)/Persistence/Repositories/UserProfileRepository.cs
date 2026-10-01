using Form.Entities;
using Form.Interfaces;
using Form.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Form.Repositories;

public class UserProfileRepository(AppDbContext _context) : IUserProfileRepository
{
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<UserProfile> AddAsync(UserProfile profile)
    {
        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return await GetByUserIdAsync(profile.UserId) ?? profile;
    }

    public async Task<UserProfile?> UpdateAsync(Guid userId, UserProfile updated)
    {
        // Do NOT include p.User here. Including a navigation property tracks
        // it, and SaveChangesAsync will then emit UPDATE [Users] for a row we
        // never intended to change — potentially hitting 0 rows on a stale
        // entity and throwing DbUpdateConcurrencyException.
        // The caller (MapToDto) needs p.User.Email — re-fetch it read-only below.
        var existing = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (existing is null) return null;

        existing.Address = updated.Address;
        existing.Gender = updated.Gender;
        existing.PhoneNumbers = updated.PhoneNumbers;

        if (!string.IsNullOrEmpty(updated.AvatarUrl))
            existing.AvatarUrl = updated.AvatarUrl;

        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Re-attach User as no-tracking so MapToDto can read Email/IsActive.
        existing.User = await _context.Users
            .AsNoTracking()
            .FirstAsync(u => u.Id == userId);

        return existing;
    }

    public async Task<UserProfile?> GetByMemberNumberAsync(int memberNumber) =>
        await _context.UserProfiles.FirstOrDefaultAsync(p => p.MemberNumber == memberNumber);

    public async Task<(List<UserProfile> Items, int TotalCount)> SearchAsync(
        int page, int pageSize, string? search, int? rollNo, UserRole? role)
    {
        var query =
            from u in _context.Users
            join p in _context.UserProfiles on u.Id equals p.UserId into profileJoin
            from p in profileJoin.DefaultIfEmpty()
            select new { User = u, Profile = p };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.User.Email.Contains(search) ||
                (x.Profile != null && x.Profile.FullName.Contains(search)));
        }
        if (rollNo.HasValue)
            query = query.Where(x => x.Profile != null && x.Profile.MemberNumber == rollNo.Value);

        if (role.HasValue)
            query = query.Where(x => x.User.RoleAssignments.Any(ra => ra.Role == role.Value));

        var totalCount = await query.CountAsync();

        var raw = await query
            .OrderBy(x => x.Profile != null ? x.Profile.MemberNumber : int.MaxValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Placeholder now matches the CURRENT UserProfile shape — Phone is
        // gone, so Address/PhoneNumbers get sensible empty defaults instead.
        var items = raw.Select(x => x.Profile ?? new UserProfile
        {
            UserId = x.User.Id,
            User = x.User,
            FullName = x.User.Email,
            Address = string.Empty,
            PhoneNumbers = new List<string>(),
            MemberNumber = 0,
            //IsActive = x.User.IsActive

        }).ToList();

        return (items, totalCount);
    }
    public async Task<List<UserProfile>> GetByUserIdsAsync(List<Guid> userIds) =>
    await _context.UserProfiles.Where(p => userIds.Contains(p.UserId)).ToListAsync();

    public async Task<UserProfile?> AdminUpdateNameAsync(Guid userId, string fullName)
    {
        // No Include(p => p.User) — same reason as UpdateAsync above.
        var existing = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (existing is null) return null;

        existing.FullName = fullName;
        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Re-attach User read-only so MapToDto can access Email/IsActive.
        existing.User = await _context.Users
            .AsNoTracking()
            .FirstAsync(u => u.Id == userId);

        return existing;
    }
}