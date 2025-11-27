# 📊 Activity Diagrams - Instructor & Admin Modules

## **Danh sách các Activity Diagrams**

Tất cả các diagrams được vẽ dựa trên **code thực tế** trong dự án IoT-SRM_Backend.

---

### **🔹 2.2 Class Management**

1. ✅ **[Get Assigned Classes](Get_Assigned_Classes.puml)** - API: `GET /api/instructor/classes`
   - Lấy danh sách classes được gán cho instructor
   - Include: Instructor, Semester, Enrollments, Groups, Projects

---

### **🔹 2.3 Group Management**

2. ✅ **[View All Groups in Class](View_Groups_In_Class.puml)** - API: `GET /api/instructor/classes/{classId}/groups`
   - Xem tất cả groups trong một class

3. ✅ **[Edit Group Info](Edit_Group_Info.puml)** - API: `PUT /api/instructor/groups/{groupId}`
   - Sửa tên và mô tả của group

4. ✅ **[Add Student to Group](Add_Student_To_Group.puml)** - API: `POST /api/instructor/groups/{groupId}/members`
   - Thêm student vào group
   - Validate: student không được ở group khác trong cùng class

5. ✅ **[Remove Student from Group](Remove_Student_From_Group.puml)** - API: `DELETE /api/instructor/groups/{groupId}/members/{userId}`
   - Xóa student khỏi group
   - Gửi notification cho student

---

### **🔹 2.4 Topic Review**

6. ✅ **[View Pending Topic Proposals](View_Pending_Topic_Proposals.puml)** - API: `GET /api/instructor/pending-proposals`
   - Xem danh sách topic proposals chờ duyệt
   - Filter: Chỉ lấy proposals chưa có approval history

7. ✅ **[Review Topic Proposal](Review_Topic_Proposal.puml)** - API: `POST /api/instructor/proposals/{submissionId}/review`
   - Approve/Reject/Request Revision
   - Tạo ProjectApprovalHistory
   - Cập nhật Project status nếu Approved

---

### **🔹 2.5 Milestone Management**

8. ✅ **[Create Milestone](Create_Milestone.puml)** - API: `POST /api/projects/{projectId}/milestones`
   - Tạo milestone mới
   - **Validation**: Total weight ≤ 100%

9. ✅ **[Edit Milestone](Edit_Milestone.puml)** - API: `PUT /api/projects/{projectId}/milestones/{milestoneId}`
   - Sửa milestone
   - **Validation**: Total weight ≤ 100% (khi update weight)

10. ✅ **[Delete Milestone](Delete_Milestone.puml)** - API: `DELETE /api/projects/{projectId}/milestones/{milestoneId}`
    - Xóa milestone

---

### **🔹 2.6 Grading & Feedback**

11. ✅ **[Grade Submission](Grade_Submission.puml)** - API: `POST /api/instructor/milestones/grade`
    - Chấm điểm milestone submission
    - Tạo/Update MilestoneEvaluation
    - Lưu weight ratio snapshot
    - Gửi notification cho students

    ⚠️ **Note**: Tính năng "Restrict editing after publishing" chưa được implement trong code hiện tại.

---

### **🔹 2.7 Reports**

12. ✅ **[Get Class Stats](Get_Class_Stats.puml)** - API: `GET /api/instructor/classes/{classId}/stats`
    - Lấy thống kê class:
      - Total students, projects
      - Submission rate
      - Average score
      - Project status breakdown
      - Project details

---

### **🔹 2.8 Announcements**

13. ✅ **[Send Announcement](Send_Announcement.puml)** - API: `POST /api/instructor/announcements`
    - Gửi announcement đến students/class
    - Tạo Announcement entity
    - Gửi notifications hàng loạt

14. ✅ **[Get Sent Announcements](Get_Sent_Announcements.puml)** - API: `GET /api/instructor/announcements`
    - Lấy danh sách announcements đã gửi
    - Filter theo admin/instructor ID

---

## **📝 Notes**

### **❌ Tính năng CHƯA có implementation:**

- **Set max groups / max members per group**: Chỉ có DTOs (`ClassSettingsDtos.cs`) nhưng chưa có service/controller implementation → **KHÔNG VẼ**

### **⚠️ Tính năng CHƯA hoàn chỉnh:**

- **Restrict editing after publishing**: Logic chấm điểm có update evaluation, nhưng chưa có validation để prevent editing sau khi published → **ĐÃ VẼ** nhưng có note

---

## **🎨 Format Diagrams**

Tất cả diagrams sử dụng **3 swimlanes**:
1. **Instructor** - Hành động của instructor
2. **Web Application** - Frontend logic
3. **Backend API** - Server-side processing

**Ký hiệu:**
- ⚫ **Start node**: Solid black circle
- ⚪ **End node**: Circle with border
- 🔷 **Decision node**: Diamond shape
- 📦 **Activity node**: Rounded rectangle
- ➡️ **Flow**: Arrows connecting nodes

---

## **📖 Cách sử dụng**

### **Xem diagrams:**

1. Cài đặt PlantUML plugin cho VS Code/IDE
2. Hoặc sử dụng online: http://www.plantuml.com/plantuml/uml/
3. Hoặc render bằng command: `plantuml ActivityDiagrams/*.puml`

### **Export sang PNG/SVG:**

```bash
# Cài PlantUML
# Windows (Chocolatey):
choco install plantuml

# Render tất cả
plantuml ActivityDiagrams/*.puml

# Hoặc render từng file
plantuml ActivityDiagrams/Get_Assigned_Classes.puml
```

---

---

## **🛠️ 3. Admin Module**

### **🔹 3.1 Login & Dashboard**

- ❌ **[Admin Dashboard](Admin_Dashboard.puml)** - Dashboard với totals và charts
  - **Note**: Chưa có implementation riêng cho Admin Dashboard trong code hiện tại

---

### **🔹 3.2 Class Management**

15. ✅ **[Create Class](Create_Class.puml)** - Tạo class mới
    - Validate: semester exists, class name unique trong semester
    - Optional: assign instructor khi tạo

16. ✅ **[Assign Instructor](Assign_Instructor.puml)** - Gán instructor cho class
    - Validate: instructor exists và có role Instructor

17. ✅ **[Edit Class Info](Edit_Class_Info.puml)** - Sửa thông tin class
    - Update: ClassName, Description, InstructorId
    - Validate: class name unique trong semester

---

### **🔹 3.3 User Management**

18. ✅ **[Create User Manually](Create_User_Manually.puml)** - Tạo user thủ công
    - Validate: email unique, password requirements
    - Hash password bằng BCrypt

19. ✅ **[Assign Role](Assign_Role.puml)** - Gán role cho user
    - Update RoleId của user
    - Chỉ Admin mới được phép

20. ✅ **[Reset Password](Reset_Password.puml)** - Reset password cho user
    - Hash new password bằng BCrypt
    - Chỉ Admin mới được phép

---

### **🔹 3.4 Monitor Projects**

21. ✅ **[View All Groups/Projects](View_All_Groups_Projects.puml)** - Xem tất cả groups và projects
    - Lấy tất cả groups và projects trong system
    - Include: Members, Leader, Class, Milestones

---

### **🔹 3.5 Announcements**

22. ✅ **[Send Global Announcement](Send_Global_Announcement.puml)** - Gửi announcement toàn hệ thống/class/role
    - Target: Global, Class, hoặc Role
    - Gửi bulk notifications tới target users

23. ✅ **[Get Sent Announcements (Admin)](Get_Sent_Announcements_Admin.puml)** - Lấy announcements đã gửi
    - Filter theo admin ID

---

### **🔹 3.6 Reports & Dashboard**

- ❌ **[System-wide Reports](System_Reports.puml)** - System-wide statistics và charts
  - **Note**: Chưa có implementation riêng cho System-wide reports trong code hiện tại
  - Có thể sử dụng ClassStatsService cho từng class

---

### **🔹 3.7 Hall of Fame & Leaderboard**

- ❌ **[Get Top 10 Projects](Top_10_Projects.puml)** - Lấy top 10 projects theo semester
  - **Note**: Model HallOfFame đã có nhưng chưa có service/controller implementation

- ❌ **[Manage Hall of Fame](Manage_Hall_Of_Fame.puml)** - Quản lý Hall of Fame list
  - **Note**: Model HallOfFame đã có nhưng chưa có service/controller implementation

---

**Last Updated**: 2025-01-28  
**Based on**: IoT-SRM_Backend Codebase

---

## **🔗 Related Documentation**

- **Sequence Diagrams**: `../SequenceDiagrams/README.md` - Các sơ đồ tương tác theo thời gian
- **Notification API Guide**: `../NOTIFICATION_API_GUIDE.md` - Hướng dẫn sử dụng Notification System

---

