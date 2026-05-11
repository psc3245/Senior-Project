# db.py
import json
import sqlite3
import uuid
from pathlib import Path
from typing import Optional

from datetime import datetime, timezone

DB_PATH = Path("snapshots.sqlite3")


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def connect() -> sqlite3.Connection:
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn


def init_db() -> None:
    conn = connect()
    try:
        conn.execute("""
        CREATE TABLE IF NOT EXISTS valuations (
            snapshot_id TEXT PRIMARY KEY,
            created_at_utc TEXT NOT NULL,
            ticker TEXT NOT NULL,

            request_json TEXT NOT NULL,
            quote_json TEXT NOT NULL,
            result_json TEXT NOT NULL
        )
                     
        """)
        conn.execute(
            "CREATE INDEX IF NOT EXISTS idx_valuations_ticker_created "
            "ON valuations(ticker, created_at_utc DESC)"
        )
        conn.commit()

        

    finally:
        conn.close()


def save_snapshot(
    *,
    ticker: str,
    request_obj: dict,
    quote_obj: dict,
    result_obj: dict,
) -> str:
    snapshot_id = str(uuid.uuid4())
    created_at = _now_iso()

    conn = connect()
    try:
        conn.execute(
            """
            INSERT INTO valuations (
                snapshot_id,
                created_at_utc,
                ticker,
                request_json,
                quote_json,
                result_json
            )
            VALUES (?, ?, ?, ?, ?, ?)
            """,
            (
                snapshot_id,
                created_at,
                ticker.upper(),
                json.dumps(request_obj),
                json.dumps(quote_obj),
                json.dumps(result_obj),
            ),
        )
        conn.commit()
    finally:
        conn.close()

    return snapshot_id


def get_snapshot(snapshot_id: str) -> Optional[dict]:
    conn = connect()
    try:
        row = conn.execute(
            "SELECT * FROM valuations WHERE snapshot_id = ?",
            (snapshot_id,),
        ).fetchone()

        if not row:
            return None

        return {
            "snapshot_id": row["snapshot_id"],
            "created_at_utc": row["created_at_utc"],
            "ticker": row["ticker"],
            "request": json.loads(row["request_json"]),
            "quote": json.loads(row["quote_json"]),
            "result": json.loads(row["result_json"]),
        }
    finally:
        conn.close()


def list_snapshots(ticker: Optional[str] = None, limit: int = 20) -> list[dict]:
    limit = max(1, min(limit, 200))
    conn = connect()

    try:
        if ticker:
            rows = conn.execute(
                """
                SELECT snapshot_id, created_at_utc, ticker
                FROM valuations
                WHERE ticker = ?
                ORDER BY created_at_utc DESC
                LIMIT ?
                """,
                (ticker.upper(), limit),
            ).fetchall()
        else:
            rows = conn.execute(
                """
                SELECT snapshot_id, created_at_utc, ticker
                FROM valuations
                ORDER BY created_at_utc DESC
                LIMIT ?
                """,
                (limit,),
            ).fetchall()

        return [
            {
                "snapshot_id": r["snapshot_id"],
                "created_at_utc": r["created_at_utc"],
                "ticker": r["ticker"],
            }
            for r in rows
        ]
    finally:
        conn.close()
