-- ============================================================
--  FIS Database Schema
--  We Build Stuff — Financial Information System
--  MySQL 8.x
--  Run this once to create all tables in your fis_db schema
-- ============================================================

CREATE DATABASE IF NOT EXISTS fis_db CHARACTER SET utf8 COLLATE utf8_general_ci;
USE fis_db;

-- ── Users ────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Users (
    UserId          INT             AUTO_INCREMENT PRIMARY KEY,
    Username        VARCHAR(50)     NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255)    NOT NULL,
    FullName        VARCHAR(100)    NOT NULL,
    Email           VARCHAR(100)    NOT NULL,
    Role            VARCHAR(20)     NOT NULL DEFAULT 'Clerk',   -- Admin | Manager | Clerk | Employee
    IsActive        TINYINT(1)      NOT NULL DEFAULT 1,
    CreatedAt       DATETIME        NOT NULL DEFAULT NOW(),
    LastLoginAt     DATETIME        NULL,
    EmployeeId      INT             NULL                        -- FK added after Employees table
);

-- ── Vendors ──────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Vendors (
    VendorId            INT             AUTO_INCREMENT PRIMARY KEY,
    VendorName          VARCHAR(100)    NOT NULL,
    ContactName         VARCHAR(100)    NULL,
    Phone               VARCHAR(20)     NULL,
    Email               VARCHAR(100)    NULL,
    Address             VARCHAR(255)    NULL,
    RelationshipStatus  VARCHAR(20)     NOT NULL DEFAULT 'Active',  -- Active | Suspended | Terminated
    CreatedAt           DATETIME        NOT NULL DEFAULT NOW()
);

-- ── Raw Materials ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS RawMaterials (
    RawMaterialId       INT             AUTO_INCREMENT PRIMARY KEY,
    MaterialName        VARCHAR(100)    NOT NULL,
    UnitOfMeasure       VARCHAR(20)     NOT NULL,
    QuantityOnHand      DECIMAL(10,2)   NOT NULL DEFAULT 0,      -- Written by POPS; read by FIS
    ReorderThreshold    DECIMAL(10,2)   NOT NULL DEFAULT 0,      -- Auto-reorder fires when below this
    ReorderQuantity     DECIMAL(10,2)   NOT NULL DEFAULT 0,
    VendorId            INT             NOT NULL,                -- Preferred vendor FK
    UnitPrice           DECIMAL(10,2)   NOT NULL DEFAULT 0,
    UpdatedAt           DATETIME        NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_rawmat_vendor FOREIGN KEY (VendorId) REFERENCES Vendors(VendorId)
);

-- ── Purchase Orders ───────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS PurchaseOrders (
    PurchaseOrderId         INT             AUTO_INCREMENT PRIMARY KEY,
    VendorId                INT             NOT NULL,
    OrderDate               DATETIME        NOT NULL DEFAULT NOW(),
    ExpectedDeliveryDate    DATETIME        NULL,
    ActualDeliveryDate      DATETIME        NULL,
    TotalAmount             DECIMAL(12,2)   NOT NULL DEFAULT 0,
    Status                  VARCHAR(20)     NOT NULL DEFAULT 'Pending',  -- Pending | Delivered | Cancelled
    CONSTRAINT fk_po_vendor FOREIGN KEY (VendorId) REFERENCES Vendors(VendorId)
);

-- ── Purchase Order Items (line items) ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS PurchaseOrderItems (
    PurchaseOrderItemId INT             AUTO_INCREMENT PRIMARY KEY,
    PurchaseOrderId     INT             NOT NULL,
    RawMaterialId       INT             NOT NULL,
    QuantityOrdered     DECIMAL(10,2)   NOT NULL,
    UnitPrice           DECIMAL(10,2)   NOT NULL,
    LineTotal           DECIMAL(12,2)   NOT NULL,
    CONSTRAINT fk_poitem_po      FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrders(PurchaseOrderId),
    CONSTRAINT fk_poitem_rawmat  FOREIGN KEY (RawMaterialId)   REFERENCES RawMaterials(RawMaterialId)
);

-- ── Accounts Payable — Vendor Invoices ────────────────────────────────────────
CREATE TABLE IF NOT EXISTS InvoicesAP (
    InvoiceAPId         INT             AUTO_INCREMENT PRIMARY KEY,
    VendorId            INT             NOT NULL,
    PurchaseOrderId     INT             NULL,                    -- Nullable: invoice may arrive before PO match
    InvoiceNumber       VARCHAR(50)     NOT NULL,               -- Vendor's own reference
    InvoiceDate         DATETIME        NOT NULL,
    DueDate             DATETIME        NOT NULL,
    TotalAmount         DECIMAL(12,2)   NOT NULL,
    AmountPaid          DECIMAL(12,2)   NOT NULL DEFAULT 0,
    DatePaid            DATETIME        NULL,
    Status              VARCHAR(20)     NOT NULL DEFAULT 'Unpaid',  -- Unpaid | Paid | Overdue
    ReceivedAt          DATETIME        NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_invoiceap_vendor  FOREIGN KEY (VendorId)        REFERENCES Vendors(VendorId),
    CONSTRAINT fk_invoiceap_po      FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrders(PurchaseOrderId)
);

-- ── Accounts Payable — Vendor Payments ───────────────────────────────────────
CREATE TABLE IF NOT EXISTS PaymentsAP (
    PaymentAPId         INT             AUTO_INCREMENT PRIMARY KEY,
    InvoiceAPId         INT             NOT NULL,
    AmountPaid          DECIMAL(12,2)   NOT NULL,
    DatePaid            DATETIME        NOT NULL DEFAULT NOW(),
    PaymentMethod       VARCHAR(30)     NOT NULL,               -- Check | ElectronicTransfer | ACH
    ReferenceNumber     VARCHAR(100)    NULL,                   -- Check number or bank confirmation
    CONSTRAINT fk_paymentap_invoice FOREIGN KEY (InvoiceAPId) REFERENCES InvoicesAP(InvoiceAPId)
);

-- ── Customers ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Customers (
    CustomerId      INT             AUTO_INCREMENT PRIMARY KEY,
    CustomerName    VARCHAR(100)    NOT NULL,
    BillingAddress  VARCHAR(255)    NULL,
    Phone           VARCHAR(20)     NULL,
    Email           VARCHAR(100)    NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT NOW()
);

-- ── Customer Orders (written by POPS; read by FIS) ───────────────────────────
CREATE TABLE IF NOT EXISTS CustomerOrders (
    CustomerOrderId     INT             AUTO_INCREMENT PRIMARY KEY,
    CustomerId          INT             NOT NULL,
    ProductDescription  VARCHAR(255)    NOT NULL,
    Quantity            INT             NOT NULL,
    TotalAmount         DECIMAL(12,2)   NOT NULL,
    OrderDate           DATETIME        NOT NULL,
    ShipDate            DATETIME        NULL,                   -- Set by POPS warehouse; triggers FIS billing
    Status              VARCHAR(20)     NOT NULL DEFAULT 'Open',-- Open | Shipped | Closed | Cancelled
    CreatedAt           DATETIME        NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_custorder_customer FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
);

-- ── Accounts Receivable — Customer Bills ─────────────────────────────────────
CREATE TABLE IF NOT EXISTS CustomerBills (
    CustomerBillId      INT             AUTO_INCREMENT PRIMARY KEY,
    CustomerOrderId     INT             NOT NULL,
    CustomerId          INT             NOT NULL,
    BillDate            DATETIME        NOT NULL DEFAULT NOW(),
    DueDate             DATETIME        NOT NULL,
    TotalAmountDue      DECIMAL(12,2)   NOT NULL,
    BalanceRemaining    DECIMAL(12,2)   NOT NULL,               -- Reduced by each PaymentAR
    Status              VARCHAR(20)     NOT NULL DEFAULT 'Unpaid',-- Unpaid | PartiallyPaid | Paid | Overdue
    CONSTRAINT fk_custbill_order    FOREIGN KEY (CustomerOrderId) REFERENCES CustomerOrders(CustomerOrderId),
    CONSTRAINT fk_custbill_customer FOREIGN KEY (CustomerId)      REFERENCES Customers(CustomerId)
);

-- ── Accounts Receivable — Customer Payments ──────────────────────────────────
CREATE TABLE IF NOT EXISTS PaymentsAR (
    PaymentARId         INT             AUTO_INCREMENT PRIMARY KEY,
    CustomerBillId      INT             NOT NULL,
    AmountReceived      DECIMAL(12,2)   NOT NULL,
    DateReceived        DATETIME        NOT NULL DEFAULT NOW(),  -- Case: partial payment recorded WITH date
    PaymentMethod       VARCHAR(30)     NOT NULL,               -- Check | ElectronicTransfer | CreditCard
    ReferenceNumber     VARCHAR(100)    NULL,
    CONSTRAINT fk_paymentar_bill FOREIGN KEY (CustomerBillId) REFERENCES CustomerBills(CustomerBillId)
);

-- ── Employees ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Employees (
    EmployeeId      INT             AUTO_INCREMENT PRIMARY KEY,
    FullName        VARCHAR(100)    NOT NULL,
    Email           VARCHAR(100)    NOT NULL,
    PaymentMethod   VARCHAR(20)     NOT NULL DEFAULT 'Check',   -- Check | DirectDeposit
    BankName        VARCHAR(100)    NULL,                       -- Only when PaymentMethod = DirectDeposit
    AccountNumber   VARCHAR(50)     NULL,
    RoutingNumber   VARCHAR(20)     NULL,
    UpdatedAt       DATETIME        NOT NULL DEFAULT NOW()
);

-- ── Payroll Records (prepared by HRS; read and processed by FIS) ─────────────
CREATE TABLE IF NOT EXISTS PayrollRecords (
    PayrollRecordId         INT             AUTO_INCREMENT PRIMARY KEY,
    EmployeeId              INT             NOT NULL,
    PayPeriodStart          DATETIME        NOT NULL,
    PayPeriodEnd            DATETIME        NOT NULL,
    HoursWorked             DECIMAL(6,2)    NOT NULL,
    GrossPay                DECIMAL(12,2)   NOT NULL,
    Deductions              DECIMAL(12,2)   NOT NULL DEFAULT 0,
    NetPay                  DECIMAL(12,2)   NOT NULL,
    Status                  VARCHAR(20)     NOT NULL DEFAULT 'Pending', -- Pending | Processed | Failed
    ProcessedAt             DATETIME        NULL,
    PaymentMethod           VARCHAR(20)     NULL,               -- Captured at disbursement time
    ConfirmationReference   VARCHAR(100)    NULL,               -- Check number or bank ref
    CONSTRAINT fk_payroll_employee FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId)
);

-- ── Add FK from Users → Employees (deferred — Employees table now exists) ────
ALTER TABLE Users
    ADD CONSTRAINT fk_user_employee
    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId);

