# ⚙️ CLASS SETTINGS API

## 🎯 Overview

API cho phép Instructor cấu hình các giới hạn cho lớp học:
- **Max Groups** - Số lượng nhóm tối đa trong lớp
- **Max Members Per Group** - Số thành viên tối đa trong một nhóm
- **Min Members Per Group** - Số thành viên tối thiểu trong một nhóm

Hệ thống sẽ cảnh báo nếu có nhóm hiện tại vi phạm các giới hạn mới.

---

## 📡 API Endpoints

### **1. GET /api/instructor/classes/{classId}/settings**
Lấy cấu hình hiện tại của class

**Response:**
```json
{
  "isSuccess": true,
  "message": "Settings retrieved successfully",
  "data": {
    "classId": 1,
    "className": "IoT Development SE1801",
    "maxGroups": 10,
    "maxMembersPerGroup": 5,
    "minMembersPerGroup": 3,
    "currentGroupCount": 6,
    "largestGroupSize": 5,
    "smallestGroupSize": 4,
    "warnings": []
  }
}
```

---

### **2. PUT /api/instructor/classes/{classId}/settings**
Cập nhật cấu hình class

**Request Body:**
```json
{
  "maxGroups": 12,
  "maxMembersPerGroup": 6,
  "minMembersPerGroup": 3
}
```

**Tất cả fields đều optional** - chỉ cần gửi những field muốn cập nhật.

**Response Success:**
```json
{
  "isSuccess": true,
  "message": "Settings updated successfully",
  "data": {
    "classId": 1,
    "className": "IoT Development SE1801",
    "maxGroups": 12,
    "maxMembersPerGroup": 6,
    "minMembersPerGroup": 3,
    "currentGroupCount": 6,
    "largestGroupSize": 5,
    "smallestGroupSize": 4,
    "warnings": []
  }
}
```

**Response With Warnings:**
```json
{
  "isSuccess": true,
  "message": "Settings updated with warnings",
  "data": {
    "classId": 1,
    "className": "IoT Development SE1801",
    "maxGroups": 5,
    "maxMembersPerGroup": 4,
    "minMembersPerGroup": 3,
    "currentGroupCount": 6,
    "largestGroupSize": 5,
    "smallestGroupSize": 2,
    "warnings": [
      "Current group count (6) exceeds new max groups limit (5)",
      "Group 'Team Alpha' has 5 members, exceeds new max (4)",
      "Group 'Team Gamma' has 2 members, below new min (3)"
    ]
  }
}
```

---

## 🔒 Business Rules

### **1. Validation Rules**
- `maxGroups`: 1 - 100
- `maxMembersPerGroup`: 1 - 20
- `minMembersPerGroup`: 1 - 20
- **Min members KHÔNG được lớn hơn Max members**

### **2. Warning System**
Hệ thống sẽ cảnh báo (nhưng vẫn cho phép update) nếu:
- ✅ Số nhóm hiện tại > max groups mới
- ✅ Có nhóm có số members > max members mới
- ✅ Có nhóm có số members < min members mới

### **3. Soft Enforcement**
- ⚠️ Cấu hình mới được apply ngay lập tức
- ⚠️ Các nhóm hiện tại KHÔNG bị xóa/điều chỉnh tự động
- ⚠️ Instructor cần xử lý manual các nhóm vi phạm
- ✅ Khi tạo nhóm mới sẽ check theo settings mới

---

## 📊 Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `classId` | int | Class ID |
| `className` | string | Tên lớp |
| `maxGroups` | int? | Số nhóm tối đa (null = unlimited) |
| `maxMembersPerGroup` | int? | Số thành viên tối đa/nhóm |
| `minMembersPerGroup` | int? | Số thành viên tối thiểu/nhóm |
| `currentGroupCount` | int | Số nhóm hiện tại |
| `largestGroupSize` | int? | Nhóm lớn nhất hiện có bao nhiêu người |
| `smallestGroupSize` | int? | Nhóm nhỏ nhất hiện có bao nhiêu người |
| `warnings` | string[] | Danh sách cảnh báo |

---

## ⚠️ Error Responses

### **Class Not Found**
```json
{
  "isSuccess": false,
  "message": "Class not found",
  "data": null
}
```

### **Min > Max Validation**
```json
{
  "isSuccess": false,
  "message": "Min members cannot be greater than max members",
  "data": null
}
```

### **Invalid Range**
```json
{
  "isSuccess": false,
  "message": "Max groups must be between 1 and 100",
  "data": null
}
```

---

## 💡 Use Cases

### **Use Case 1: Instructor thiết lập giới hạn ban đầu**
```http
PUT /api/instructor/classes/1/settings
Content-Type: application/json

{
  "maxGroups": 10,
  "maxMembersPerGroup": 5,
  "minMembersPerGroup": 3
}
```

**Kết quả:** Class được cấu hình với giới hạn:
- Tối đa 10 nhóm
- Mỗi nhóm 3-5 thành viên

---

### **Use Case 2: Tăng giới hạn vì lớp đông**
```http
PUT /api/instructor/classes/1/settings
Content-Type: application/json

{
  "maxGroups": 15,
  "maxMembersPerGroup": 6
}
```

**Kết quả:** Cho phép nhiều nhóm hơn và nhóm lớn hơn

---

### **Use Case 3: Giảm giới hạn nhưng có nhóm vượt**
```http
PUT /api/instructor/classes/1/settings
Content-Type: application/json

{
  "maxMembersPerGroup": 4
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Settings updated with warnings",
  "data": {
    ...
    "warnings": [
      "Group 'Team Alpha' has 5 members, exceeds new max (4)"
    ]
  }
}
```

**Action:** Instructor cần vào group management để di chuyển thành viên thừa.

---

### **Use Case 4: Chỉ update một field**
```http
PUT /api/instructor/classes/1/settings
Content-Type: application/json

{
  "maxGroups": 8
}
```

**Kết quả:** Chỉ maxGroups được update, các settings khác giữ nguyên.

---

## 🔐 Security

- ✅ Requires authentication (Bearer token)
- ✅ Chỉ Instructor của class mới được update settings
- ⚠️ TODO: Validate instructor ownership

---

## 📊 Database Schema

### **Classes Table** (Updated)
```sql
ALTER TABLE Classes
ADD COLUMN max_groups INT NULL,
ADD COLUMN max_members_per_group INT NULL,
ADD COLUMN min_members_per_group INT NULL;
```

---

## 🎨 Frontend Integration

```typescript
// Get current settings
async function getClassSettings(classId: number) {
  const response = await fetch(`/api/instructor/classes/${classId}/settings`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
}

// Update settings
async function updateClassSettings(classId: number, settings: ClassSettingsUpdate) {
  const response = await fetch(`/api/instructor/classes/${classId}/settings`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(settings)
  });
  return response.json();
}

// Example usage
const result = await updateClassSettings(1, {
  maxGroups: 10,
  maxMembersPerGroup: 5,
  minMembersPerGroup: 3
});

if (result.data.warnings.length > 0) {
  // Show warnings to instructor
  alert('Settings updated but there are warnings:\n' + 
        result.data.warnings.join('\n'));
}
```

---

## 🎯 UI Suggestions

### **Settings Form**
```
┌────────────────────────────────────────┐
│  Class Settings                        │
├────────────────────────────────────────┤
│                                        │
│  Max Groups:         [10]              │
│  Current: 6 groups                     │
│                                        │
│  Max Members/Group:  [5]               │
│  Current range: 4-5 members            │
│                                        │
│  Min Members/Group:  [3]               │
│                                        │
│  [Cancel]  [Save Settings]             │
│                                        │
└────────────────────────────────────────┘
```

### **Warning Display**
```
⚠️ Settings Updated with Warnings

✓ Settings have been saved
⚠️ The following groups need attention:

• Team Alpha (5 members) - exceeds new max (4)
  → Remove 1 member or increase max

• Team Gamma (2 members) - below new min (3)
  → Add 1 member or decrease min

[View Groups]  [Dismiss]
```

---

## 📝 Notes

- Settings chỉ affect việc tạo groups/add members MỚI
- Các groups hiện tại KHÔNG bị force adjust
- Instructor nhận warnings và tự quyết định xử lý
- Có thể set null để remove giới hạn
- Frontend nên hiển thị current status để instructor quyết định



