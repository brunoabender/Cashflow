CREATE TABLE IF NOT EXISTS transactions (
    id UUID PRIMARY KEY,
    amount NUMERIC NOT NULL,
    type INTEGER NOT NULL, -- 1 = Credit, 2 = Debit
    timestamp TIMESTAMP NOT NULL,
    id_potency_key UUID NOT NULL UNIQUE
);