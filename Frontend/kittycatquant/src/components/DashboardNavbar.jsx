import { Outlet, useNavigate, useLocation } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import AppLogo from "./AppLogo";
import ChatbotDrawer from "../components/ChatbotDrawer";

const API_BASE = process.env.REACT_APP_API_BASE_URL;

export default function DashboardLayout({ logout, identifier }) {
  const nav = useNavigate();
  const location = useLocation();

  const [searchQuery, setSearchQuery] = useState("");
  const [searchResults, setSearchResults] = useState([]);
  const [isSearching, setIsSearching] = useState(false);
  const [learnMode, setLearnMode] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const searchTimeout = useRef(null);

  const path = location.pathname.toLowerCase();
  const hideChatFab = path.startsWith("/chat");

  const isActive = (p) => path === p.toLowerCase();

  useEffect(() => {
    setSidebarOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    const query = searchQuery.trim().toLowerCase();

    if (!query) {
      setSearchResults([]);
      setIsSearching(false);
      return;
    }

    if (searchTimeout.current) clearTimeout(searchTimeout.current);

    searchTimeout.current = setTimeout(async () => {
      setIsSearching(true);
      try {
        const response = await fetch(`${API_BASE}/api/symbols`);
        if (!response.ok) throw new Error("Failed to fetch stocks");

        const data = await response.json();

        const filteredResults = (Array.isArray(data) ? data : [])
          .filter(
            (stock) =>
              stock.ticker.toLowerCase().includes(query) ||
              stock.name.toLowerCase().includes(query)
          )
          .map((stock) => ({
            symbol: stock.ticker,
            name: stock.name,
          }))
          .slice(0, 10);

        setSearchResults(filteredResults);
      } catch (error) {
        console.error("Search error:", error);
        setSearchResults([]);
      } finally {
        setIsSearching(false);
      }
    }, 300);

    return () => {
      if (searchTimeout.current) clearTimeout(searchTimeout.current);
    };
  }, [searchQuery]);

  useEffect(() => {
    function handleResize() {
      if (window.innerWidth > 900) {
        setSidebarOpen(false);
      }
    }

    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, []);

  const handleSelectStock = (symbol) => {
    setSearchQuery(symbol);
    setSearchResults([]);
    nav("/BuySell");
    setSidebarOpen(false);
  };

  const handleNav = (route) => {
    nav(route);
    setSidebarOpen(false);
  };

  return (
    <div className="dashShell">
      <aside className={`sidebar ${sidebarOpen ? "open" : ""}`}>
        <button
          type="button"
          className="mobileSidebarClose"
          onClick={() => setSidebarOpen(false)}
          aria-label="Close navigation"
        >
          ✕
        </button>

        <div className="sidebarTop">
          <AppLogo />
        </div>

        <div className="sideSearch" style={{ position: "relative" }}>
          <input
            placeholder="Search stock or name"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && searchResults.length > 0) {
                handleSelectStock(searchResults[0].symbol);
              }
            }}
          />

          {isSearching && (
            <div
              style={{
                marginTop: 8,
                fontSize: 12,
                color: "rgba(231,238,252,.55)",
              }}
            >
              Searching...
            </div>
          )}

          {!isSearching &&
            searchQuery.trim().length > 0 &&
            searchResults.length > 0 && (
              <div
                style={{
                  marginTop: 8,
                  background: "#172235",
                  border: "1px solid rgba(255,255,255,.08)",
                  borderRadius: 12,
                  overflow: "hidden",
                }}
              >
                {searchResults.map((stock) => (
                  <div
                    key={stock.symbol}
                    onMouseDown={() => handleSelectStock(stock.symbol)}
                    style={{
                      padding: "10px 12px",
                      cursor: "pointer",
                      borderBottom: "1px solid rgba(255,255,255,.06)",
                    }}
                  >
                    <div style={{ fontWeight: 700, color: "#e7eefc" }}>
                      {stock.symbol}
                    </div>
                    <div
                      style={{
                        fontSize: 12,
                        color: "rgba(231,238,252,.7)",
                      }}
                    >
                      {stock.name}
                    </div>
                  </div>
                ))}
              </div>
            )}

          {!isSearching &&
            searchQuery.trim().length > 0 &&
            searchResults.length === 0 && (
              <div
                style={{
                  marginTop: 8,
                  fontSize: 12,
                  color: "rgba(231,238,252,.55)",
                }}
              >
                No matches found
              </div>
            )}
        </div>

        <div className="sideNav">
          <div
            className={`sideItem ${isActive("/dashboard") ? "active" : ""}`}
            onClick={() => handleNav("/dashboard")}
          >
            Portfolio
          </div>

          <div
            className={`sideItem ${isActive("/watchlist") ? "active" : ""}`}
            onClick={() => handleNav("/watchlist")}
          >
            Watchlist
          </div>

          <div
            className={`sideItem ${isActive("/buysell") ? "active" : ""}`}
            onClick={() => handleNav("/buysell")}
          >
            Buy and Sell
          </div>

          <div
            className={`sideItem ${isActive("/stocknews") ? "active" : ""}`}
            onClick={() => handleNav("/stocknews")}
          >
            Stock News
          </div>

          <div
            className={`sideItem ${isActive("/chat") ? "active" : ""}`}
            onClick={() => handleNav("/chat")}
          >
            KittySayYouDo
          </div>

          <div
            className={`sideItem ${isActive("/settings") ? "active" : ""}`}
            onClick={() => handleNav("/settings")}
          >
            Settings
          </div>
        </div>

        <div className="sideUtility">
          <button
            type="button"
            className={`learnModePill ${learnMode ? "on" : ""}`}
            onClick={() => setLearnMode((v) => !v)}
          >
            <span className="learnModeDot" />
            <span className="learnModeLabel">Learn Mode</span>
            <span className="learnModeState">{learnMode ? "On" : "Off"}</span>
          </button>
        </div>

        <div className="sideBottom">
          <div className="sideItem" onClick={logout}>
            Log out
          </div>
          <div
            style={{
              padding: "10px 12px",
              color: "rgba(231,238,252,.55)",
              fontSize: 12,
            }}
          >
            Signed in as <strong>{identifier}</strong>
          </div>
        </div>
      </aside>

      {sidebarOpen && (
        <div
          className="sidebarOverlay"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <main className={`dashContent ${hideChatFab ? "noScroll noGutter" : ""}`}>
        <button
          type="button"
          className="mobileSidebarBtn"
          onClick={() => setSidebarOpen(true)}
          aria-label="Open navigation"
        >
          ☰
        </button>

        <Outlet
          context={{ searchQuery, setSearchQuery, learnMode, setLearnMode }}
        />
      </main>

      {!hideChatFab && <ChatbotDrawer />}
    </div>
  );
}