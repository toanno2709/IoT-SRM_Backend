using AppBackend.BusinessObjects.Dtos.Group;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroupEntity = AppBackend.BusinessObjects.Models.Group;

using AppBackend.BusinessObjects.Data;

namespace AppBackend.Services.Services.Group
{
    public class GroupService : IGroupService
    {
        private readonly IotShowroomContext _db;
        private readonly ILogger<GroupService> _logger;
        private readonly INotificationHubService _notificationHubService;

        public GroupService(
            IotShowroomContext db, 
            ILogger<GroupService> logger,
            INotificationHubService notificationHubService)
        {
            _db = db;
            _logger = logger;
            _notificationHubService = notificationHubService;
        }

        private async Task SendNotificationAsync(int userId, string title, string message, string type = "system", string? data = null)
        {
            try
            {
                var note = new AppBackend.BusinessObjects.Models.Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Data = data,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Notifications.Add(note);
                await _db.SaveChangesAsync();

                // Send real-time notification via SignalR
                var user = await _db.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var notificationDto = new NotificationResponseDto
                    {
                        NotificationId = note.NotificationId,
                        UserId = userId,
                        UserName = user.FullName,
                        Title = title,
                        Message = message,
                        Type = type,
                        Data = data,
                        IsRead = false,
                        CreatedAt = note.CreatedAt
                    };

                    if (type == "group_invitation")
                    {
                        await _notificationHubService.SendGroupInvitationNotificationAsync(user.Email, notificationDto);
                    }
                    else
                    {
                        await _notificationHubService.SendNotificationToUserAsync(user.Email, notificationDto);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notification to user {userId}");
            }
        }

        // 1. Create group: check user not in any group in same class -> create group and add leader as GroupMember role "Leader"
        public async Task<GroupCreateResultDto> CreateGroupAsync(GroupCreateDto dto, int creatorUserId)
        {
            // Check class configuration for group formation deadline
            var classConfig = await _db.ClassConfigurations
                .FirstOrDefaultAsync(c => c.ClassId == dto.ClassId);

            if (classConfig != null)
            {
                // Check if group formation deadline has passed
                if (!classConfig.IsGroupFormationAllowed())
                {
                    throw new InvalidOperationException(
                        $"Group formation deadline has passed on {classConfig.GroupFormationDeadline:yyyy-MM-dd HH:mm}. Cannot create new groups.");
                }

                // Check if student group creation is allowed
                if (!classConfig.AllowStudentCreateGroup)
                {
                    throw new InvalidOperationException("Student group creation is not allowed for this class.");
                }

                // Check max groups limit
                var currentGroupCount = await _db.Groups.CountAsync(g => g.ClassId == dto.ClassId);
                if (currentGroupCount >= classConfig.MaxGroupsAllowed)
                {
                    throw new InvalidOperationException(
                        $"Maximum number of groups ({classConfig.MaxGroupsAllowed}) has been reached for this class.");
                }
            }

            // check user not already in a group in same class
            var inSameClass = await _db.GroupMembers
                .Include(gm => gm.Group)
                .Where(gm => gm.UserId == creatorUserId && gm.Group != null && gm.Group.ClassId == dto.ClassId)
                .AnyAsync();

            if (inSameClass)
                throw new InvalidOperationException("User already in a group in this class.");

            var group = new GroupEntity
            {
                ClassId = dto.ClassId,
                GroupName = dto.GroupName,
                Description = dto.Description,
                LeaderId = creatorUserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Groups.Add(group);
            await _db.SaveChangesAsync(); // have GroupId

            // add group member as leader
            var gm = new GroupMember
            {
                GroupId = group.GroupId,
                UserId = creatorUserId,
                RoleInGroup = "Leader",
                JoinedAt = DateTime.UtcNow
            };
            _db.GroupMembers.Add(gm);
            await _db.SaveChangesAsync();
        
            // Create Data JSON for notification
            var notificationData = System.Text.Json.JsonSerializer.Serialize(new
            {
                classId = group.ClassId,
                groupId = group.GroupId,
                groupName = group.GroupName
            });
        
            // send notification to creator
            await SendNotificationAsync(creatorUserId, "Group Created", $"You have successfully created group '{group.GroupName}'.", "group_create", notificationData);

            return new GroupCreateResultDto(group.GroupId, group.GroupName, group.LeaderId, group.ClassId);
        }

        // 2. Invite: inviter must be group leader; invited user must not belong to any group in same class
        public async Task InviteMemberAsync(GroupInviteDto dto)
        {
            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            if (group.LeaderId != dto.InviterUserId)
                throw new InvalidOperationException("Only group leader can invite members.");

            // check invited user existence
            var invited = await _db.Users.FindAsync(dto.InvitedUserId);
            if (invited == null) throw new KeyNotFoundException("Invited user not found.");

            // check invited not in group in same class
            var alreadyIn = await _db.GroupMembers
                .Include(gm => gm.Group)
                .Where(gm => gm.UserId == dto.InvitedUserId && gm.Group != null && gm.Group.ClassId == group.ClassId)
                .AnyAsync();

            if (alreadyIn) throw new InvalidOperationException("Invited user already in a group in this class.");

            // NEW: Check if user already has a pending invitation to this group
            var existingInvitation = await _db.Notifications
                .Where(n => n.UserId == dto.InvitedUserId &&
                           n.Type == "group_invitation" &&
                           (n.IsRead == null || n.IsRead == false) &&
                           (n.Message ?? "").Contains($"[groupId:{dto.GroupId}]"))
                .FirstOrDefaultAsync();

            if (existingInvitation != null)
            {
                _logger.LogWarning($"User {dto.InvitedUserId} already has a pending invitation to group {dto.GroupId}");
                throw new InvalidOperationException("This user already has a pending invitation to this group.");
            }

            // Get inviter name for better notification
            var inviter = await _db.Users.FindAsync(dto.InviterUserId);
            var inviterName = inviter?.FullName ?? "A group leader";

            // Send notification to INVITED USER with SignalR support
            await SendNotificationAsync(dto.InvitedUserId,
                $"Invitation to join group {group.GroupName}",
                $"You have been invited by {inviterName} to join group '{group.GroupName}'. [groupId:{dto.GroupId}][classId:{group.ClassId}]",
                "group_invitation");

            _logger.LogInformation($"Group invitation sent from user {dto.InviterUserId} to user {dto.InvitedUserId} for group {dto.GroupId}");
        }

        // 3. Accept invite: add to group if not in other group in same class, remove any previous invite-notif? (we keep simple)
        public async Task AcceptInviteAsync(GroupAcceptInviteDto dto)
        {
            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            // check user not already in a group in same class
            var inSameClass = await _db.GroupMembers
                .Include(gm => gm.Group)
                .Where(gm => gm.UserId == dto.UserId && gm.Group != null && gm.Group.ClassId == group.ClassId)
                .AnyAsync();

            if (inSameClass)
                throw new InvalidOperationException("User already in a group in this class.");

            // add membership
            var gm = new GroupMember
            {
                GroupId = dto.GroupId,
                UserId = dto.UserId,
                RoleInGroup = "Member",
                JoinedAt = DateTime.UtcNow
            };
            _db.GroupMembers.Add(gm);

            // Mark invite notification as read - check both old "invite" type and new "group_invitation" type
            var possibleInvite = await _db.Notifications
                .Where(n => n.UserId == dto.UserId && 
                       (n.Type == "invite" || n.Type == "group_invitation") && 
                       n.Title != null && n.Title.Contains(group.GroupName!))
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            if (possibleInvite != null)
            {
                possibleInvite.IsRead = true;
            }

            await _db.SaveChangesAsync();
            
            // send notification to group leader
            var acceptingUser = await _db.Users.FindAsync(dto.UserId);
            var userName = acceptingUser?.FullName ?? "A new member";
            
            await SendNotificationAsync(group.LeaderId ?? 0,
                "Member Joined Group",
                $"{userName} has joined your group '{group.GroupName}'.",
                "group_update");
        }

        // 4. Leave group
        public async Task LeaveGroupAsync(GroupLeaveDto dto)
        {
            var gm = await _db.GroupMembers.FirstOrDefaultAsync(x => x.GroupId == dto.GroupId && x.UserId == dto.UserId);
            if (gm == null) throw new InvalidOperationException("Membership not found.");

            var group = await _db.Groups.Include(g => g.GroupMembers).FirstOrDefaultAsync(g => g.GroupId == dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            // if leaving member is leader
            if (group.LeaderId == dto.UserId)
            {
                // if other members exist -> promote first member to leader
                var otherMember = await _db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == dto.GroupId && m.UserId != dto.UserId);
                if (otherMember != null)
                {
                    group.LeaderId = otherMember.UserId;
                    otherMember.RoleInGroup = "Leader";
                    _db.GroupMembers.Remove(gm); // remove old leader membership
                }
                else
                {
                    // no other members: remove group and its members
                    // remove membership and group
                    _db.GroupMembers.Remove(gm);
                    _db.Groups.Remove(group);
                    await _db.SaveChangesAsync();
                    return;
                }
            }
            else
            {
                // normal member leaving
                _db.GroupMembers.Remove(gm);
            }

            await _db.SaveChangesAsync();
            // send notification to group leader
            await SendNotificationAsync(group.LeaderId ?? 0,
               "Member Left Group",
               $"A member has left your group '{group.GroupName}'.",
               "group_update");
        }

        // 5. Kick member (leader only)
        public async Task KickMemberAsync(GroupKickDto dto)
        {
            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            if (group.LeaderId != dto.RequesterUserId) throw new InvalidOperationException("Only leader can remove members.");

            var gm = await _db.GroupMembers.FirstOrDefaultAsync(x => x.GroupId == dto.GroupId && x.UserId == dto.TargetUserId);
            if (gm == null) throw new KeyNotFoundException("Member not found in the group.");

            // can't kick yourself
            if (dto.TargetUserId == dto.RequesterUserId) throw new InvalidOperationException("Leader cannot kick themselves.");

            _db.GroupMembers.Remove(gm);
            await _db.SaveChangesAsync();

            // send notification to kicked user
            await SendNotificationAsync(dto.TargetUserId,
                $"Removed from group {group.GroupName}",
                $"You have been removed from group '{group.GroupName}'.",
                "group_removed");
        }

        // 6. Update group (leader only)
        public async Task UpdateGroupAsync(GroupUpdateDto dto)
        {
            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            if (group.LeaderId != dto.RequesterUserId) throw new InvalidOperationException("Only leader can update group.");

            if (!string.IsNullOrWhiteSpace(dto.GroupName)) group.GroupName = dto.GroupName;
            if (dto.Description != null) group.Description = dto.Description;
            group.UpdatedAt = DateTime.UtcNow;

            _db.Groups.Update(group);
            await _db.SaveChangesAsync();
            var memberIds = await _db.GroupMembers
                .Where(m => m.GroupId == group.GroupId && m.UserId != group.LeaderId)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var uid in memberIds)
                await SendNotificationAsync(uid, "Group Updated", $"Group '{group.GroupName}' information has been updated.", "group_update");
        }

        // 7. Delete group (Admin or Instructor or Leader depending policy) – here only Admin (role id 1) or Instructor (role id 3?) or leader
        public async Task DeleteGroupAsync(int groupId, int requesterUserId)
        {
            var group = await _db.Groups.Include(g => g.GroupMembers).FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            var requester = await _db.Users.FindAsync(requesterUserId);
            if (requester == null) throw new KeyNotFoundException("Requester user not found.");

            var isLeader = group.LeaderId == requesterUserId;
            var isAdmin = requester.RoleId == 1;
            var isInstructor = requester.RoleId == 2 || requester.RoleId == 3; // adjust based on your role mapping

            if (!isLeader && !isAdmin && !isInstructor) throw new InvalidOperationException("Not allowed to delete group.");

            // optional: check there is no project associated
            var hasProject = await _db.Projects.AnyAsync(p => p.GroupId == groupId);
            if (hasProject) throw new InvalidOperationException("Cannot delete group with an associated project.");

            var memberIds = group.GroupMembers.Select(m => m.UserId).ToList();

            // remove members and group
            _db.GroupMembers.RemoveRange(group.GroupMembers);
            _db.Groups.Remove(group);
            await _db.SaveChangesAsync();

            foreach (var uid in memberIds)
                await SendNotificationAsync(uid, "Group Deleted", $"Your group '{group.GroupName}' has been deleted.", "group_deleted");
        }

        // 8. Get groups by class (simple list)
        public async Task<IEnumerable<GroupListItemDto>> GetGroupListByClassAsync(int classId)
        {
            var results = await _db.Groups
                .Where(g => g.ClassId == classId)
                .Select(g => new GroupListItemDto
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    LeaderId = g.LeaderId,
                    MemberCount = _db.GroupMembers.Count(m => m.GroupId == g.GroupId)
                })
                .ToListAsync();

            return results;
        }

        // Get groups by class with ResultModel (for controller compatibility)
        public async Task<ResultModel<List<GroupResponseDto>>> GetGroupsByClassAsync(int classId)
        {
            try
            {
                var groups = await _db.Groups
                    .Include(g => g.Leader)
                    .Include(g => g.Class)
                    .Include(g => g.GroupMembers).ThenInclude(gm => gm.User)
                    .Include(g => g.Projects)
                    .Where(g => g.ClassId == classId)
                    .ToListAsync();

                var dtos = groups.Select(g => new GroupResponseDto
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    Description = g.Description,
                    LeaderId = g.LeaderId,
                    LeaderName = g.Leader?.FullName,
                    ClassId = g.ClassId,
                    ClassName = g.Class?.ClassName,
                    CreatedAt = g.CreatedAt,
                    UpdatedAt = g.UpdatedAt,
                    MemberCount = g.GroupMembers?.Count ?? 0,
                    ProjectCount = g.Projects?.Count ?? 0,
                    Members = (g.GroupMembers ?? new List<GroupMember>())
                        .Select(m => new AppBackend.Services.ApiModels.Commons.GroupMemberDto
                        {
                            UserId = m.UserId,
                            FullName = m.User?.FullName,
                            Email = m.User?.Email,
                            RoleInGroup = m.RoleInGroup,
                            AvatarUrl = m.User?.AvatarUrl  // Added AvatarUrl mapping
                        }).ToList()
                }).ToList();

                return new ResultModel<List<GroupResponseDto>>
                {
                    IsSuccess = true,
                    Message = "Groups retrieved successfully",
                    Data = dtos
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<List<GroupResponseDto>>
                {
                    IsSuccess = false,
                    Message = $"Error retrieving groups: {ex.Message}",
                    Data = null
                };
            }
        }

        // 9. Get group detail
        public async Task<GroupDetailDto> GetGroupDetailAsync(int groupId)
        {
            var group = await _db.Groups
                .Include(g => g.GroupMembers)
                    .ThenInclude(gm => gm.User)
                .Include(g => g.Projects)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null) throw new KeyNotFoundException("Group not found.");

            // FIXED: Find actual leader from members with "Leader" role
            // The leaderId in Groups table may be outdated, so we use the actual role in GroupMembers
            var actualLeader = group.GroupMembers.FirstOrDefault(m => 
                m.RoleInGroup != null && 
                m.RoleInGroup.Equals("Leader", StringComparison.OrdinalIgnoreCase));
            
            // Use the actual leader's ID from GroupMembers, not from Groups.LeaderId
            int? correctLeaderId = actualLeader?.UserId;
            
            // Log if there's a mismatch for debugging
            if (actualLeader != null && group.LeaderId != actualLeader.UserId)
            {
                _logger.LogWarning(
                    "Group {GroupId} has leaderId mismatch. Groups.LeaderId={OldLeaderId}, but actual Leader in GroupMembers is UserId={ActualLeaderId}",
                    groupId, group.LeaderId, actualLeader.UserId);
            }

            var dto = new GroupDetailDto
            {
                GroupId = group.GroupId,
                ClassId = group.ClassId,
                GroupName = group.GroupName,
                Description = group.Description,
                LeaderId = correctLeaderId, // Use correct leader ID from GroupMembers
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt,
                Members = group.GroupMembers.Select(m => new AppBackend.BusinessObjects.Dtos.Group.GroupMemberDto
                {
                    GmId = m.GmId,
                    UserId = m.UserId,
                    FullName = m.User?.FullName,
                    Email = m.User?.Email,
                    AvatarUrl = m.User?.AvatarUrl,
                    RoleInGroup = m.RoleInGroup,
                    JoinedAt = m.JoinedAt
                }).ToList(),
                ProjectIds = group.Projects?.Select(p => p.ProjectId).ToList() ?? new List<int>()
            };

            return dto;
        }

        /// <summary>
        /// Create random groups for students who don't have a group yet
        /// </summary>
        public async Task<ResultModel<RandomGroupCreationResultDto>> CreateRandomGroupsAsync(int classId, int instructorId)
        {
            try
            {
                // 1. Verify instructor authorization
                var classEntity = await _db.Classes
                    .FirstOrDefaultAsync(c => c.ClassId == classId && c.InstructorId == instructorId);

                if (classEntity == null)
                {
                    return new ResultModel<RandomGroupCreationResultDto>
                    {
                        IsSuccess = false,
                        Message = "Class not found or you are not the instructor of this class",
                        StatusCode = 403
                    };
                }

                // 2. Get class configuration
                var config = await _db.ClassConfigurations
                    .FirstOrDefaultAsync(c => c.ClassId == classId);

                if (config == null)
                {
                    return new ResultModel<RandomGroupCreationResultDto>
                    {
                        IsSuccess = false,
                        Message = "Class configuration not found. Please set up class configuration first.",
                        StatusCode = 400
                    };
                }

                int minMembers = config.MinMembersPerGroup;
                int maxMembers = config.MaxMembersPerGroup;

                // 3. Get all students enrolled in the class
                var allStudents = await _db.ClassEnrollments
                    .Include(ce => ce.Student)
                    .Where(ce => ce.ClassId == classId)
                    .Select(ce => ce.Student)
                    .ToListAsync();

                // 4. Get students who already have a group in this class
                var studentsWithGroups = await _db.GroupMembers
                    .Include(gm => gm.Group)
                    .Where(gm => gm.Group != null && gm.Group.ClassId == classId)
                    .Select(gm => gm.UserId)
                    .Distinct()
                    .ToListAsync();

                // 5. Get unassigned students (shuffle for randomness)
                var unassignedStudents = allStudents
                    .Where(s => s != null && !studentsWithGroups.Contains(s.UserId))
                    .OrderBy(x => Guid.NewGuid()) // Random shuffle
                    .ToList();

                if (!unassignedStudents.Any())
                {
                    return new ResultModel<RandomGroupCreationResultDto>
                    {
                        IsSuccess = true,
                        Message = "All students already have groups",
                        Data = new RandomGroupCreationResultDto
                        {
                            ClassId = classId,
                            ClassName = classEntity.ClassName ?? "Unknown",
                            TotalStudentsInClass = allStudents.Count,
                            StudentsAlreadyInGroups = studentsWithGroups.Count,
                            UnassignedStudents = 0,
                            GroupsCreated = 0,
                            StudentsAssigned = 0,
                            StudentsRemaining = 0,
                            MinMembersPerGroup = minMembers,
                            MaxMembersPerGroup = maxMembers,
                            Message = "No unassigned students to create groups"
                        },
                        StatusCode = 200
                    };
                }

                // 6. Calculate optimal group distribution
                int totalUnassigned = unassignedStudents.Count;
                List<CreatedGroupSummaryDto> createdGroups = new();
                int groupCounter = await GetNextGroupNumberAsync(classId);
                int studentsAssigned = 0;

                var now = DateTime.UtcNow;

                // Create groups with optimal size distribution
                while (unassignedStudents.Any())
                {
                    int remainingStudents = unassignedStudents.Count;
                    
                    // If remaining students less than min, stop
                    if (remainingStudents < minMembers)
                    {
                        break;
                    }

                    // Calculate group size for this iteration
                    int groupSize = maxMembers;
                    
                    // If remaining students would leave less than minMembers for next group
                    // adjust current group size
                    if (remainingStudents > maxMembers && 
                        remainingStudents - maxMembers < minMembers)
                    {
                        // Distribute more evenly
                        groupSize = (int)Math.Ceiling(remainingStudents / 2.0);
                        groupSize = Math.Min(groupSize, maxMembers);
                        groupSize = Math.Max(groupSize, minMembers);
                    }

                    // Take students for this group
                    var groupMembers = unassignedStudents.Take(groupSize).ToList();
                    unassignedStudents = unassignedStudents.Skip(groupSize).ToList();

                    // Select random leader from group members
                    var leader = groupMembers.First();

                    // Create group
                    string groupName = $"Group {groupCounter}";
                    var group = new GroupEntity
                    {
                        ClassId = classId,
                        GroupName = groupName,
                        Description = $"Auto-generated group for class {classEntity.ClassName}",
                        LeaderId = leader.UserId,
                        CreatedAt = now
                    };

                    _db.Groups.Add(group);
                    await _db.SaveChangesAsync();

                    // Add all members to group
                    foreach (var student in groupMembers)
                    {
                        var member = new GroupMember
                        {
                            GroupId = group.GroupId,
                            UserId = student.UserId,
                            RoleInGroup = student.UserId == leader.UserId ? "Leader" : "Member",
                            JoinedAt = now
                        };
                        _db.GroupMembers.Add(member);

                        // Create Data JSON for notification
                        var notificationData = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            classId = classId,
                            groupId = group.GroupId,
                            groupName = groupName
                        });

                        // Send notification to each student
                        await SendNotificationAsync(
                            student.UserId,
                            "Added to Group",
                            $"You have been automatically assigned to '{groupName}' in {classEntity.ClassName}",
                            "group_create",
                            notificationData
                        );
                    }

                    await _db.SaveChangesAsync();

                    createdGroups.Add(new CreatedGroupSummaryDto
                    {
                        GroupId = group.GroupId,
                        GroupName = groupName,
                        LeaderId = leader.UserId,
                        LeaderName = leader.FullName ?? "Unknown",
                        MemberCount = groupMembers.Count,
                        MemberNames = groupMembers.Select(m => m.FullName ?? "Unknown").ToList()
                    });

                    studentsAssigned += groupMembers.Count;
                    groupCounter++;
                }

                return new ResultModel<RandomGroupCreationResultDto>
                {
                    IsSuccess = true,
                    Message = $"Successfully created {createdGroups.Count} groups with {studentsAssigned} students",
                    Data = new RandomGroupCreationResultDto
                    {
                        ClassId = classId,
                        ClassName = classEntity.ClassName ?? "Unknown",
                        TotalStudentsInClass = allStudents.Count,
                        StudentsAlreadyInGroups = studentsWithGroups.Count,
                        UnassignedStudents = totalUnassigned,
                        GroupsCreated = createdGroups.Count,
                        StudentsAssigned = studentsAssigned,
                        StudentsRemaining = unassignedStudents.Count,
                        MinMembersPerGroup = minMembers,
                        MaxMembersPerGroup = maxMembers,
                        CreatedGroups = createdGroups,
                        Message = unassignedStudents.Any() 
                            ? $"Created {createdGroups.Count} groups. {unassignedStudents.Count} students remaining (insufficient for another group)"
                            : $"All unassigned students have been assigned to groups"
                    },
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating random groups for class {classId}");
                return new ResultModel<RandomGroupCreationResultDto>
                {
                    IsSuccess = false,
                    Message = $"Error creating random groups: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        /// <summary>
        /// Get the next available group number for a class
        /// </summary>
        private async Task<int> GetNextGroupNumberAsync(int classId)
        {
            var existingGroups = await _db.Groups
                .Where(g => g.ClassId == classId && g.GroupName != null && g.GroupName.StartsWith("Group "))
                .Select(g => g.GroupName)
                .ToListAsync();

            int maxNumber = 0;
            foreach (var name in existingGroups)
            {
                var parts = name!.Split(' ');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int num))
                {
                    maxNumber = Math.Max(maxNumber, num);
                }
            }

            return maxNumber + 1;
        }
    }
}