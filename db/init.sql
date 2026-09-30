-- 訂便當系統 (Bento Ordering System) PostgreSQL 初始資料庫腳本 (含角色權限與稽核日誌)

-- 1. 建立資料表

-- 角色與側邊欄權限資料表 (Roles)
CREATE TABLE IF NOT EXISTS "Roles" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(255),
    "AllowedMenus" TEXT NOT NULL, -- JSON 陣列字串: '["/", "/orders", "/stores", "/users", "/roles", "/logs"]'
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 使用者資料表 (Users)
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(50) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "FullName" VARCHAR(100) NOT NULL,
    "Role" VARCHAR(20) NOT NULL DEFAULT 'User', -- 'Admin' 或 'User' 或角色名稱
    "RoleId" INT REFERENCES "Roles"("Id"),
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 店家資料表 (Stores)
CREATE TABLE IF NOT EXISTS "Stores" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Phone" VARCHAR(30),
    "Address" VARCHAR(255),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 菜單項目資料表 (MenuItems)
CREATE TABLE IF NOT EXISTS "MenuItems" (
    "Id" SERIAL PRIMARY KEY,
    "StoreId" INT NOT NULL REFERENCES "Stores"("Id") ON DELETE CASCADE,
    "Name" VARCHAR(100) NOT NULL,
    "Description" TEXT,
    "Price" DECIMAL(10, 2) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 點餐團購場次資料表 (OrderSessions)
CREATE TABLE IF NOT EXISTS "OrderSessions" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(100) NOT NULL,
    "StoreId" INT NOT NULL REFERENCES "Stores"("Id"),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Open', -- 'Open', 'Closed', 'Canceled'
    "Deadline" TIMESTAMP WITH TIME ZONE,
    "CreatedByUserId" INT NOT NULL REFERENCES "Users"("Id"),
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 訂單明細資料表 (OrderItems)
CREATE TABLE IF NOT EXISTS "OrderItems" (
    "Id" SERIAL PRIMARY KEY,
    "OrderSessionId" INT NOT NULL REFERENCES "OrderSessions"("Id") ON DELETE CASCADE,
    "UserId" INT NOT NULL REFERENCES "Users"("Id"),
    "MenuItemId" INT NOT NULL REFERENCES "MenuItems"("Id"),
    "Quantity" INT NOT NULL DEFAULT 1,
    "UnitPrice" DECIMAL(10, 2) NOT NULL,
    "Note" VARCHAR(255),
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 操作紀錄稽核日誌 (AuditLogs)
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INT,
    "UserName" VARCHAR(100) NOT NULL,
    "Action" VARCHAR(100) NOT NULL,
    "Details" TEXT,
    "IpAddress" VARCHAR(50),
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. 寫入範例/初始資料 (Seed Data)

-- 預設角色
INSERT INTO "Roles" ("Id", "Name", "Description", "AllowedMenus") VALUES
(1, 'Admin', '系統最高管理員，擁有所有選單與操作權限', '["/", "/orders", "/stores", "/users", "/roles", "/logs"]'),
(2, 'User', '一般員工，可參與便當團購點餐與檢視個人明細', '["/", "/orders"]'),
(3, 'Manager', '便當團購專員，可發起團購與管理便當店家', '["/", "/orders", "/stores"]')
ON CONFLICT ("Id") DO NOTHING;

-- 預設使用者 (密碼 admin123 雜湊: $2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy)
INSERT INTO "Users" ("Username", "PasswordHash", "FullName", "Role", "RoleId") VALUES
('admin', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', '系統管理員', 'Admin', 1),
('user1', '$2a$11$45/JmvhNq.j900eB4GvGmuP.ZTh9a.Zl10zL26e8sB1F2H3I4J5K6', '張小明', 'User', 2),
('user2', '$2a$11$45/JmvhNq.j900eB4GvGmuP.ZTh9a.Zl10zL26e8sB1F2H3I4J5K6', '陳大華', 'User', 2)
ON CONFLICT ("Username") DO NOTHING;

-- 新增店家
INSERT INTO "Stores" ("Id", "Name", "Phone", "Address", "IsActive") VALUES
(1, '池上排骨飯', '02-2345-6789', '台北市中山區南京東路二段100號', TRUE),
(2, '正宗台南雞腿便當', '02-8765-4321', '台北市大安區信義路四段50號', TRUE)
ON CONFLICT ("Id") DO NOTHING;

-- 新增菜單項目
INSERT INTO "MenuItems" ("Id", "StoreId", "Name", "Description", "Price", "IsActive") VALUES
(1, 1, '招牌排骨飯', '香酥排骨搭配特製配菜', 110.00, TRUE),
(2, 1, '紅燒牛肉飯', '軟嫩牛肉佐濃郁紅燒醬汁', 130.00, TRUE),
(3, 1, '香草雞腿飯', '酥脆炸雞腿便當', 120.00, TRUE),
(4, 1, '素菜便當', '多款時令蔬菜與豆干', 90.00, TRUE),
(5, 2, '特級大雞腿飯', '超大外酥內嫩雞腿', 125.00, TRUE),
(6, 2, '滷排骨飯', '傳統古早味古法滷排骨', 105.00, TRUE),
(7, 2, '蒲燒鰻魚飯', '特調蒲燒醬香烤鰻魚', 160.00, TRUE)
ON CONFLICT ("Id") DO NOTHING;

-- 初始操作紀錄
INSERT INTO "AuditLogs" ("UserId", "UserName", "Action", "Details", "IpAddress") VALUES
(1, '系統管理員', '系統初始化', '建立初始資料庫與系統預設管理員帳號', '127.0.0.1');

-- 重設 Sequences
SELECT setval(pg_get_serial_sequence('"Roles"', 'Id'), coalesce(max("Id"), 1)) FROM "Roles";
SELECT setval(pg_get_serial_sequence('"Users"', 'Id'), coalesce(max("Id"), 1)) FROM "Users";
SELECT setval(pg_get_serial_sequence('"Stores"', 'Id'), coalesce(max("Id"), 1)) FROM "Stores";
SELECT setval(pg_get_serial_sequence('"MenuItems"', 'Id'), coalesce(max("Id"), 1)) FROM "MenuItems";
SELECT setval(pg_get_serial_sequence('"AuditLogs"', 'Id'), coalesce(max("Id"), 1)) FROM "AuditLogs";
