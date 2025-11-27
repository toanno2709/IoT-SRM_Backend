# 📊 Sequence Diagrams - Instructor & Admin Modules

## **Danh sách các Sequence Diagrams**

Tất cả các diagrams được vẽ dựa trên **code thực tế** trong dự án IoT-SRM_Backend và theo format của hình mẫu đã cung cấp.

---

## **👨‍🏫 2. Instructor Module**

### **🔹 2.2 Class Management**

1. ✅ **[Get Assigned Classes](Get_Assigned_Classes.puml)**
   - Sequence: Instructor → Browser → Controller → ClassService → Database
   - Extract instructorId from JWT token
   - Return list of assigned classes

---

### **🔹 2.3 Group Management**

2. ✅ **[View All Groups in Class](View_Groups_In_Class.puml)**
   - Sequence: Instructor → Browser → Controller → GroupService → Database
   - Get groups by classId with related data

3. ✅ **[Edit Group Info](Edit_Group_Info.puml)**
   - Sequence: Instructor → Browser → Controller → GroupManagementService → Database
   - Validate group exists before update
   - Alt fragment for success/error

4. ✅ **[Add Student to Group](Add_Student_To_Group.puml)**
   - Sequence includes NotificationService for sending notification
   - Validate: student not already in another group
   - Create GroupMember entity

5. ✅ **[Remove Student from Group](Remove_Student_From_Group.puml)**
   - Sequence includes NotificationService for sending notification
   - Remove GroupMember entity
   - Send notification to removed student

---

### **🔹 2.4 Topic Review**

6. ✅ **[View Pending Topic Proposals](View_Pending_Topic_Proposals.puml)**
   - Sequence: Instructor → Browser → Controller → TopicProposalService → Database
   - Filter by instructorId and pending status

7. ✅ **[Review Topic Proposal](Review_Topic_Proposal.puml)**
   - Sequence with authorization check
   - Create ProjectApprovalHistory
   - Conditional update Project status if Approved

---

### **🔹 2.5 Milestone Management**

8. ✅ **[Create Milestone](Create_Milestone.puml)**
   - Sequence includes weight validation
   - Check total weight ≤ 100%
   - Alt fragments for validation errors

9. ✅ **[Edit Milestone](Edit_Milestone.puml)**
   - Sequence with weight validation
   - Conditional weight update check
   - Alt fragments for different scenarios

10. ✅ **[Delete Milestone](Delete_Milestone.puml)**
    - Sequence: Instructor → Browser → Controller → ProjectMilestoneService → Database
    - Validate milestone exists before delete

---

### **🔹 2.6 Grading & Feedback**

11. ✅ **[Grade Submission](Grade_Submission.puml)**
    - Sequence: Instructor → Browser → Controller → MilestoneGradingService → Database
    - Check if evaluation exists (create or update)
    - Save weight ratio snapshot

---

### **🔹 2.7 Reports**

12. ✅ **[Get Class Stats](Get_Class_Stats.puml)**
    - Sequence includes multiple database queries
    - Calculate submission rate and average score
    - Aggregate statistics

---

### **🔹 2.8 Announcements**

13. ✅ **[Send Announcement](Send_Announcement.puml)**
    - Sequence includes NotificationService
    - Conditional logic for different target audiences
    - Bulk notification creation

14. ✅ **[Get Sent Announcements](Get_Sent_Announcements.puml)**
    - Sequence: Instructor → Browser → Controller → AnnouncementService → Database
    - Filter by admin/instructor ID

---

## **🛠️ 3. Admin Module**

### **🔹 3.2 Class Management**

15. ✅ **[Create Class](Create_Class.puml)**
    - Sequence includes semester validation
    - Validate class name uniqueness
    - Optional instructor assignment with role validation

16. ✅ **[Assign Instructor](Assign_Instructor.puml)**
    - Sequence: Admin → Browser → Controller → ClassService → Database
    - Validate instructor exists and has correct role
    - Update class InstructorId

17. ✅ **[Edit Class Info](Edit_Class_Info.puml)**
    - Sequence with multiple conditional updates
    - Validate class name uniqueness
    - Optional instructor update with validation

---

### **🔹 3.3 User Management**

18. ✅ **[Create User Manually](Create_User_Manually.puml)**
    - Sequence includes UserHelper for password hashing
    - Validate email uniqueness
    - Hash password using BCrypt

19. ✅ **[Assign Role](Assign_Role.puml)**
    - Sequence: Admin → Browser → Controller → UserService → Database
    - Authorization check (Admin only)
    - Update user RoleId

20. ✅ **[Reset Password](Reset_Password.puml)**
    - Sequence includes UserHelper for password hashing
    - Authorization check (Admin only)
    - Hash new password using BCrypt

---

### **🔹 3.4 Monitor Projects**

21. ✅ **[View All Groups/Projects](View_All_Groups_Projects.puml)**
    - Sequence includes both GroupService and ProjectService
    - Multiple database queries for groups and projects
    - Combine results

---

### **🔹 3.5 Announcements**

22. ✅ **[Send Global Announcement](Send_Global_Announcement.puml)**
    - Sequence includes NotificationService
    - Conditional logic for Global/Class/Role targets
    - Bulk notification creation

23. ✅ **[Get Sent Announcements (Admin)](Get_Sent_Announcements_Admin.puml)**
    - Sequence: Admin → Browser → Controller → AnnouncementService → Database
    - Filter by admin ID

---

## **📝 Format & Conventions**

### **Participants (Lifelines):**
- **Actor**: `:Instructor` hoặc `:Admin` (stick figure)
- **Browser**: `:Web Application`
- **Controller**: `:InstructorController`, `:ClassesController`, etc.
- **Service**: `:ClassService`, `:GroupService`, etc.
- **Helper**: `:UserHelper` (nếu cần)
- **Database**: `:Database`

### **Message Types:**
- **Synchronous messages**: Solid arrows (`->`)
- **Return messages**: Dashed arrows (`-->`)
- **Self-messages**: Arrows within same participant
- **Messages numbered**: 1, 2, 3... for clear sequence

### **Activation Bars:**
- `activate` when participant starts processing
- `deactivate` when participant finishes

### **Fragments:**
- **Alt fragments**: `alt` / `else` for conditional flows (success/error)
- **Opt fragments**: `opt` for optional steps

### **Error Handling:**
- All diagrams include alt fragments for success/error cases
- Validation errors handled at Browser level
- Business logic errors handled at Service level

---

## **🎯 Key Patterns**

1. **Authentication**: Extract user ID from JWT token in Controller
2. **Validation**: Multiple validation layers (Browser → Controller → Service)
3. **Database Operations**: Clear separation between query and update operations
4. **Error Handling**: Consistent error responses at each layer
5. **Notifications**: Separate NotificationService calls for user notifications

---

## **📖 Cách sử dụng**

### **Xem diagrams:**

1. Cài đặt PlantUML plugin cho VS Code/IDE
2. Hoặc sử dụng online: http://www.plantuml.com/plantuml/uml/
3. Hoặc render bằng command: `plantuml SequenceDiagrams/*.puml`

### **Export sang PNG/SVG:**

```bash
# Cài PlantUML
# Windows (Chocolatey):
choco install plantuml

# Render tất cả
plantuml SequenceDiagrams/*.puml

# Hoặc render từng file
plantuml SequenceDiagrams/Get_Assigned_Classes.puml
```

---

---

## **📝 Chú ý**

- ✅ **Nền trắng**: Tất cả participants và actors có nền trắng, viền đen
- ✅ **Database**: Hiển thị dưới dạng participant (ô vuông) thay vì database icon
- ✅ **Format**: Tuân thủ đúng format của Sequence Diagram mẫu đã cung cấp

---

**Last Updated**: 2025-01-28  
**Based on**: IoT-SRM_Backend Codebase  
**Format Reference**: Sequence Diagram mẫu đã cung cấp

---

## **🔗 Related Documentation**

- **Activity Diagrams**: `../ActivityDiagrams/README.md` - Các sơ đồ luồng hoạt động
- **Notification API Guide**: `../NOTIFICATION_API_GUIDE.md` - Hướng dẫn sử dụng Notification System

---

