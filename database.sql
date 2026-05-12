CREATE DATABASE IF NOT EXISTS subscription_reminder_db;
USE subscription_reminder_db;

CREATE TABLE IF NOT EXISTS Customers (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  FullName VARCHAR(120) NOT NULL,
  Email VARCHAR(160) NOT NULL,
  PhoneNumber VARCHAR(30) NOT NULL,
  Username VARCHAR(80) NOT NULL,
  PasswordHash VARCHAR(500) NOT NULL,
  UNIQUE KEY UX_Customers_Username (Username)
);

CREATE TABLE IF NOT EXISTS Subscriptions (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  CustomerId INT NOT NULL,
  Type INT NOT NULL,
  ProviderName VARCHAR(120) NOT NULL,
  SubscriberNumber VARCHAR(80) NOT NULL,
  Status INT NOT NULL,
  BillingDay INT NOT NULL DEFAULT 1,
  PreferredPaymentDay INT NOT NULL DEFAULT 10,
  CONSTRAINT FK_Subscriptions_Customers FOREIGN KEY (CustomerId)
    REFERENCES Customers(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Payments (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  SubscriptionId INT NOT NULL,
  Amount DECIMAL(12, 2) NOT NULL,
  PaymentDate DATETIME(6) NOT NULL,
  Period VARCHAR(7) NOT NULL,
  Status INT NOT NULL,
  CONSTRAINT FK_Payments_Subscriptions FOREIGN KEY (SubscriptionId)
    REFERENCES Subscriptions(Id) ON DELETE CASCADE
);
