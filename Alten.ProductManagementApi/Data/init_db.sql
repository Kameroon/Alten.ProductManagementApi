-- Create Users table
CREATE TABLE IF NOT EXISTS Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(255) NOT NULL,
    Firstname VARCHAR(255),
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL
);

-- Create Products table
CREATE TABLE IF NOT EXISTS Products (
    Id SERIAL PRIMARY KEY,
    Code VARCHAR(255) UNIQUE NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    Image VARCHAR(255),
    Category VARCHAR(255),
    Price DECIMAL(10, 2) NOT NULL,
    Quantity INT NOT NULL,
    InternalReference VARCHAR(255),
    ShellId INT,
    InventoryStatus VARCHAR(50), -- e.g., 'INSTOCK', 'LOWSTOCK', 'OUTOFSTOCK'
    Rating DECIMAL(2, 1),
    CreatedAt BIGINT NOT NULL,
    UpdatedAt BIGINT NOT NULL
);

-- Create CartItems table
CREATE TABLE IF NOT EXISTS CartItems (
    Id SERIAL PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    AddedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_CartItems_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    UNIQUE (UserId, ProductId) -- Ensures a user can only have one of each product in their cart
);

-- Create WishlistItems table
CREATE TABLE IF NOT EXISTS WishlistItems (
    Id SERIAL PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    AddedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_WishlistItems_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_WishlistItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    UNIQUE (UserId, ProductId) -- Ensures a user can only have one of each product in their wishlist
);

-- Optional: Insert a default admin user for testing
-- Replace 'your_hashed_admin_password_here' with an actual hashed password (e.g., using a tool or by running the /account endpoint once and copying the hash)
INSERT INTO Users (Username, Firstname, Email, PasswordHash)
VALUES ('admin', 'Admin', 'admin@admin.com', '$2a$11$E.d9c7B8h6F5e4D3g2a1O.j9x7y6z5w4v3u2t1s0r9q8p7o6n5m4l3k2j1i0') -- Example hash, generate your own!
ON CONFLICT (Email) DO NOTHING; -- Prevents re-insertion if user already exists