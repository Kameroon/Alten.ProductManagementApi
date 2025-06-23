
-- Create the new database
CREATE DATABASE product_db
   WITH
    OWNER = postgres      
    ENCODING = 'UTF8'
    LC_COLLATE = 'fr_FR.UTF-8' 
    LC_CTYPE = 'fr_FR.UTF-8'  
    TEMPLATE = template0       
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;


-- Create Users table
CREATE TABLE IF NOT EXISTS Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(255) NOT NULL,
    Firstname VARCHAR(255),
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
	
);

ALTER TABLE Users
ADD COLUMN IsActive BOOLEAN;

Alter table users add createdat bigint 

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


-----------------------------------------------------------------------------------------------------------
---------                     Insert sample products and cart items for testing                 -----------
-----------------------------------------------------------------------------------------------------------
INSERT INTO products (
    code,
    name,
    description,
    image,
    category,
    price,
    quantity,
    internalreference,
    shellid,
    inventorystatus,
    rating,
    createdat,
    updatedat
) VALUES (
    'PROD001',
    'Clavier Mécanique RGB',
    'Clavier gaming durable avec rétroéclairage RGB personnalisable et interrupteurs tactiles.',
    'http://example.com/images/keyboard_rgb.jpg',
    'Informatique',
    129.99,
    50,
    'REF-KBD-2024-001',
    101,
    'INSTOCK',
    4.5,
    1718712000, -- Unix timestamp
    1718712000
);

--------------------------------------------------------------------------------------------------------

INSERT INTO cartitems (
    userid,
    productid,
    quantity
) VALUES (
    1, -- Remplace par un UserId existant
    1, -- Remplace par un ProductId existant
    2
);

-- L'utilisateur 1 ajoute 1 souris au panier
INSERT INTO cartitems (
    userid,
    productid,
    quantity
) VALUES (
    2, -- Remplace par le même UserId
    2, -- Remplace par un ProductId existant
    1
);


--------------------------------------------------------------------------------------------------------
-- L'utilisateur 1 ajoute le casque audio à sa liste de souhaits
INSERT INTO wishlistitems (
    userid,
    productid
) VALUES (
    1, -- Remplace par un UserId existant
    3  -- Remplace par un ProductId existant
);

-- L'utilisateur 2 ajoute le clavier mécanique à sa liste de souhaits
INSERT INTO wishlistitems (
    userid,
    productid
) VALUES (
    2, -- Remplace par un UserId existant
    1  -- Remplace par un ProductId existant
);
