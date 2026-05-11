// src/pages/StockNews.jsx
import { useEffect, useMemo, useState, useRef } from "react";
import { analyzeHeadline } from "../utils/sentimentModel";
import "../styles/dashboard.css";


const LS_KEY = "kcq_saved_news_ids";
const API_BASE = process.env.REACT_APP_API_BASE_URL || "";
const userId = localStorage.getItem("kcq_userId") || "";


function HeartIcon({ filled = false, size = 18 }) {
  return (
    <span
      style={{
        width: size,
        height: size,
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        overflow: "visible",  
      }}
    >
      <svg
        viewBox="0 0 24 24"
        width="100%"
        height="100%"
        preserveAspectRatio="xMidYMid meet"
        fill={filled ? "currentColor" : "none"}
        stroke="currentColor"
        strokeWidth="2"
      >
        <path d="M12 21s-6.7-4.4-9.3-8.3c-1.6-2.3-.7-5.5 2-6.8 1.8-.9 4-.3 5.3 1.3l2 2 2-2c1.3-1.6 3.5-2.2 5.3-1.3 2.7 1.3 3.6 4.5 2 6.8C18.7 16.6 12 21 12 21z" />
      </svg>
    </span>
  );
}


function SearchIcon({ size = 16 }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 26 26"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      style={{ display: "block", flexShrink: 0 }}
    >
      <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="1.8" />
      <path d="M16.5 16.5L21 21" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}


function normalizeArticle(a, idx) {
  const id = a?.id || `article_${idx}`;
  const source = a?.sourceName || "Unknown";
  const title = a?.title || "Untitled";
  const image = a?.image || "";
  const url = a?.url || "";
  const publishedAt = a?.publishedAt ? new Date(a.publishedAt) : null;


  return {
    id,
    source,
    title,
    image,
    url,
    time: publishedAt ? publishedAt.toLocaleString() : "",
    content: a?.content || a?.description || "",
    description: a?.description || "",
    favorited: a?.favorited || false,
  };
}


export default function StockNews() {
  const [expandedId, setExpandedId] = useState(null);
  const [showSavedOnly, setShowSavedOnly] = useState(false);
  const [showCategoryMenu, setShowCategoryMenu] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [searchInput, setSearchInput] = useState("");


  const [loading, setLoading] = useState(true);
  const [searching, setSearching] = useState(false);
  const [err, setErr] = useState(null);
  const [articlesRaw, setArticlesRaw] = useState([]);
  const [sentimentMap, setSentimentMap] = useState({});


  // Tracks which article IDs are currently mid-request, preventing double-clicks
  const pendingFavorites = useRef(new Set());


  const searchTimeoutRef = useRef(null);


  // Saved IDs — persisted to localStorage as a fast local cache
  const [savedIds, setSavedIds] = useState(() => {
    try {
      const raw = localStorage.getItem(LS_KEY);
      const arr = raw ? JSON.parse(raw) : [];
      return Array.isArray(arr) ? arr : [];
    } catch {
      return [];
    }
  });


  const categories = [
    "general",
    "business",
    "technology",
    "entertainment",
    "sports",
    "science",
    "health",
  ];


  // useEffect(() => {
  //   loadModel(); // warm up model immediately
  // }, []);


  useEffect(() => {
    localStorage.setItem(LS_KEY, JSON.stringify(savedIds));
  }, [savedIds]);


  const savedSet = useMemo(() => new Set(savedIds), [savedIds]);


  const articles = useMemo(() => {
    const normalized = (articlesRaw || []).map(normalizeArticle);


    if (!showSavedOnly) return normalized;


    return normalized.filter((a) => savedSet.has(a.id));
  }, [articlesRaw, showSavedOnly, savedSet]);


  const cacheRef = useRef({});


  useEffect(() => {
    if (!articles || articles.length === 0) return;


    let isCancelled = false;


    async function runSentiment() {
      const results = {};


      await Promise.all(
        articles.map(async (a) => {
          const id = a.id;


          try {
            if (cacheRef.current[a.title]) {
              results[id] = cacheRef.current[a.title];
              return;
            }


            const title = typeof a.title === "string" ? a.title : "";
            const analysis = await analyzeHeadline(title);


            // Fallback if model fails
            console.log("Analysis for:", a.title, analysis);
            if (!analysis) {
              results[id] = {
                sentiment: "neutral",
                confidence: 0,
                impact: "Unknown",
              };
              return;
            }


            // Save to cache
            cacheRef.current[a.title] = analysis;
            results[id] = analysis;
          } catch (err) {
            console.error("Sentiment error:", err);
            results[id] = {
              sentiment: "neutral",
              confidence: 0,
              impact: "Unknown",
            };
          }
        })
      );


      if (!isCancelled) {
        setSentimentMap(results);
      }
    }


    runSentiment();


    return () => {
      isCancelled = true;
    };
  }, [articles]);


  function toggleArticle(id) {
    setExpandedId((prev) => (prev === id ? null : id));
  }


  async function makeFavorite(articleId) {
    const res = await fetch(
      `${API_BASE}/api/articles/favorites/${encodeURIComponent(articleId)}?userId=${encodeURIComponent(userId)}`,
      { method: "POST" }
    );
    if (!res.ok) {
      throw new Error(`Failed to save article (${res.status})`);
    }
  }


  async function deleteFavorite(articleId) {
    const res = await fetch(
      `${API_BASE}/api/articles/favorites/${encodeURIComponent(articleId)}?userId=${encodeURIComponent(userId)}`,
      { method: "DELETE" }
    );
    if (!res.ok) {
      throw new Error(`Failed to remove article (${res.status})`);
    }
  }


  async function toggleSave(articleId) {
    if (pendingFavorites.current.has(articleId)) return;
    pendingFavorites.current.add(articleId);


    const isCurrentlySaved = savedSet.has(articleId);


    setSavedIds((prev) =>
      isCurrentlySaved
        ? prev.filter((x) => x !== articleId)
        : [articleId, ...prev]
    );


    try {
      if (isCurrentlySaved) {
        await deleteFavorite(articleId);
      } else {
        await makeFavorite(articleId);
      }
    } catch (e) {
      console.error("Favorite toggle failed:", e);
      setSavedIds((prev) =>
        isCurrentlySaved
          ? [articleId, ...prev]
          : prev.filter((x) => x !== articleId)
      );
    } finally {
      pendingFavorites.current.delete(articleId);
    }
  }


  async function fetchCategoryArticles(category) {
    try {
      setLoading(true);
      setErr(null);
      setShowSavedOnly(false);


      const url = `${API_BASE}/api/articles/headlines?category=${category}&lang=en&country=us&page=1&max=10`;


      const res = await fetch(url);
      const data = await res.json();


      if (!res.ok) throw new Error(data?.message || `Request failed (${res.status})`);


      setArticlesRaw(Array.isArray(data) ? data : []);
    } catch (e) {
      console.error(e);
      setErr(e?.message || String(e));
      setArticlesRaw([]);
    } finally {
      setLoading(false);
    }
  }


  async function fetchHeadlines() {
    try {
      setLoading(true);
      setErr(null);
      setShowSavedOnly(false);


      const url = `${API_BASE}/api/articles/headlines?lang=en&country=us&page=1&max=10`;
      console.log("Fetching headlines from:", url);
      // const res = await fetch(url);
      const res = await fetch(url);
      // const text = await res.text();
      // console.log(text);
      const data = await res.json();


      if (!res.ok) throw new Error(data?.message || `Request failed (${res.status})`);


      setArticlesRaw(Array.isArray(data) ? data : []);
    } catch (e) {
      console.error(e);
      setErr(e?.message || String(e));
      setArticlesRaw([]);
    } finally {
      setLoading(false);
    }
  }


  // ─── GET: fetch the user's favorited articles from the backend ─────────────
  async function fetchFavorites() {
    try {
      setLoading(true);
      setErr(null);


      const url = `${API_BASE}/api/articles/favorites?userId=${encodeURIComponent(userId)}`;
      const res = await fetch(url);
      const data = await res.json();


      if (!res.ok) throw new Error(data?.message || `Request failed (${res.status})`);


      const arr = Array.isArray(data) ? data : [];


      // Keep the local savedIds cache in sync with what the server returns
      setSavedIds(arr.map((a) => a.id));
      setArticlesRaw(arr);
      setShowSavedOnly(true);
    } catch (e) {
      console.error(e);
      setErr(e?.message || String(e));
      setArticlesRaw([]);
    } finally {
      setLoading(false);
    }
  }


  // ─── Saved button: toggle between favorites view and headlines view ─────────
  function handleToggleSaved() {
    if (showSavedOnly) {
      // Already in saved view — go back to headlines
      fetchHeadlines();
    } else {
      // Entering saved view — fetch fresh from backend
      fetchFavorites();
    }
  }


  // ─── GET: search ──────────────────────────────────────────────────────────
  async function fetchSearch(query) {
    try {
      setSearching(true);
      setErr(null);


      const url = `${API_BASE}/api/articles/search?q=${encodeURIComponent(query)}&lang=en&country=us&page=1&max=10`;
      const res = await fetch(url);
      const data = await res.json();


      if (!res.ok) throw new Error(data?.message || `Request failed (${res.status})`);


      setArticlesRaw(Array.isArray(data) ? data : []);
    } catch (e) {
      console.error(e);
      setErr(e?.message || String(e));
      setArticlesRaw([]);
    } finally {
      setSearching(false);
    }
  }


  function handleSearchChange(e) {
    const val = e.target.value;
    setSearchInput(val);
    clearTimeout(searchTimeoutRef.current);


    if (val.trim() === "") {
      setSearchQuery("");
      fetchHeadlines();
      return;
    }


    searchTimeoutRef.current = setTimeout(() => {
      setSearchQuery(val.trim());
      fetchSearch(val.trim());
    }, 400);
  }


  function clearSearch() {
    setSearchInput("");
    setSearchQuery("");
    fetchHeadlines();
  }


  useEffect(() => {
    fetchHeadlines();
    return () => clearTimeout(searchTimeoutRef.current);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);


  const savedCount = savedIds.length;
  const isLoadingAny = loading || searching;


  return (
    <div className="main">
      {/* Header row */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          gap: 12,
          marginBottom: 12,
          flexWrap: "wrap",
        }}
      >
        <div>
          <div style={{ fontSize: 22, fontWeight: 900, letterSpacing: ".2px" }}>
            Stock News
          </div>
          <div style={{ color: "rgba(231,238,252,.55)", fontSize: 13, marginTop: 4 }}>
            Latest Headlines{searchQuery ? ` for "${searchQuery}"` : ""}
          </div>
        </div>


        <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
          {/* <button className="btn" onClick={fetchHeadlines} disabled={isLoadingAny}>
            {isLoadingAny ? "Loading…" : "Refresh"}
          </button> */}


          <button
            className="btn"
            onClick={handleToggleSaved}
            disabled={isLoadingAny}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 8,
              borderRadius: 12,
              background: showSavedOnly ? "rgba(255,107,107,.16)" : "rgba(255,255,255,.04)",
              borderColor: showSavedOnly ? "rgba(255,107,107,.35)" : "rgba(255,255,255,.10)",
              color: showSavedOnly ? "rgba(255,107,107,.95)" : "rgba(231,238,252,.85)",
            }}
            title={showSavedOnly ? "Back to headlines" : "Show saved"}
          >
            <HeartIcon filled={showSavedOnly} size={18} />
            {savedCount}
          </button>


          <button
            className="btn"
            onClick={() => setShowCategoryMenu(!showCategoryMenu)}
            disabled={isLoadingAny}
            style={{
              display: "inline-flex",
              alignItems: "center",
              justifyContent: "center",
              height: 40,
              width: 40,
              borderRadius: 12,
              background: "rgba(255,255,255,.04)",
              borderColor: "rgba(255,255,255,.10)",
            }}
            title="Filter by category"
          >
            <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
              <span style={{ width: 16, height: 2, background: "#fff" }} />
              <span style={{ width: 16, height: 2, background: "#fff" }} />
              <span style={{ width: 16, height: 2, background: "#fff" }} />
            </div>
          </button>
          {showCategoryMenu && (
            <div
              style={{
                position: "absolute",
                top: 48,
                right: 0,
                transform: "translateX(-28px) translateY(36px)",  
                maxWidth: 280,
                background: "rgba(255,255,255,.04)",  
                backdropFilter: "blur(10px)",
                borderRadius: 18,
                padding: 10,
                border: "1px solid rgba(255,255,255,.10)",
                boxShadow: "0 12px 30px rgba(0,0,0,.35)",
                zIndex: 50,
              }}
            >
              {categories.map((cat) => {
                const active = selectedCategory === cat;


                return (
                  <button
                    key={cat}
                    onClick={() => {
                      if (selectedCategory === cat) {
                        setSelectedCategory(null);
                        fetchHeadlines();
                      } else {
                        setSelectedCategory(cat);
                        fetchCategoryArticles(cat);
                      }
                    }}
                    style={{
                      width: "100%",
                      textAlign: "left",
                      padding: "9px 12px",
                      borderRadius: 12,
                      border: "none",
                      background: active
                        ? "rgba(255,107,107,.16)"
                        : "transparent",
                      color: active
                        ? "rgba(255,107,107,.95)"
                        : "rgba(231,238,252,.85)",
                      fontSize: 14,
                      cursor: "pointer",
                      transition: "all .15s ease",
                    }}
                    onMouseEnter={(e) => {
                      if (!active) e.target.style.background = "rgba(255,255,255,.06)";
                    }}
                    onMouseLeave={(e) => {
                      if (!active) e.target.style.background = "transparent";
                    }}
                  >
                    {cat.charAt(0).toUpperCase() + cat.slice(1)}
                  </button>
                );
              })}
            </div>
          )}
        </div>
      </div>


      {/* Search bar */}
      <div style={{ position: "relative", marginBottom: 14 }}>
        <div
          style={{
            position: "absolute",
            left: 14,
            top: "50%",
            transform: "translateY(-50%)",
            color: "rgba(231,238,252,.4)",
            pointerEvents: "none",
            display: "flex",
            alignItems: "center",
          }}
        >
          <SearchIcon size={16} />
        </div>


        <input
          type="text"
          value={searchInput}
          onChange={handleSearchChange}
          placeholder="Search articles by headline"
          style={{
            width: "100%",
            boxSizing: "border-box",
            padding: "10px 40px 10px 40px",
            borderRadius: 14,
            border: "1px solid rgba(255,255,255,.10)",
            background: "rgba(255,255,255,.04)",
            color: "rgba(231,238,252,.9)",
            fontSize: 14,
            outline: "none",
            transition: "border-color .2s ease, background .2s ease",
          }}
          onFocus={(e) => {
            e.target.style.borderColor = "rgba(255,255,255,.22)";
            e.target.style.background = "rgba(255,255,255,.07)";
          }}
          onBlur={(e) => {
            e.target.style.borderColor = "rgba(255,255,255,.10)";
            e.target.style.background = "rgba(255,255,255,.04)";
          }}
        />


        {searchInput && (
          <button
            onClick={clearSearch}
            style={{
              position: "absolute",
              right: 10,
              top: "50%",
              transform: "translateY(-50%)",
              background: "none",
              border: "none",
              cursor: "pointer",
              color: "rgba(231,238,252,.45)",
              padding: "4px 6px",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              borderRadius: 6,
              fontSize: 16,
              lineHeight: 1,
            }}
            title="Clear search"
          >
            ×
          </button>
        )}
      </div>


      {/* Error */}
      {err && (
        <div
          style={{
            border: "1px solid rgba(255,107,107,.35)",
            background: "rgba(255,107,107,.10)",
            borderRadius: 14,
            padding: 12,
            color: "rgba(231,238,252,.9)",
            marginBottom: 14,
          }}
        >
          <strong style={{ color: "salmon" }}>News error:</strong> {err}
        </div>
      )}


      {/* List container */}
      <div
        style={{
          marginTop: 10,
          border: "1px solid rgba(255,255,255,.08)",
          borderRadius: 22,
          background: "rgba(255,255,255,.03)",
          overflow: "hidden",
        }}
      >
        {isLoadingAny ? (
          <div style={{ padding: 22, color: "rgba(231,238,252,.65)" }}>
            {searching ? "Searching…" : "Loading articles…"}
          </div>
        ) : articles.length === 0 ? (
          <div style={{ padding: 22, color: "rgba(231,238,252,.65)" }}>
            {showSavedOnly
              ? "No saved articles yet. Tap the heart on an article to save it."
              : searchQuery
              ? `No articles found for "${searchQuery}".`
              : "No articles to show."}
          </div>
        ) : (
          articles.map((a, idx) => {
            const isOpen = expandedId === a.id;
            const isSaved = savedSet.has(a.id);


            return (
              <div
                key={a.id}
                style={{
                  padding: 18,
                  borderBottom:
                    idx === articles.length - 1
                      ? "none"
                      : "1px solid rgba(255,255,255,.06)",
                }}
              >
                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns: "1fr 112px",
                    gap: 14,
                    alignItems: "center",
                  }}
                >
                  <div>
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        gap: 10,
                        flexWrap: "wrap",
                      }}
                    >
                      <div
                        style={{
                          color: "rgba(231,238,252,.55)",
                          fontSize: 13,
                          fontWeight: 700,
                        }}
                      >
                        {a.source}
                        {a.time ? ` · ${a.time}` : ""}
                      </div>


                      <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
                        {/* {a.url ? (
                          <a
                            className="btn"
                            href={a.url}
                            target="_blank"
                            rel="noreferrer"
                            style={{
                              padding: "8px 10px",
                              borderRadius: 12,
                              display: "inline-flex",
                              alignItems: "center",
                              justifyContent: "center",
                            }}
                            title="Open original"
                          >
                            Open
                          </a>
                        ) : null} */}


                        <button
                          onClick={() => toggleSave(a.id)}
                          className="btn"
                          title={isSaved ? "Unsave" : "Save"}
                          style={{
                            padding: "8px",
                            display: "inline-flex",
                            alignItems: "center",
                            justifyContent: "center",
                            lineHeight: 0,
                            borderRadius: 12,
                            background: isSaved
                              ? "rgba(255,107,107,.16)"
                              : "rgba(255,255,255,.04)",
                            borderColor: isSaved
                              ? "rgba(255,107,107,.35)"
                              : "rgba(255,255,255,.10)",
                            color: isSaved
                              ? "rgba(255,107,107,.95)"
                              : "rgba(231,238,252,.85)",
                          }}
                        >
                          <HeartIcon filled={isSaved} size={18} />
                        </button>
                      </div>
                    </div>


                    <div
                      style={{
                        marginTop: 10,
                        fontSize: 22,
                        fontWeight: 900,
                        lineHeight: 1.15,
                      }}
                    >
                      {a.title}
                      {!sentimentMap[a.id] && (
                        <div style={{ fontSize: 12, color: "rgba(231,238,252,.5)", marginTop: 6 }}>
                          Analyzing sentiment...
                        </div>
                      )}
                      {sentimentMap[a.id] && (
                        <div
                          style={{
                            marginTop: 10,
                            padding: "10px 12px",
                            borderRadius: 12,
                            background: "rgba(255,255,255,.04)",
                            border: "1px solid rgba(255,255,255,.08)",
                            display: "flex",
                            justifyContent: "space-between",
                            alignItems: "center",
                            gap: 10,
                            flexWrap: "wrap"
                          }}
                        >
                          <div style={{ fontSize: 13 }}>
                            Sentiment:
                            <span style={{
                              marginLeft: 6,
                              fontWeight: 700,
                              color:
                                sentimentMap[a.id].sentiment === "bullish"
                                  ? "rgba(53,208,127,.95)"
                                  : sentimentMap[a.id].sentiment === "bearish"
                                  ? "rgba(255,107,107,.95)"
                                  : "rgba(231,238,252,.8)"
                            }}>
                              {sentimentMap[a.id].sentiment.toUpperCase()}
                            </span>
                          </div>


                          <div style={{ fontSize: 13 }}>
                            Impact: <strong>{sentimentMap[a.id].impact}</strong>
                          </div>


                          <div style={{ fontSize: 13 }}>
                            Confidence: <strong>{sentimentMap[a.id].confidence}%</strong>
                          </div>
                        </div>
                      )}
                    </div>


                    <button
                      className="btn"
                      style={{ marginTop: 12 }}
                      onClick={() => toggleArticle(a.id)}
                    >
                      {isOpen ? "Close" : "Read More"}
                    </button>
                  </div>


                  <div
                    style={{
                      width: 112,
                      height: 112,
                      borderRadius: 20,
                      overflow: "hidden",
                      border: "1px solid rgba(255,255,255,.10)",
                      background: "rgba(0,0,0,.18)",
                    }}
                  >
                    {a.image ? (
                      <img
                        src={a.image}
                        alt=""
                        style={{ width: "100%", height: "100%", objectFit: "cover" }}
                      />
                    ) : null}
                  </div>
                </div>


                {/* Expanded content */}
                <div
                  style={{
                    marginTop: isOpen ? 18 : 0,
                    maxHeight: isOpen ? 520 : 0,
                    opacity: isOpen ? 1 : 0,
                    overflow: "hidden",
                    transition: "all .35s ease",
                  }}
                >
                  <div
                    style={{
                      padding: isOpen ? "12px 0 6px" : 0,
                      color: "rgba(231,238,252,.85)",
                      lineHeight: 1.55,
                      fontSize: 15,
                    }}
                  >
                    {a.content || a.description || "No preview available."}
                    {a.url ? (
                      <div style={{ marginTop: 12 }}>
                        <a href={a.url} target="_blank" rel="noreferrer">
                          Read full article →
                        </a>
                      </div>
                    ) : null}
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}

