# 📊 INSTRUCTOR DASHBOARD API

## 🎯 Overview

Dashboard API cung cấp tổng quan cho Instructor khi đăng nhập vào hệ thống, bao gồm:
- Thống kê tổng quan (classes, groups, projects, students)
- Công việc cần làm (proposals chờ duyệt, submissions cần chấm)
- Danh sách lớp gần đây
- Hoạt động gần đây

---

## 📡 API Endpoint

### **GET /api/instructor/dashboard**

Lấy dashboard overview cho instructor

**Authorization:** Bearer Token (Instructor role)

---

## 📥 Response

```json
{
  "isSuccess": true,
  "message": "Dashboard data retrieved successfully",
  "data": {
    "totalClasses": 3,
    "totalGroups": 15,
    "totalProjects": 12,
    "totalStudents": 75,
    "pendingProposals": 5,
    "submissionsToGrade": 8,
    "recentAnnouncements": 2,
    
    "recentClasses": [
      {
        "classId": 1,
        "className": "IoT Development SE1801",
        "semesterName": "Spring 2025",
        "totalStudents": 30,
        "totalGroups": 6,
        "totalProjects": 5,
        "pendingProposals": 2,
        "lastActivity": "2025-10-23T14:30:00Z"
      },
      {
        "classId": 2,
        "className": "IoT Development SE1802",
        "semesterName": "Spring 2025",
        "totalStudents": 25,
        "totalGroups": 5,
        "totalProjects": 4,
        "pendingProposals": 1,
        "lastActivity": "2025-10-22T10:15:00Z"
      }
    ],
    
    "recentActivities": [
      {
        "activityType": "Proposal",
        "description": "New proposal: Smart Home Automation System",
        "relatedClass": "IoT Development SE1801",
        "relatedGroup": "Team Alpha",
        "activityDate": "2025-10-23T14:30:00Z"
      },
      {
        "activityType": "Announcement",
        "description": "Deadline reminder for Milestone 2",
        "relatedClass": "All Classes",
        "activityDate": "2025-10-23T09:00:00Z"
      },
      {
        "activityType": "Proposal",
        "description": "New proposal: IoT Healthcare Monitoring",
        "relatedClass": "IoT Development SE1802",
        "relatedGroup": "Team Beta",
        "activityDate": "2025-10-22T16:45:00Z"
      }
    ]
  }
}
```

---

## 📊 Data Fields

### **Dashboard Overview**

| Field | Type | Description |
|-------|------|-------------|
| `totalClasses` | int | Tổng số lớp được phân công |
| `totalGroups` | int | Tổng số nhóm trong tất cả lớp |
| `totalProjects` | int | Tổng số project |
| `totalStudents` | int | Tổng số sinh viên |
| `pendingProposals` | int | Số proposal chờ duyệt |
| `submissionsToGrade` | int | Số submission đã approved nhưng chưa chấm điểm |
| `recentAnnouncements` | int | Số thông báo trong 7 ngày gần nhất |

### **Recent Classes**

| Field | Type | Description |
|-------|------|-------------|
| `classId` | int | Class ID |
| `className` | string | Tên lớp |
| `semesterName` | string | Tên học kỳ |
| `totalStudents` | int | Số sinh viên trong lớp |
| `totalGroups` | int | Số nhóm trong lớp |
| `totalProjects` | int | Số project trong lớp |
| `pendingProposals` | int | Số proposal chờ duyệt của lớp này |
| `lastActivity` | datetime | Thời gian hoạt động gần nhất |

### **Recent Activities**

| Field | Type | Description |
|-------|------|-------------|
| `activityType` | string | Loại hoạt động: "Proposal", "Submission", "Grading", "Announcement" |
| `description` | string | Mô tả hoạt động |
| `relatedClass` | string | Tên lớp liên quan |
| `relatedGroup` | string | Tên nhóm liên quan (nếu có) |
| `activityDate` | datetime | Thời gian hoạt động |

---

## 🔍 Business Logic

### **1. Pending Proposals**
- Đếm số `MilestoneSubmission` có:
  - `submissionStatus = "Pending"`
  - Milestone có title chứa "Proposal"
  - Thuộc các lớp của instructor

### **2. Submissions To Grade**
- Đếm số `MilestoneSubmission` có:
  - `submissionStatus = "Approved"`
  - Chưa có `MilestoneEvaluation` tương ứng
  - Thuộc các project của các lớp instructor

### **3. Recent Announcements**
- Đếm số `Announcement` được tạo trong 7 ngày gần nhất bởi instructor

### **4. Recent Classes**
- Lấy top 5 classes
- Sắp xếp theo `lastActivity` (thời gian update gần nhất của projects)

### **5. Recent Activities**
- Kết hợp:
  - Top 5 pending proposals mới nhất
  - Top 3 announcements gần nhất
- Sắp xếp theo thời gian giảm dần
- Lấy tối đa 10 activities

---

## 💡 Use Cases

### **Frontend Dashboard Display**

```typescript
// Example React component
function InstructorDashboard() {
  const [dashboard, setDashboard] = useState(null);
  
  useEffect(() => {
    fetch('/api/instructor/dashboard', {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })
    .then(res => res.json())
    .then(data => setDashboard(data.data));
  }, []);
  
  return (
    <div>
      <h1>Welcome back, Instructor!</h1>
      
      {/* Stats Cards */}
      <div className="stats-grid">
        <StatCard title="Classes" value={dashboard.totalClasses} />
        <StatCard title="Groups" value={dashboard.totalGroups} />
        <StatCard title="Students" value={dashboard.totalStudents} />
      </div>
      
      {/* Action Items */}
      <div className="actions">
        <ActionCard 
          title="Pending Proposals" 
          count={dashboard.pendingProposals}
          link="/instructor/proposals"
        />
        <ActionCard 
          title="To Grade" 
          count={dashboard.submissionsToGrade}
          link="/instructor/grading"
        />
      </div>
      
      {/* Recent Classes */}
      <RecentClassesList classes={dashboard.recentClasses} />
      
      {/* Activities Timeline */}
      <ActivitiesTimeline activities={dashboard.recentActivities} />
    </div>
  );
}
```

---

## 🎨 UI Suggestions

### **Dashboard Layout**

```
┌─────────────────────────────────────────────────────┐
│  Welcome back, Dr. Nguyen Van A                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│  📚 Classes: 3    👥 Groups: 15    🎓 Students: 75 │
│                                                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ⚡ Action Required                                │
│  • 5 Proposals pending review                      │
│  • 8 Submissions to grade                          │
│                                                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│  📖 Recent Classes                                  │
│  • IoT Development SE1801 (2 pending proposals)    │
│  • IoT Development SE1802 (1 pending proposal)     │
│                                                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│  🕐 Recent Activities                               │
│  • New proposal: Smart Home... (just now)          │
│  • Announcement: Deadline reminder (2h ago)        │
│  • New proposal: Healthcare... (1d ago)            │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## 🔒 Security

- Endpoint yêu cầu authentication (Bearer token)
- Chỉ trả về dữ liệu của các lớp mà instructor được phân công
- Instructor ID được lấy từ JWT token (hiện tại hardcode = 1)

---

## 📝 Notes

- Dashboard data được tính toán real-time mỗi lần request
- Có thể cache data trong 5-10 phút để tăng performance
- Recent activities hiển thị tối đa 10 items gần nhất
- Recent classes hiển thị tối đa 5 classes



