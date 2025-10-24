# 👥 GROUP MANAGEMENT API

## 🎯 Overview

API cho phép Instructor quản lý các nhóm sinh viên trong lớp, bao gồm:
- Cập nhật thông tin nhóm (tên, mô tả)
- Thêm sinh viên vào nhóm
- Xóa sinh viên khỏi nhóm
- Cập nhật vai trò của thành viên

---

## 📡 API Endpoints

### **1. PUT /api/instructor/groups/{groupId}**
Cập nhật thông tin group (tên và mô tả)

**Request Body:**
```json
{
  "groupName": "Team Alpha",
  "description": "IoT Smart Home Development Team"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Group updated successfully",
  "data": {
    "groupId": 5,
    "groupName": "Team Alpha",
    "description": "IoT Smart Home Development Team",
    "leaderId": 25,
    "leaderName": "Nguyen Van A",
    "classId": 1,
    "className": "IoT Development SE1801",
    "createdAt": "2025-09-01T10:00:00Z",
    "updatedAt": "2025-10-23T15:30:00Z",
    "memberCount": 5,
    "projectCount": 1
  }
}
```

---

### **2. POST /api/instructor/groups/{groupId}/members**
Thêm thành viên vào nhóm

**Request Body:**
```json
{
  "userId": 30,
  "roleInGroup": "Member"  // "Leader", "Deputy", "Member"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Member added successfully",
  "data": {
    "groupId": 5,
    "groupName": "Team Alpha",
    "userId": 30,
    "userName": "Tran Thi B",
    "email": "tranthib@example.com",
    "roleInGroup": "Member",
    "operation": "Added",
    "operationDate": "2025-10-23T15:45:00Z"
  }
}
```

---

### **3. DELETE /api/instructor/groups/{groupId}/members/{userId}**
Xóa thành viên khỏi nhóm

**Response:**
```json
{
  "isSuccess": true,
  "message": "Member removed successfully",
  "data": {
    "groupId": 5,
    "groupName": "Team Alpha",
    "userId": 30,
    "userName": "Tran Thi B",
    "email": "tranthib@example.com",
    "roleInGroup": "Member",
    "operation": "Removed",
    "operationDate": "2025-10-23T16:00:00Z"
  }
}
```

---

### **4. PUT /api/instructor/groups/{groupId}/members/{userId}/role**
Cập nhật vai trò của thành viên trong nhóm

**Request Body:**
```json
{
  "roleInGroup": "Deputy"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Member role updated successfully",
  "data": {
    "groupId": 5,
    "groupName": "Team Alpha",
    "userId": 28,
    "userName": "Le Van C",
    "email": "levanc@example.com",
    "roleInGroup": "Deputy",
    "operation": "Updated",
    "operationDate": "2025-10-23T16:15:00Z"
  }
}
```

---

## 🔒 Business Rules

### **1. Update Group Info**
- ✅ Group name phải có độ dài từ 3-255 ký tự
- ✅ Description tối đa 1000 ký tự
- ✅ Tự động cập nhật `updatedAt` timestamp

### **2. Add Member**
- ✅ User phải tồn tại trong hệ thống
- ❌ Không được thêm user đã có trong nhóm
- ✅ Role mặc định là "Member" nếu không chỉ định
- ✅ Roles hợp lệ: "Leader", "Deputy", "Member"

### **3. Remove Member**
- ✅ Member phải tồn tại trong nhóm
- ❌ **KHÔNG thể xóa Leader** - phải chuyển leader cho người khác trước
- ✅ Xóa cascade các dữ liệu liên quan (nếu có)

### **4. Update Member Role**
- ✅ Member phải tồn tại trong nhóm
- ✅ Role mới phải hợp lệ

---

## 📋 Validation Rules

### **GroupUpdateRequestDto**
```csharp
{
  "groupName": "Required, 3-255 chars",
  "description": "Optional, max 1000 chars"
}
```

### **AddGroupMemberRequestDto**
```csharp
{
  "userId": "Required",
  "roleInGroup": "Optional, max 50 chars, default='Member'"
}
```

### **UpdateMemberRoleRequestDto**
```csharp
{
  "roleInGroup": "Required, max 50 chars"
}
```

---

## ⚠️ Error Responses

### **Group Not Found**
```json
{
  "isSuccess": false,
  "message": "Group not found",
  "data": null
}
```

### **User Not Found**
```json
{
  "isSuccess": false,
  "message": "User not found",
  "data": null
}
```

### **Member Already Exists**
```json
{
  "isSuccess": false,
  "message": "User is already a member of this group",
  "data": null
}
```

### **Cannot Remove Leader**
```json
{
  "isSuccess": false,
  "message": "Cannot remove group leader. Please assign a new leader first.",
  "data": null
}
```

### **Member Not Found**
```json
{
  "isSuccess": false,
  "message": "Member not found in this group",
  "data": null
}
```

---

## 💡 Use Cases

### **Use Case 1: Instructor cập nhật tên nhóm**
```http
PUT /api/instructor/groups/5
Content-Type: application/json

{
  "groupName": "Team Alpha - Updated",
  "description": "IoT Smart Home with AI Integration"
}
```

---

### **Use Case 2: Instructor thêm sinh viên vào nhóm thiếu người**
```http
POST /api/instructor/groups/5/members
Content-Type: application/json

{
  "userId": 35,
  "roleInGroup": "Member"
}
```

---

### **Use Case 3: Instructor xóa sinh viên không tham gia**
```http
DELETE /api/instructor/groups/5/members/32
```

---

### **Use Case 4: Instructor chỉ định Deputy leader**
```http
PUT /api/instructor/groups/5/members/28/role
Content-Type: application/json

{
  "roleInGroup": "Deputy"
}
```

---

## 🔐 Security

- ✅ Requires authentication (Bearer token)
- ✅ Instructor chỉ có thể quản lý groups trong các class được phân công
- ✅ Validate group ownership trước khi cho phép thao tác
- ⚠️ TODO: Thêm check instructor có quyền với class không

---

## 📊 Database Schema

### **Groups Table**
```sql
group_id INT PRIMARY KEY
group_name VARCHAR(255) NOT NULL
description TEXT
leader_id INT FK → Users
class_id INT FK → Classes
created_at DATETIME
updated_at DATETIME
```

### **Group_Members Table**
```sql
group_member_id INT PRIMARY KEY
group_id INT FK → Groups
user_id INT FK → Users
role_in_group VARCHAR(50)
joined_at DATETIME
```

---

## 🎨 Frontend Integration Example

```typescript
// Update group info
async function updateGroup(groupId: number, data: GroupUpdateRequest) {
  const response = await fetch(`/api/instructor/groups/${groupId}`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  });
  return response.json();
}

// Add member
async function addMember(groupId: number, userId: number, role: string = 'Member') {
  const response = await fetch(`/api/instructor/groups/${groupId}/members`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ userId, roleInGroup: role })
  });
  return response.json();
}

// Remove member
async function removeMember(groupId: number, userId: number) {
  const response = await fetch(`/api/instructor/groups/${groupId}/members/${userId}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
}

// Update role
async function updateRole(groupId: number, userId: number, newRole: string) {
  const response = await fetch(`/api/instructor/groups/${groupId}/members/${userId}/role`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ roleInGroup: newRole })
  });
  return response.json();
}
```

---

## 📝 Notes

- Group leader được lưu trong `Groups.leader_id`
- Member roles: "Leader", "Deputy", "Member"
- Khi xóa member, không xóa user khỏi class
- Khi xóa member, cần check xem có ảnh hưởng đến projects không




