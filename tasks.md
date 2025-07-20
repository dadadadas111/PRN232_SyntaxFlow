
## 🌟 **API Tasks for Share Block Features, Stars, and Forks**

### **Phase 1: Public Block Discovery & Sharing**

**Task 1.1: Public Blocks API Endpoints**
- Add `GET /api/blocks/public` - Get all public blocks with pagination, sorting, and filtering
- Add `GET /api/blocks/public/{id}` - Get public block details (accessible to all authenticated users)
- Add search parameters: `?search=keyword&tags=tag1,tag2&sortBy=stars|forks|created|updated&page=1&size=10`

**Task 1.2: Block Visibility Management**
- Update existing `PUT /api/blocks/{id}` to handle `IsPublic` property (done)
- Add validation: only block owner can change visibility (done)

### **Phase 2: Star System**

**Task 2.1: Star Database Models**
- Create `BlockStar` entity: `Id, BlockId, UserId, CreatedAt`
- Add unique constraint on `(BlockId, UserId)` - users can only star once per block
- Update database migration

**Task 2.2: Star API Endpoints**
- Add `POST /api/blocks/{id}/star` - Star a block (increment StarCount)
- Add `DELETE /api/blocks/{id}/star` - Unstar a block (decrement StarCount)
- Add `GET /api/blocks/starred` - Get user's starred blocks
- Add `GET /api/blocks/{id}/stars` - Get list of users who starred the block

**Task 2.3: Star Business Logic**
- Implement star/unstar service methods with transaction safety
- Update `StarCount` automatically when stars are added/removed
- Validate: users cannot star their own blocks
- Validate: users cannot star private blocks they don't own

### **Phase 3: Fork System**

**Task 3.1: Fork API Endpoints**
- Add `POST /api/blocks/{id}/fork` - Fork a block (creates new block with ForkedFromId set)
- Add `GET /api/blocks/{id}/forks` - Get all forks of a block
- Add `GET /api/blocks/forked` - Get blocks user has forked

**Task 3.2: Fork Business Logic**
- Copy block content, name (with "Fork of" prefix), and tags
- Set new owner, reset star/fork counts to 0
- Increment original block's `ForkCount`
- Validate: users can fork public blocks and their own blocks
- Handle fork chains (track original source)

### **Phase 4: Enhanced Block Management**

**Task 4.1: Advanced Block Queries**
- Add trending algorithm (recent stars + forks weighted by time)
- Add `GET /api/blocks/trending` endpoint
- Add block statistics in responses (total views, fork depth)

**Task 4.2: User Activity Tracking**
- Create `BlockView` entity for view tracking
- Add view count to block responses
- Track user activity for recommendations

---

## 🎨 **UI Tasks for Share Block Features, Stars, and Forks**

### **Phase 1: Public Block Discovery**

**Task 1.1: Shared Blocks Page Enhancement**
- Replace placeholder in SharedBlocks.cshtml with functional block browser
- Add search bar with keyword and tag filtering
- Add sorting options (Most Stars, Most Forks, Newest, Recently Updated)
- Implement infinite scroll or pagination
- Show block cards with: name, owner, star count, fork count, tags, preview

**Task 1.2: Block Discovery Features**
- Add "Public" tab to navigation if not present
- Add trending section on home page
- Add "Recently Shared" section
- Show public/private indicator on all block lists

### **Phase 2: Star System UI**

**Task 2.1: Star Button Implementation**
- Add star button to BlockDetail.cshtml (filled/unfilled star icon)
- Add star button to block cards in MyBlocks.cshtml and SharedBlocks.cshtml
- Show star count next to star button
- Implement star/unstar click handlers with API calls
- Add visual feedback (loading states, success animations)

**Task 2.2: Starred Blocks Management**
- Add "Starred Blocks" page (`/Home/StarredBlocks`)
- Add "Starred" tab to user navigation
- Show starred blocks in card layout similar to MyBlocks.cshtml
- Add unstar functionality from starred blocks page

**Task 2.3: Star Indicators & Counts**
- Update all block cards to show star status (starred by current user)
- Add star count to block metadata displays
- Show "You starred this" indicator where relevant

### **Phase 3: Fork System UI**

**Task 3.1: Fork Button Implementation**
- Add fork button to BlockDetail.cshtml for public blocks and user's own blocks
- Add fork button to block cards in SharedBlocks.cshtml
- Implement fork confirmation modal with name input (prefilled with "Fork of {original}")
- Handle fork API call and redirect to new forked block

**Task 3.2: Fork Relationship Display**
- Show "Forked from" information in block metadata
- Add link to original block in forked blocks
- Show fork count and "View Forks" link in original blocks
- Add fork tree/history view for complex fork chains

**Task 3.3: Forked Blocks Management**
- Add "Forked Blocks" section to MyBlocks.cshtml or separate tab
- Show blocks user has forked with links to originals
- Show forks of user's blocks with links to fork details

### **Phase 4: Enhanced User Experience**

**Task 4.1: Block Detail Page Enhancements**
- Add social proof section (X stars, Y forks, Z views)
- Add "Users who starred this" modal
- Add "Recent forks" section
- Add owner profile link and basic stats

**Task 4.2: User Profile & Social Features**
- Add user profile page showing: public blocks, total stars received, fork activity
- Add "Follow User" functionality (optional)
- Add user activity feed (starred, forked, created blocks)

**Task 4.3: Discoverability Features**
- Add "You might like" recommendations based on starred/forked blocks
- Add tag-based discovery ("More blocks like this")
- Add "Popular in [tag]" sections
- Implement block of the day/week feature

### **Phase 5: Mobile & Responsive Enhancements**

**Task 5.1: Mobile Optimization**
- Optimize star/fork buttons for touch
- Implement swipe gestures for quick actions
- Add mobile-friendly block browser with filters
- Optimize infinite scroll for mobile performance

**Task 5.2: PWA Features** (Optional)
- Add offline capability for starred blocks
- Add push notifications for stars/forks of user's blocks
- Add home screen installation prompt

---

## 🚀 **Implementation Priority**

**Week 1**: API Phase 1 & 2 (Public blocks + Star system)
**Week 2**: UI Phase 1 & 2 (Block discovery + Star UI)  
**Week 3**: API Phase 3 (Fork system)
**Week 4**: UI Phase 3 (Fork UI)
**Week 5**: Polish & Phase 4 enhancements

This comprehensive plan will transform your Blockly editor into a full social coding platform similar to GitHub for visual programming blocks!