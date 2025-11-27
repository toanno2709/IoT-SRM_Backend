# 📊 Class Diagrams / Entity Relationship Diagrams (ERD)

## **IoT-SRM Database Schema**

Bộ sơ đồ Class Diagram (ERD style) mô tả cấu trúc cơ sở dữ liệu của hệ thống IoT-SRM Backend.

---

## **📁 Files**

1. **[IoT_SRM_Database_ERD.puml](IoT_SRM_Database_ERD.puml)** - Sơ đồ ERD tổng thể của toàn bộ database

---

## **🎨 Màu sắc và Format**

### **Format Entity (3 phần):**
- **Phần trên**: Tên Entity
- **Phần giữa**: `PK: primary_key` (Primary Key)
- **Phần dưới**: Các thuộc tính khác
  - `FK: foreign_key` (Foreign Key - tham chiếu đến entity khác)
  - Các attributes thông thường

### **Màu sắc Entities:**
- **Xanh lá nhạt (#E8F5E9)**: Hầu hết các entities (User, Role, Class, Group, Project, v.v.)
- **Vàng nhạt (#FFF9C4)**: Semester (entity quan trọng về thời gian học kỳ)
- **Tím nhạt (#E1BEE7)**: Project (entity trung tâm của hệ thống)

---

## **📋 Danh sách Entities**

### **Core User Management:**
- **User**: Tài khoản người dùng (Student, Instructor, Admin)
- **Role**: Vai trò trong hệ thống

### **Semester & Class Management:**
- **Semester**: Học kỳ
- **Class**: Lớp học
- **ClassEnrollment**: Đăng ký lớp học của sinh viên

### **Group Management:**
- **Group**: Nhóm sinh viên trong lớp
- **GroupMember**: Thành viên của nhóm

### **Project Management:**
- **Project**: Dự án IoT của nhóm
- **ProjectMilestone**: Milestone của dự án
- **MilestoneSubmission**: Submission cho milestone
- **SubmissionFile**: File đính kèm trong submission
- **MilestoneEvaluation**: Đánh giá milestone bởi instructor
- **ProjectApprovalHistory**: Lịch sử duyệt topic proposal

### **Communication:**
- **Announcement**: Thông báo từ admin/instructor
- **Notification**: Thông báo cho người dùng

### **Hall of Fame:**
- **HallOfFame**: Danh sách dự án xuất sắc theo semester

---

## **🔗 Relationships (Mối quan hệ)**

### **1-to-Many (1 --- N):**
- **Role** (1) → **User** (N): Một role có nhiều users
- **Semester** (1) → **Class** (N): Một semester có nhiều classes
- **Class** (1) → **Group** (N): Một class có nhiều groups
- **Group** (1) → **Project** (N): Một group có một project
- **Project** (1) → **ProjectMilestone** (N): Một project có nhiều milestones
- **ProjectMilestone** (1) → **MilestoneSubmission** (N): Một milestone có nhiều submissions
- **MilestoneSubmission** (1) → **SubmissionFile** (N): Một submission có nhiều files
- **User** (1) → **Notification** (N): Một user nhận nhiều notifications

### **Many-to-Many (thông qua junction tables):**
- **User** ↔ **Class** (thông qua ClassEnrollment)
- **User** ↔ **Group** (thông qua GroupMember)

---

## **📖 Cách sử dụng**

### **Xem diagrams:**

1. **Cài đặt PlantUML plugin** cho VS Code/IDE
2. **Hoặc sử dụng online**: http://www.plantuml.com/plantuml/uml/
3. **Hoặc render bằng command**:
   ```bash
   plantuml ClassDiagrams/*.puml
   ```

### **Export sang PNG/SVG:**

```bash
# Cài PlantUML
# Windows (Chocolatey):
choco install plantuml

# Render tất cả
plantuml ClassDiagrams/*.puml

# Hoặc render từng file
plantuml ClassDiagrams/IoT_SRM_Database_ERD.puml
```

---

## **📝 Chú ý**

- ✅ **Format**: Tuân thủ format ERD với 3 phần (Entity Name, PK, Attributes)
- ✅ **Màu sắc**: Sử dụng màu xanh lá nhạt cho hầu hết, vàng cho Semester, tím cho Project
- ✅ **Relationships**: Sử dụng ký hiệu 1-to-N (||--o{) và Many-to-Many
- ✅ **Foreign Keys**: Được đánh dấu rõ ràng với prefix "FK:"

---

## **🔗 Related Documentation**

- **Activity Diagrams**: `../ActivityDiagrams/README.md` - Các sơ đồ luồng hoạt động
- **Sequence Diagrams**: `../SequenceDiagrams/README.md` - Các sơ đồ tương tác theo thời gian
- **Notification API Guide**: `../NOTIFICATION_API_GUIDE.md` - Hướng dẫn sử dụng Notification System

---

**Last Updated**: 2025-01-28  
**Based on**: IoT-SRM_Backend Codebase Models  
**Format Reference**: ERD mẫu đã cung cấp

---


