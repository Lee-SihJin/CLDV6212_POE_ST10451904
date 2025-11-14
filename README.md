# CLDV6212_POE_ST10451904

# ABC Retailers - E-commerce Platform

A comprehensive ASP.NET Core e-commerce solution built with Azure cloud services, featuring a modern retail management system with customer shopping cart, order processing, and administrative capabilities.

## 🚀 Features

### Customer Features
- **User Registration & Authentication** - Secure account creation and login
- **Product Catalog** - Browse and search products with detailed information
- **Shopping Cart** - Add, update, and manage cart items
- **Order Management** - Place orders and track order history
- **Customer Profile** - View and update personal information and shipping details
- **Secure Checkout** - Complete orders with shipping information

### Admin Features
- **Product Management** - CRUD operations for products with image upload
- **Customer Management** - View and manage customer accounts
- **Order Management** - Process and update order status
- **Inventory Management** - Track stock levels and prevent overselling
- **Dashboard** - Overview of business metrics and statistics

## 🛠️ Technology Stack

### Backend
- **ASP.NET Core 6.0** - Web application framework
- **Entity Framework Core** - ORM for data access
- **Azure Functions** - Serverless backend services
- **Dapper** - High-performance micro-ORM

### Frontend
- **Razor Pages** - Server-side rendering
- **Bootstrap 5** - Responsive UI framework
- **jQuery** - Client-side interactions
- **Font Awesome** - Icons and visuals

### Azure Services
- **Azure SQL Database** - Relational data storage
- **Azure Table Storage** - NoSQL product catalog
- **Azure Blob Storage** - File and image storage
- **Azure Queue Storage** - Message processing
- **Azure File Shares** - Document management
- **Application Insights** - Monitoring and analytics

### Authentication & Authorization
- **Cookie-based Authentication** - Secure user sessions
- **Role-based Access Control** - Admin, Manager, and Customer roles
- **Claims-based Identity** - User information management

## 📋 Prerequisites

Before running this application, ensure you have:

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) (for local development)

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/your-username/abc-retailers.git
cd abc-retailers
```

### 2. Configure Azure Services

#### Azure Storage Configuration
Update `appsettings.json` with your Azure Storage connection string:
```json
{
  "ConnectionStrings": {
    "AzureStorage": "Your_Azure_Storage_Connection_String",
    "DefaultConnection": "Your_SQL_Server_Connection_String"
  }
}
```

#### Local Settings (for Azure Functions)
Update `local.settings.json`:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

### 3. Database Setup

#### SQL Database
Execute the following SQL script to create the required tables:

```sql
-- Users table
CREATE TABLE Users (
    UserId UNIQUEIDENTIFIER PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'Customer',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginDate DATETIME2 NULL
);

-- UserProfiles table
CREATE TABLE UserProfiles (
    ProfileId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    PhoneNumber NVARCHAR(20) NULL,
    ShippingAddress NVARCHAR(500) NULL,
    DateOfBirth DATE NULL,
    ProfilePictureUrl NVARCHAR(500) NULL
);

-- ShoppingCart table
CREATE TABLE ShoppingCart (
    CartId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- CartItems table
CREATE TABLE CartItems (
    CartItemId UNIQUEIDENTIFIER PRIMARY KEY,
    CartId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES ShoppingCart(CartId),
    ProductId NVARCHAR(100) NOT NULL,
    ProductName NVARCHAR(255) NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL,
    AddedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Orders table
CREATE TABLE Orders (
    OrderId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Submitted',
    ShippingAddress NVARCHAR(500) NOT NULL,
    PaymentStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    OrderDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    TableStorageOrderId NVARCHAR(100) NULL
);

-- OrderItems table
CREATE TABLE OrderItems (
    OrderItemId UNIQUEIDENTIFIER PRIMARY KEY,
    OrderId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Orders(OrderId),
    ProductId NVARCHAR(100) NOT NULL,
    ProductName NVARCHAR(255) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL
);
```

### 4. Run the Application

#### Start the Web Application
```bash
dotnet run --project ABCRetailers
```

#### Start Azure Functions (separate terminal)
```bash
cd ABCRetailers.Functions
func start
```

The application will be available at:
- **Web Application**: https://localhost:7001
- **Azure Functions**: http://localhost:7167

## 👥 Default Users

After setting up the database, you can create default users:

### Admin User
- **Username**: admin
- **Password**: Admin123!
- **Role**: Admin

### Customer User
- **Username**: john.customer
- **Password**: Customer123!
- **Role**: Customer

## 📁 Project Structure

```
ABCRetailers/
├── Controllers/                 # MVC Controllers
│   ├── HomeController.cs
│   ├── AuthController.cs
│   ├── CustomerAccountController.cs
│   ├── CartController.cs
│   ├── StoreController.cs
│   ├── ProductController.cs
│   ├── OrderController.cs
│   └── CustomerController.cs
├── Models/                     # Data Models and ViewModels
│   ├── Entities/              # Database entities
│   ├── ViewModels/            # View models
│   └── ShoppingCartModels.cs
├── Services/                  # Business logic services
│   ├── IAuthService.cs
│   ├── IShoppingCartService.cs
│   ├── IFunctionsApi.cs
│   └── StorageInitializationService.cs
├── Views/                     # Razor views
├── wwwroot/                  # Static files
└── Program.cs               # Application entry point

ABCRetailers.Functions/
├── TableStorageFunctions.cs
├── BlobStorageFunctions.cs
├── QueueStorageFunctions.cs
├── FileShareFunctions.cs
└── StorageInitializationFunction.cs
```

## 🔧 Configuration

### Application Settings
Key configuration options in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey",
    "DefaultConnection": "Server=yourserver;Database=ABCRetailerSQLDB;..."
  },
  "PasswordSalt": "your-secret-salt-key",
  "AzureStorage": {
    "AutoInitialize": true
  }
}
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT` - Development/Production
- `AzureWebJobsStorage` - Azure Functions storage connection

## 🧪 Testing

### Running Tests
```bash
dotnet test
```

### Manual Testing Scenarios
1. **User Registration & Login**
2. **Product Browsing & Search**
3. **Shopping Cart Operations**
4. **Order Placement & Tracking**
5. **Admin Product Management**
6. **File Upload Operations**

## 📊 API Endpoints

### Azure Functions API
- `GET /api/table/{tableName}` - Get all entities
- `POST /api/table/{tableName}` - Add entity
- `PUT /api/table/{tableName}` - Update entity
- `DELETE /api/table/{tableName}/{partitionKey}/{rowKey}` - Delete entity
- `POST /api/blob/{containerName}` - Upload blob
- `POST /api/queue/{queueName}` - Send message

### Web Application Controllers
- `/Auth/Login` - User authentication
- `/Store` - Product catalog
- `/Cart` - Shopping cart management
- `/CustomerAccount` - Customer profile management
- `/Product` - Product management (Admin)
- `/Order` - Order management (Admin)

## 🚀 Deployment

### Azure App Service Deployment
```bash
# Publish web application
dotnet publish --configuration Release --output ./publish
az webapp up --name abc-retailers-app --resource-group your-resource-group
```

### Azure Functions Deployment
```bash
# Publish functions
func azure functionapp publish abc-retailers-functions
```

### Database Deployment
Use Azure SQL Database or deploy to your preferred SQL server.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request




---

**ABC Retailers** - Modern E-commerce Solution built with .NET and Azure Cloud Services.
