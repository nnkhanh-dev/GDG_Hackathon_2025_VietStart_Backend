# Chức năng Tính Embedding và Gợi Ý Người Dùng Phù Hợp

## Tổng quan

Hệ thống sử dụng **Gemini Embedding API** để tính toán vector embeddings và **Weighted Cosine Similarity** để gợi ý những người dùng phù hợp nhất cho mỗi startup.

## Kiến trúc

### 1. Services

#### `IEmbeddingService` & `EmbeddingService`
- **Location**: `Services/EmbeddingService.cs`
- **Chức năng**:
  - Tính embedding vector từ Gemini API cho text
  - Tính cosine similarity giữa 2 embedding vectors
  - Tìm người dùng phù hợp cho startup dựa trên weighted similarity

### 2. Trọng số (Weights)

Hệ thống sử dụng trọng số cho từng tiêu chí (tổng = 100%):

```csharp
WEIGHT_SKILLS = 0.35       // 35% - Kỹ năng của người dùng
WEIGHT_ROLES = 0.30        // 30% - Vai trò trong startup
WEIGHT_CATEGORIES = 0.25   // 25% - Lĩnh vực quan tâm
WEIGHT_TEAM = 0.10         // 10% - Team description
```

### 3. Embeddings trong Database

#### Bảng `StartUp`:
- `TeamEmbedding`: Embedding của mô tả team
- `CategoryEmbedding`: Embedding của category startup

#### Bảng `AppUser`:
- `SkillsEmbadding`: Embedding của kỹ năng
- `RolesEmbadding`: Embedding của vai trò
- `CategoriesEmbadding`: Embedding của lĩnh vực quan tâm

## API Endpoints

### 1. Tạo Startup với Embedding
**POST** `/api/startups`

Khi tạo startup, hệ thống tự động:
- Tính embedding cho `Team` description
- Tính embedding cho `Category` của startup
- Lưu vào database

**Request Body**:
```json
{
  "team": "Đội ngũ 3 founder với 10 năm kinh nghiệm trong AI và blockchain",
  "idea": "Nền tảng kết nối startup với nhà đầu tư",
  "prototype": "MVP đã hoàn thành",
  "plan": "Launch Q1 2026",
  "relationship": "Có quan hệ với VietStart",
  "privacy": 0,
  "point": 85,
  "ideaPoint": 20,
  "teamPoint": 18,
  "prototypePoint": 15,
  "planPoint": 17,
  "relationshipPoint": 15,
  "categoryId": 1
}
```

### 2. Gợi Ý Người Dùng Phù Hợp
**GET** `/api/startups/{id}/suggest-users`

**Authorization**: Bearer Token (Admin, Client)

**Response**:
```json
{
  "startupId": 1,
  "startupName": "Nền tảng kết nối startup với nhà đầu tư",
  "totalSuggestions": 15,
  "suggestions": [
    {
      "userId": "abc-123",
      "fullName": "Nguyễn Văn A",
      "email": "user@example.com",
      "avatar": "https://...",
      "bio": "Founder & CEO với 10 năm kinh nghiệm",
      "location": "Hà Nội",
      "skills": "AI, Blockchain, Product Management",
      "rolesInStartup": "CEO, CTO",
      "matchScore": 87.5,
      "matchDetails": {
        "Skills": 92.3,
        "Roles": 85.7,
        "Categories": 88.1,
        "Team": 84.5
      }
    }
  ]
}
```

**Giải thích MatchScore**:
- `matchScore`: Điểm tổng hợp (0-100%) dựa trên trọng số
- `matchDetails`: Điểm chi tiết cho từng tiêu chí
- Chỉ hiển thị người dùng có điểm >= 30%
- Sắp xếp theo điểm từ cao xuống thấp
- Tối đa 20 gợi ý

### 3. Cập Nhật User Profile với Embedding
**PUT** `/api/users/{id}`

Khi user cập nhật profile, hệ thống tự động tính embedding cho:
- `Skills`
- `RolesInStartup`
- `CategoryInvests`

**Note**: Embedding được tính bất đồng bộ để không làm chậm response.

## Công thức Tính Toán

### 1. Cosine Similarity

```
similarity = (A · B) / (||A|| × ||B||)
```

Trong đó:
- `A · B`: Tích vô hướng (dot product)
- `||A||`, `||B||`: Độ dài vector (magnitude)

### 2. Weighted Score

```
totalScore = (skillsSimilarity × 0.35) + 
             (rolesSimilarity × 0.30) + 
             (categorySimilarity × 0.25) + 
             (teamSimilarity × 0.10)
```

## Luồng Hoạt Động

### Khi tạo Startup:
1. User gửi POST request với thông tin startup
2. Controller gọi `EmbeddingService.GetEmbeddingAsync()` để tính:
   - TeamEmbedding từ `Team` description
   - CategoryEmbedding từ `Category.Name + Description`
3. Lưu startup vào database với embeddings

### Khi gợi ý người dùng:
1. Lấy startup từ database (bao gồm embeddings)
2. Nếu chưa có embedding, tính ngay lập tức
3. Gọi `EmbeddingService.GetSuggestedUsersForStartupAsync()`
4. Với mỗi user:
   - Tính similarity cho 4 tiêu chí
   - Tính weighted score
   - Lọc những người >= 30%
5. Sắp xếp và trả về top 20

### Khi cập nhật User:
1. User gửi PUT request
2. Cập nhật thông tin profile
3. Background task tính embeddings cho Skills, Roles, Categories
4. Lưu embeddings vào database

## Configuration

### appsettings.json
```json
{
  "Gemini": {
    "Key": "YOUR_GEMINI_API_KEY"
  }
}
```

### Dependency Injection (Program.cs)
```csharp
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddHttpClient();
```

## Error Handling

- Nếu Gemini API lỗi, trả về empty embedding `"[]"`
- Nếu không tính được similarity, trả về `0.0`
- Nếu không có user nào phù hợp, trả về danh sách rỗng
- Log tất cả errors ra console

## Performance

### Optimizations:
- Embeddings được cache trong database
- Chỉ tính lại nếu chưa có
- User embedding tính bất đồng bộ
- Giới hạn 20 kết quả
- Filter điểm >= 30% trước khi sort

### API Limits:
- Gemini API có rate limit
- Nên implement retry logic nếu cần
- Cân nhắc caching cho production

## Testing

### Test Scenarios:

1. **Tạo Startup**:
   - Verify embeddings được tính
   - Check embeddings saved to DB

2. **Gợi Ý User**:
   - Test với startup có/không có embeddings
   - Verify score calculation
   - Check filtering và sorting

3. **Update User**:
   - Test embedding calculation
   - Verify background task

## Future Enhancements

1. **Caching**: Redis cache cho embeddings thường dùng
2. **Batch Processing**: Tính nhiều embeddings cùng lúc
3. **Real-time Updates**: WebSocket để notify khi có match mới
4. **Advanced Matching**: Thêm ML model để improve matching
5. **Analytics**: Track match success rate
