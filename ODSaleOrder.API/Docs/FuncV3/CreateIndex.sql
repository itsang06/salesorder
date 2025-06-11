-- Create index for SO_OrderInformations table
CREATE INDEX idx_orderinfo ON "SO_OrderInformations" ("OrderRefNumber", "OrderDate", "DistributorCode");

-- Create index for SO_OrderItems table
CREATE INDEX idx_orderitem ON "SO_OrderItems" ("OrderRefNumber");