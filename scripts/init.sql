CREATE TABLE IF NOT EXISTS transactions (
    id UUID PRIMARY KEY,
    amount NUMERIC(18,2) NOT NULL,
    type INTEGER NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    id_potency_key UUID NOT NULL UNIQUE
);