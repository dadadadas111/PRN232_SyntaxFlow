# SyntaxFlow Project Tasks

## Real-Time Comment System

### Overview
Implement a real-time comment system for blocks where users can comment under blocks with live updates via MQTT messaging.

### Technical Requirements

#### 1. Database Schema
- [ ] Create `Comment` model/entity
  - `Id` (int, primary key)
  - `BlockId` (int, foreign key to Block)
  - `UserId` (string, foreign key to User)
  - `Content` (string, comment text)
  - `CreatedAt` (DateTime)
  - `UpdatedAt` (DateTime)
  - `IsDeleted` (bool, for soft deletion)

- [ ] Update `ApplicationDbContext`
  - Add `DbSet<Comment> Comments`
  - Configure relationships and constraints
  - Create migration for Comment table

#### 2. API Development
- [ ] Create `CommentController`
  - `GET /api/comments/{blockId}` - Get comments for a block
  - `POST /api/comments` - Create new comment
  - `PUT /api/comments/{id}` - Update comment (optional)
  - `DELETE /api/comments/{id}` - Delete comment (optional)

- [ ] Create `CommentService` and `ICommentService`
  - `GetCommentsByBlockIdAsync(int blockId)`
  - `CreateCommentAsync(CommentCreateDto dto, string userId)`
  - `UpdateCommentAsync(int id, string content, string userId)` (optional)
  - `DeleteCommentAsync(int id, string userId)` (optional)

- [ ] Create DTOs
  - `CommentDto` - for returning comment data
  - `CommentCreateDto` - for creating comments
  - `CommentUpdateDto` - for updating comments (optional)

#### 3. MQTT Integration
- [ ] Install MQTT packages
  - `MQTTnet` or `MQTTnet.AspNetCore`
  - Configure MQTT client/broker settings

- [ ] Create `MqttService` and `IMqttService`
  - `PublishCommentAsync(CommentDto comment)`
  - Connection management
  - Topic structure: `syntaxflow/comments/{blockId}`

- [ ] Update `CommentService`
  - Publish to MQTT after saving comment to database
  - Handle MQTT publishing errors gracefully

#### 4. Frontend Development
- [ ] Create comment UI components
  - Comment list display
  - Comment input form
  - Real-time comment updates
  - User authentication check

- [ ] JavaScript MQTT client
  - Install MQTT client library (e.g., `mqtt.js` via CDN)
  - Subscribe to comment topics
  - Handle incoming comment messages
  - Update DOM in real-time

- [ ] Update block detail views
  - Add comment section to `BlockDetail.cshtml`
  - Add comment section to `SharedBlocks.cshtml` (optional)
  - Style comment components with Bootstrap

#### 5. Security & Validation
- [ ] Authentication requirements
  - Only authenticated users can post comments
  - Users can only edit/delete their own comments

- [ ] Input validation
  - Comment content length limits
  - XSS protection
  - SQL injection prevention

- [ ] Rate limiting
  - Prevent comment spam
  - MQTT message throttling

#### 6. Configuration
- [ ] Update `appsettings.json`
  - MQTT broker configuration
  - Comment system settings
  - Rate limiting settings

- [ ] Environment variables
  - MQTT credentials
  - Connection strings

### Implementation Steps

#### Phase 1: Backend Foundation
1. Create Comment model and database migration
2. Implement CommentService with basic CRUD operations
3. Create CommentController with API endpoints
4. Add MQTT service integration

#### Phase 2: Frontend Integration
1. Create comment UI components
2. Implement JavaScript MQTT client
3. Add comment sections to block views
4. Style and responsive design

#### Phase 3: Real-time Features
1. MQTT message publishing on comment creation
2. Real-time comment updates via MQTT subscription
3. Handle connection failures and reconnection
4. Optimize performance for multiple concurrent users

#### Phase 4: Polish & Security
1. Add authentication checks
2. Implement rate limiting
3. Add input validation and sanitization
4. Error handling and user feedback
5. Testing and debugging

### Technical Considerations
- **MQTT Broker**: Consider using a cloud MQTT broker (e.g., HiveMQ, AWS IoT, Azure IoT Hub) or local broker
- **Scalability**: Design for multiple concurrent users and high comment volume
- **Performance**: Optimize database queries and MQTT message handling
- **Fallback**: Ensure graceful degradation if MQTT is unavailable
- **Security**: Implement proper authentication and authorization for MQTT topics

### Dependencies
- MQTTnet package for .NET
- MQTT client library for JavaScript (mqtt.js)
- Entity Framework Core for database operations
- SignalR (alternative to MQTT if preferred)

### Estimated Timeline
- Phase 1: 2-3 days
- Phase 2: 2-3 days  
- Phase 3: 1-2 days
- Phase 4: 1-2 days
- **Total**: 6-10 days

---

## Completed Tasks
✅ Fork system implementation  
✅ Fork information display  
✅ Color scheme improvements  
✅ Foreign key constraint resolution for block deletion
