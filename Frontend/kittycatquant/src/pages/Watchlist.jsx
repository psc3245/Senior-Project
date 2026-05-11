import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useOutletContext } from "react-router-dom";
import "../styles/watchlist.css";

const API_BASE = process.env.REACT_APP_API_BASE_URL || "";

const money = (n) =>
  typeof n === "number" && Number.isFinite(n)
    ? n.toLocaleString(undefined, { style: "currency", currency: "USD" })
    : "—";

const numberFmt = (n) =>
  typeof n === "number" && Number.isFinite(n) ? n.toLocaleString() : "—";

export default function Watchlist() {
  const navigate = useNavigate();
  const outlet = useOutletContext?.() || {};
  const setSearchQuery = outlet.setSearchQuery || (() => {});

  const [watchlists, setWatchlists] = useState([]);
  const [selectedWatchlistId, setSelectedWatchlistId] = useState(null);
  const [watchlistStocks, setWatchlistStocks] = useState([]);
  const [enrichedStocks, setEnrichedStocks] = useState([]);

  const [newWatchlistName, setNewWatchlistName] = useState("");
  const [loadingLists, setLoadingLists] = useState(false);
  const [loadingStocks, setLoadingStocks] = useState(false);
  const [loadingMarketData, setLoadingMarketData] = useState(false);
  const [creatingList, setCreatingList] = useState(false);
  const [addingStock, setAddingStock] = useState(false);

  const [error, setError] = useState("");
  const [stockSearchQuery, setStockSearchQuery] = useState("");
  const [stockSearchResults, setStockSearchResults] = useState([]);
  const [isSearchingStocks, setIsSearchingStocks] = useState(false);

  const userId = useMemo(() => {
    const storedUserId = localStorage.getItem("kcq_userId");
    if (storedUserId) return storedUserId;

    try {
      const user = JSON.parse(localStorage.getItem("kcq_user") || "{}");
      return user.userId || "";
    } catch {
      return "";
    }
  }, []);

  const selectedWatchlist = useMemo(
    () =>
      watchlists.find(
        (w) => String(w.id ?? w.watchlistId) === String(selectedWatchlistId)
      ) || null,
    [watchlists, selectedWatchlistId]
  );

  const selectedWatchlistName =
    selectedWatchlist?.name ??
    selectedWatchlist?.title ??
    "Select a Watchlist";

  const fetchWatchlists = useCallback(async () => {
    setLoadingLists(true);
    setError("");

    try {
      if (!userId) throw new Error("No logged-in user found.");

      const res = await fetch(
        `${API_BASE}/api/watchlists?userId=${encodeURIComponent(userId)}`
      );

      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(
          `Failed to fetch watchlists: ${res.status} - ${errorText}`
        );
      }

      const data = await res.json();
      const lists = Array.isArray(data) ? data : [];
      setWatchlists(lists);

      if (lists.length > 0) {
        setSelectedWatchlistId((prev) => prev ?? (lists[0].id ?? lists[0].watchlistId));
      } else {
        setSelectedWatchlistId(null);
      }
    } catch (err) {
      console.error(err);
      setError(err.message || "Could not load watchlists.");
    } finally {
      setLoadingLists(false);
    }
  }, [userId]);

  const fetchWatchlistStocks = useCallback(async (watchlistId) => {
    setLoadingStocks(true);
    setError("");

    try {
      const res = await fetch(
        `${API_BASE}/api/watchlists/${watchlistId}/stocks`
      );

      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(`Failed to fetch stocks: ${res.status} - ${errorText}`);
      }

      const data = await res.json();
      setWatchlistStocks(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error(err);
      setError(err.message || "Could not load watchlist stocks.");
      setWatchlistStocks([]);
    } finally {
      setLoadingStocks(false);
    }
  }, []);

  const enrichStocksWithMarketData = useCallback(async (stocks) => {
    if (!Array.isArray(stocks) || stocks.length === 0) {
      setEnrichedStocks([]);
      return;
    }

    setLoadingMarketData(true);

    try {
      const enriched = await Promise.all(
        stocks.map(async (stock) => {
          const symbolId = stock.symbolId ?? stock.id ?? stock.ticker;
          const symbol = stock.symbol?.ticker ?? stock.ticker ?? "—";
          const name = stock.symbol?.name ?? stock.name ?? "—";

          try {
            const res = await fetch(
              `${API_BASE}/api/market/bars?symbol=${encodeURIComponent(symbol)}&tf=1d&limit=2`
            );

            if (!res.ok) {
              return {
                symbolId,
                symbol,
                name,
                price: null,
                change: null,
                changePct: null,
                volume: null,
              };
            }

            const body = await res.json();
            const bars = Array.isArray(body?.data) ? body.data : [];

            if (bars.length === 0) {
              return {
                symbolId,
                symbol,
                name,
                price: null,
                change: null,
                changePct: null,
                volume: null,
              };
            }

            const sorted = [...bars].sort(
              (a, b) => new Date(a.t).getTime() - new Date(b.t).getTime()
            );

            const latest = sorted[sorted.length - 1];
            const prev = sorted.length > 1 ? sorted[sorted.length - 2] : null;

            const price = Number(latest.c ?? latest.close ?? latest.Close);
            const prevPrice =
              prev != null
                ? Number(prev.c ?? prev.close ?? prev.Close)
                : null;

            const change =
              prevPrice != null && Number.isFinite(prevPrice)
                ? price - prevPrice
                : 0;

            const changePct =
              prevPrice != null &&
              Number.isFinite(prevPrice) &&
              prevPrice !== 0
                ? (change / prevPrice) * 100
                : 0;

            const volume =
              Number(latest.v ?? latest.volume ?? latest.Volume) || null;

            return {
              symbolId,
              symbol,
              name,
              price,
              change,
              changePct,
              volume,
            };
          } catch {
            return {
              symbolId,
              symbol,
              name,
              price: null,
              change: null,
              changePct: null,
              volume: null,
            };
          }
        })
      );

      setEnrichedStocks(enriched);
    } finally {
      setLoadingMarketData(false);
    }
  }, []);

  useEffect(() => {
    fetchWatchlists();
  }, [fetchWatchlists]);

  useEffect(() => {
    if (selectedWatchlistId) {
      fetchWatchlistStocks(selectedWatchlistId);
    } else {
      setWatchlistStocks([]);
      setEnrichedStocks([]);
    }

    setStockSearchQuery("");
    setStockSearchResults([]);
  }, [selectedWatchlistId, fetchWatchlistStocks]);

  useEffect(() => {
    enrichStocksWithMarketData(watchlistStocks);
  }, [watchlistStocks, enrichStocksWithMarketData]);

  useEffect(() => {
    if (!stockSearchQuery.trim()) {
      setStockSearchResults([]);
      setIsSearchingStocks(false);
      return;
    }

    const timeout = setTimeout(async () => {
      setIsSearchingStocks(true);

      try {
        const res = await fetch(`${API_BASE}/api/symbols`);
        if (!res.ok) throw new Error("Failed to fetch stocks");

        const data = await res.json();
        const query = stockSearchQuery.trim().toLowerCase();

        const filtered = (Array.isArray(data) ? data : [])
          .filter((stock) => {
            const ticker = (stock.ticker || "").toLowerCase();
            const name = (stock.name || "").toLowerCase();
            return ticker.includes(query) || name.includes(query);
          })
          .slice(0, 8);

        setStockSearchResults(filtered);
      } catch (err) {
        console.error("Stock search error:", err);
        setStockSearchResults([]);
      } finally {
        setIsSearchingStocks(false);
      }
    }, 250);

    return () => clearTimeout(timeout);
  }, [stockSearchQuery]);

  async function createWatchlist() {
    const name = newWatchlistName.trim();
    if (!name) return;

    setCreatingList(true);
    setError("");

    try {
      if (!userId) throw new Error("No logged-in user found.");

      const res = await fetch(`${API_BASE}/api/watchlists`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userId,
          name,
        }),
      });

      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(
          `Failed to create watchlist: ${res.status} - ${errorText}`
        );
      }

      const created = await res.json();
      const newWatchlistId = created.id ?? created.watchlistId;

      setNewWatchlistName("");
      await fetchWatchlists();

      if (newWatchlistId) {
        setSelectedWatchlistId(newWatchlistId);
        await fetchWatchlistStocks(newWatchlistId);
      }
    } catch (err) {
      console.error(err);
      setError(err.message || "Could not create watchlist.");
    } finally {
      setCreatingList(false);
    }
  }

  async function deleteWatchlist(watchlistId) {
    setError("");

    try {
      const res = await fetch(`${API_BASE}/api/watchlists/${watchlistId}`, {
        method: "DELETE",
      });

      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(
          `Failed to delete watchlist: ${res.status} - ${errorText}`
        );
      }

      const remaining = watchlists.filter(
        (w) => String(w.id ?? w.watchlistId) !== String(watchlistId)
      );

      setWatchlists(remaining);

      if (String(selectedWatchlistId) === String(watchlistId)) {
        setSelectedWatchlistId(
          remaining[0]?.id ?? remaining[0]?.watchlistId ?? null
        );
      }
    } catch (err) {
      console.error(err);
      setError(err.message || "Could not delete watchlist.");
    }
  }

  async function addSelectedStockToWatchlist(stock) {
    if (!selectedWatchlistId) {
      setError("No watchlist selected.");
      return;
    }

    setAddingStock(true);
    setError("");

    try {
      const res = await fetch(
        `${API_BASE}/api/watchlists/${selectedWatchlistId}/stocks`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ ticker: stock.ticker }),
        }
      );

      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(`Failed to add stock: ${res.status} - ${errorText}`);
      }

      setStockSearchQuery("");
      setStockSearchResults([]);
      await fetchWatchlistStocks(selectedWatchlistId);
    } catch (err) {
      console.error(err);
      setError(err.message || "Could not add stock to watchlist.");
    } finally {
      setAddingStock(false);
    }
  }

  async function removeStockFromWatchlist(symbolId) {
    if (!selectedWatchlistId) return;

    setError("");

    try {
      const res = await fetch(
        `${API_BASE}/api/watchlists/${selectedWatchlistId}/stocks/${symbolId}`,
        {
          method: "DELETE",
        }
      );

      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(`Failed to remove stock: ${res.status} - ${errorText}`);
      }

      setWatchlistStocks((prev) =>
        prev.filter(
          (stock) =>
            String(stock.symbolId ?? stock.id ?? stock.ticker) !== String(symbolId)
        )
      );
    } catch (err) {
      console.error(err);
      setError(err.message || "Could not remove stock.");
    }
  }

  function openBuySell(symbol) {
    setSearchQuery(symbol);
    navigate("/buysell");
  }

  const stats = useMemo(() => {
    const up = enrichedStocks.filter(
      (s) => typeof s.change === "number" && s.change > 0
    ).length;
    const down = enrichedStocks.filter(
      (s) => typeof s.change === "number" && s.change < 0
    ).length;
    return {
      total: enrichedStocks.length,
      up,
      down,
    };
  }, [enrichedStocks]);

  return (
    <div className="watchlistPage">
      <div className="watchlistHeader">
        <div>
          <h1>Watchlist</h1>
          <p>Manage your watchlists and tracked stocks.</p>
        </div>
      </div>

      {error && <div className="watchlistError">{error}</div>}

      <div className="watchlistTopBar">
        <div className="watchlistCreateRow fullWidth">
          <input
            type="text"
            placeholder="New watchlist name"
            value={newWatchlistName}
            onChange={(e) => setNewWatchlistName(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") createWatchlist();
            }}
          />
          <button
            onClick={createWatchlist}
            disabled={creatingList || !newWatchlistName.trim()}
          >
            {creatingList ? "Creating..." : "Create"}
          </button>
        </div>
      </div>

      <div className="watchlistTabsCard">
        <div className="watchlistTabsHeader">
          <h2>Your Watchlists</h2>
          {loadingLists && <div className="watchlistMuted">Loading watchlists...</div>}
        </div>

        {!loadingLists && watchlists.length === 0 ? (
          <div className="watchlistMuted">No watchlists yet.</div>
        ) : (
          <div className="watchlistTabs">
            {watchlists.map((watchlist) => {
              const id = watchlist.id ?? watchlist.watchlistId;
              const name =
                watchlist.name ?? watchlist.title ?? `Watchlist ${id}`;
              const isActive = String(id) === String(selectedWatchlistId);

              return (
                <div
                  key={id}
                  className={`watchlistTab ${isActive ? "active" : ""}`}
                  onClick={() => setSelectedWatchlistId(id)}
                >
                  <span className="watchlistTabName">{name}</span>

                  <button
                    className="dangerBtn small"
                    onClick={(e) => {
                      e.stopPropagation();
                      deleteWatchlist(id);
                    }}
                  >
                    Delete
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <section className="watchlistMainCard">
        <div className="watchlistContentHeader">
          <div>
            <h2>{selectedWatchlistName}</h2>
            <p>
              {selectedWatchlistId
                ? `${stats.total} stocks • ${stats.up} up • ${stats.down} down`
                : "Select a watchlist to view tracked stocks."}
            </p>
          </div>
        </div>

        {selectedWatchlistId && (
          <div className="watchlistCreateRow" style={{ marginBottom: 20 }}>
            <div style={{ position: "relative", flex: 1 }}>
              <input
                type="text"
                placeholder="Search stock (e.g. AAPL or Apple)"
                value={stockSearchQuery}
                onChange={(e) => setStockSearchQuery(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && stockSearchResults.length > 0) {
                    addSelectedStockToWatchlist(stockSearchResults[0]);
                  }
                }}
              />

              {isSearchingStocks && (
                <div className="watchlistMuted" style={{ marginTop: 8 }}>
                  Searching stocks...
                </div>
              )}

              {!isSearchingStocks &&
                stockSearchQuery.trim() &&
                stockSearchResults.length > 0 && (
                  <div className="stockDropdown">
                    {stockSearchResults.map((stock) => (
                      <div
                        key={stock.ticker}
                        className="stockItem"
                        onMouseDown={() => addSelectedStockToWatchlist(stock)}
                      >
                        <div className="stockItemTicker">{stock.ticker}</div>
                        <div className="stockItemName">{stock.name}</div>
                      </div>
                    ))}
                  </div>
                )}

              {!isSearchingStocks &&
                stockSearchQuery.trim() &&
                stockSearchResults.length === 0 && (
                  <div className="watchlistMuted" style={{ marginTop: 8 }}>
                    No matching stocks found.
                  </div>
                )}
            </div>

            <button
              disabled={addingStock || stockSearchResults.length === 0}
              onClick={() => {
                if (stockSearchResults.length > 0) {
                  addSelectedStockToWatchlist(stockSearchResults[0]);
                }
              }}
            >
              {addingStock ? "Adding..." : "Add Stock"}
            </button>
          </div>
        )}

        {!selectedWatchlistId ? (
          <div className="watchlistMuted">No watchlist selected.</div>
        ) : loadingStocks || loadingMarketData ? (
          <div className="watchlistMuted">Loading stocks...</div>
        ) : enrichedStocks.length === 0 ? (
          <div className="watchlistMuted">No stocks in this watchlist yet.</div>
        ) : (
          <div className="watchlistTableWrap">
            <table className="watchlistTable expanded">
              <thead>
                <tr>
                  <th>Symbol</th>
                  <th>Name</th>
                  <th>Market Price</th>
                  <th>Change</th>
                  <th>% Change</th>
                  <th>Volume</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {enrichedStocks.map((stock) => {
                  const isPos = typeof stock.change === "number" && stock.change >= 0;
                  const isNeg = typeof stock.change === "number" && stock.change < 0;

                  return (
                    <tr
                      key={stock.symbolId}
                      className="clickableRow"
                      onClick={() => openBuySell(stock.symbol)}
                    >
                      <td className="symbolCell">{stock.symbol}</td>
                      <td>{stock.name}</td>
                      <td>{money(stock.price)}</td>

                      <td className={isPos ? "pos" : isNeg ? "neg" : ""}>
                        {typeof stock.change === "number"
                          ? `${stock.change >= 0 ? "+" : ""}${money(stock.change)}`
                          : "—"}
                      </td>

                      <td className={isPos ? "pos" : isNeg ? "neg" : ""}>
                        {typeof stock.changePct === "number"
                          ? `${stock.changePct >= 0 ? "+" : ""}${stock.changePct.toFixed(2)}%`
                          : "—"}
                      </td>

                      <td>{numberFmt(stock.volume)}</td>

                      <td>
                        <button
                          className="dangerBtn"
                          onClick={(e) => {
                            e.stopPropagation();
                            removeStockFromWatchlist(stock.symbolId);
                          }}
                        >
                          Remove
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}