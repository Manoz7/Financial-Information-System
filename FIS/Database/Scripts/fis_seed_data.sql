USE fis_db;

-- ── Vendors ─────────────────────────────
INSERT INTO Vendors (VendorName, ContactName, Phone, Email) VALUES
('ABC Supplies', 'John Doe', '1111111111', 'abc@vendor.com'),
('Global Materials', 'Jane Smith', '2222222222', 'global@vendor.com'),
('Prime Industrial', 'Mike Ross', '3333333333', 'prime@vendor.com'),
('BuildSource', 'Rachel Zane', '4444444444', 'build@vendor.com'),
('MegaSupply Co.', 'Harvey Specter', '5555555555', 'mega@vendor.com');

-- ── Raw Materials ───────────────────────
INSERT INTO RawMaterials (MaterialName, UnitOfMeasure, QuantityOnHand, ReorderThreshold, VendorId, UnitPrice) VALUES
('Nails', 'Pieces', 1000, 200, 1, 0.05),
('Wood Planks', 'Units', 500, 100, 2, 10.00),
('Screws', 'Pieces', 800, 150, 3, 0.03),
('Metal Brackets', 'Units', 300, 50, 4, 2.50),
('Glue', 'Liters', 100, 20, 5, 5.00);

-- ── Customers ───────────────────────────
INSERT INTO Customers (CustomerName, BillingAddress, Phone, Email) VALUES
('Acme Corp', '123 Main St', '5550000001', 'acme@mail.com'),
('BuildIt LLC', '456 Oak Ave', '5550000002', 'buildit@mail.com'),
('ConstructPro', '789 Pine Rd', '5550000003', 'construct@mail.com'),
('Urban Builders', '321 Maple St', '5550000004', 'urban@mail.com'),
('Skyline Inc', '654 Elm St', '5550000005', 'skyline@mail.com');

-- ── Employees ───────────────────────────
INSERT INTO Employees (FullName, Email, PaymentMethod) VALUES
('Alice Johnson', 'alice@company.com', 'Check'),
('Bob Brown', 'bob@company.com', 'DirectDeposit'),
('Charlie Davis', 'charlie@company.com', 'Check'),
('Diana Prince', 'diana@company.com', 'DirectDeposit'),
('Ethan Hunt', 'ethan@company.com', 'Check');

-- ── Purchase Orders ─────────────────────
INSERT INTO PurchaseOrders (VendorId, TotalAmount) VALUES
(1, 500.00),
(2, 1000.00),
(3, 300.00),
(4, 250.00),
(5, 150.00);

-- ── Purchase Order Items ────────────────
INSERT INTO PurchaseOrderItems (PurchaseOrderId, RawMaterialId, QuantityOrdered, UnitPrice, LineTotal) VALUES
(1, 1, 1000, 0.05, 50.00),
(2, 2, 100, 10.00, 1000.00),
(3, 3, 500, 0.03, 15.00),
(4, 4, 100, 2.50, 250.00),
(5, 5, 50, 5.00, 250.00);

-- ── Invoices AP ─────────────────────────
INSERT INTO InvoicesAP (VendorId, InvoiceNumber, InvoiceDate, DueDate, TotalAmount) VALUES
(1, 'INV-001', NOW(), DATE_ADD(NOW(), INTERVAL 30 DAY), 500.00),
(2, 'INV-002', NOW(), DATE_ADD(NOW(), INTERVAL 30 DAY), 1000.00),
(3, 'INV-003', NOW(), DATE_ADD(NOW(), INTERVAL 30 DAY), 300.00),
(4, 'INV-004', NOW(), DATE_ADD(NOW(), INTERVAL 30 DAY), 250.00),
(5, 'INV-005', NOW(), DATE_ADD(NOW(), INTERVAL 30 DAY), 150.00);

-- ── Payments AP ─────────────────────────
INSERT INTO PaymentsAP (InvoiceAPId, AmountPaid, PaymentMethod) VALUES
(1, 500.00, 'Check'),
(2, 500.00, 'ACH'),
(3, 300.00, 'ElectronicTransfer'),
(4, 100.00, 'Check'),
(5, 150.00, 'ACH');

-- ── Customer Orders ─────────────────────
INSERT INTO CustomerOrders (CustomerId, ProductDescription, Quantity, TotalAmount, OrderDate, Status) VALUES
(1, 'Wood Table', 2, 400.00, NOW(), 'Open'),
(2, 'Chair Set', 5, 250.00, NOW(), 'Open'),
(3, 'Cabinet', 1, 300.00, NOW(), 'Open'),
(4, 'Desk', 3, 600.00, NOW(), 'Open'),
(5, 'Bookshelf', 2, 200.00, NOW(), 'Open');

-- ── Customer Bills ──────────────────────
INSERT INTO CustomerBills (CustomerOrderId, CustomerId, DueDate, TotalAmountDue, BalanceRemaining) VALUES
(1, 1, DATE_ADD(NOW(), INTERVAL 15 DAY), 400.00, 400.00),
(2, 2, DATE_ADD(NOW(), INTERVAL 15 DAY), 250.00, 250.00),
(3, 3, DATE_ADD(NOW(), INTERVAL 15 DAY), 300.00, 300.00),
(4, 4, DATE_ADD(NOW(), INTERVAL 15 DAY), 600.00, 600.00),
(5, 5, DATE_ADD(NOW(), INTERVAL 15 DAY), 200.00, 200.00);

-- ── Payments AR ─────────────────────────
INSERT INTO PaymentsAR (CustomerBillId, AmountReceived, PaymentMethod) VALUES
(1, 200.00, 'CreditCard'),
(2, 100.00, 'Check'),
(3, 300.00, 'ElectronicTransfer'),
(4, 300.00, 'CreditCard'),
(5, 200.00, 'Check');

-- ── Payroll Records ─────────────────────
INSERT INTO PayrollRecords (EmployeeId, PayPeriodStart, PayPeriodEnd, HoursWorked, GrossPay, NetPay) VALUES
(1, NOW(), NOW(), 40, 1000.00, 900.00),
(2, NOW(), NOW(), 38, 950.00, 850.00),
(3, NOW(), NOW(), 42, 1100.00, 1000.00),
(4, NOW(), NOW(), 36, 900.00, 800.00),
(5, NOW(), NOW(), 40, 1000.00, 900.00);

-- ── Users ──────────────────────────────
INSERT INTO Users (Username, PasswordHash, FullName, Email, Role) VALUES
('admin', 'hash1', 'Admin User', 'admin@company.com', 'Admin'),
('manager', 'hash2', 'Manager User', 'manager@company.com', 'Manager'),
('clerk1', 'hash3', 'Clerk One', 'clerk1@company.com', 'Clerk'),
('clerk2', 'hash4', 'Clerk Two', 'clerk2@company.com', 'Clerk'),
('employee1', 'hash5', 'Employee One', 'emp1@company.com', 'Employee');