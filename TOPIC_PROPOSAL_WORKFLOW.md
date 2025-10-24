# 📋 TOPIC PROPOSAL WORKFLOW

## 🔄 Luồng hoạt động đề xuất chủ đề (Topic Proposal)

### **BƯỚC 1: Instructor tạo Milestone "Topic Proposal"**

Instructor cần tạo một milestone đặc biệt cho topic proposal khi tạo project template:

```http
POST /api/milestones
Content-Type: application/json

{
  "projectId": 1,
  "title": "Topic Proposal",
  "description": "Submit your project topic proposal for approval",
  "dueDate": "2025-11-30T23:59:59Z",
  "weight": 0  // Không tính điểm hoặc weight nhỏ
}
```

**Lưu ý:** Tên milestone phải chứa từ "Proposal" để hệ thống nhận diện.

---

### **BƯỚC 2: Group nộp Topic Proposal**

Student (leader) nộp proposal qua `MilestoneSubmission`:

```http
POST /api/milestone-submissions
Content-Type: application/json

{
  "projectId": 1,
  "milestoneDefId": 5,  // ID của milestone "Topic Proposal"
  "submissionNote": "Chúng em xin đề xuất chủ đề: IoT Smart Home System...",
  "files": [
    {
      "fileName": "proposal.pdf",
      "fileUrl": "https://cloudinary.com/..."
    }
  ]
}
```

**Kết quả:** Submission được tạo với `submissionStatus = "Pending"`

---

### **BƯỚC 3: Instructor xem danh sách proposals chờ duyệt**

```http
GET /api/instructor/pending-proposals
Authorization: Bearer {token}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Pending proposals retrieved successfully",
  "data": [
    {
      "submissionId": 10,
      "projectId": 1,
      "projectTitle": "IoT Smart Home",
      "projectDescription": "An IoT system for home automation",
      "groupId": 3,
      "groupName": "Group Alpha",
      "leaderId": 25,
      "leaderName": "Nguyen Van A",
      "proposalNote": "Chúng em xin đề xuất...",
      "status": "Pending",
      "submittedAt": "2025-10-23T10:30:00Z",
      "files": [
        {
          "fileId": 12,
          "fileName": "proposal.pdf",
          "fileUrl": "https://..."
        }
      ]
    }
  ]
}
```

---

### **BƯỚC 4: Instructor duyệt proposal**

#### **4a. APPROVE (Chấp nhận)**
```http
POST /api/instructor/proposals/10/review
Content-Type: application/json

{
  "reviewStatus": "Approved",
  "reviewComment": "Topic is interesting and feasible. Approved!"
}
```

**Kết quả:**
- `submissionStatus` → "Approved"
- `projectStatus` → "Approved"
- Tạo record trong `Project_Approval_History`

---

#### **4b. REVISION (Yêu cầu sửa)**
```http
POST /api/instructor/proposals/10/review
Content-Type: application/json

{
  "reviewStatus": "Revision",
  "reviewComment": "Please clarify the scope and add more technical details."
}
```

**Kết quả:**
- `submissionStatus` → "Revision"
- Student cần nộp lại proposal mới

---

#### **4c. REJECT (Từ chối)**
```http
POST /api/instructor/proposals/10/review
Content-Type: application/json

{
  "reviewStatus": "Rejected",
  "reviewComment": "Topic is too broad and not aligned with course objectives."
}
```

**Kết quả:**
- `submissionStatus` → "Rejected"
- Group cần đề xuất topic mới

---

## 🗂️ Database Schema

### **MilestoneSubmission** (cho Topic Proposal)
```sql
submission_id INT PRIMARY KEY
project_id INT FK → Projects
milestone_def_id INT FK → Project_Milestones (milestone "Proposal")
submission_status VARCHAR -- 'Pending', 'Approved', 'Rejected', 'Revision'
submission_note TEXT -- Nội dung proposal
submitted_at DATETIME
```

### **Project_Approval_History**
```sql
history_id INT PRIMARY KEY
submission_id INT FK → Milestone_Submissions
reviewer_id INT FK → Users (instructor)
review_status VARCHAR -- 'Approved', 'Rejected', 'Revision'
review_comment TEXT
reviewed_at DATETIME
```

---

## 🎯 Lưu ý quan trọng

1. **Milestone "Proposal" phải có title chứa từ "Proposal"** để hệ thống filter đúng
2. **Một project chỉ nên có 1 milestone Proposal**
3. **Weight của milestone Proposal nên = 0%** hoặc rất nhỏ
4. **Chỉ Instructor của class mới có quyền review proposal**
5. **Status flow:** `Pending` → `Approved`/`Rejected`/`Revision`

---

## 📌 API Endpoints Summary

| Endpoint | Method | Mô tả | Role |
|----------|--------|-------|------|
| `/api/instructor/pending-proposals` | GET | Xem proposals chờ duyệt | Instructor |
| `/api/instructor/proposals/{id}/review` | POST | Duyệt proposal | Instructor |
| `/api/milestone-submissions` | POST | Nộp proposal | Student |
| `/api/milestone-submissions/{id}` | GET | Xem chi tiết submission | Student/Instructor |



