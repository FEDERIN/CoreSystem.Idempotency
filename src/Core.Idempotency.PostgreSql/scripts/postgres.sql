CREATE TABLE IF NOT EXISTS idempotency_keys
(
    key TEXT PRIMARY KEY,

    state TEXT NOT NULL DEFAULT 'completed' CHECK (state IN ('in_progress', 'completed')),

    lease_id TEXT,

    request_fingerprint TEXT,

    hash_algorithm TEXT,

    status_code INTEGER,

    content_type TEXT,

    headers BYTEA,

    body BYTEA,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    expires_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_idempotency_expires
    ON idempotency_keys (expires_at);
