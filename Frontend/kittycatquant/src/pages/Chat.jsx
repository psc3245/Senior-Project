// src/pages/Chat.jsx
import { useEffect, useMemo, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import "../styles/kittychat.css";

const API_BASE = (process.env.REACT_APP_API_BASE_URL || "").replace(/\/$/, "");
const HUB_URL = process.env.REACT_APP_SIGNALR_CHAT_URL || `${API_BASE}/chat`;

function clampStr(s, n) {
  if (!s) return "";
  return s.length > n ? `${s.slice(0, n - 1)}…` : s;
}

function AvatarPill({ label }) {
  return (
    <div
      style={{
        width: 32,
        height: 32,
        borderRadius: 999,
        display: "grid",
        placeItems: "center",
        border: "1px solid rgba(255,255,255,.10)",
        background: "rgba(255,255,255,.04)",
        color: "rgba(231,238,252,.9)",
        fontWeight: 800,
        fontSize: 13,
        flex: "0 0 auto",
      }}
      title={label}
    >
      {label}
    </div>
  );
}

function XIcon({ size = 16 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none">
      <path
        d="M18 6L6 18M6 6l12 12"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

function PencilIcon({ size = 16 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none">
      <path
        d="M12 20h9"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
      <path
        d="M16.5 3.5a2.12 2.12 0 1 1 3 3L7 19l-4 1 1-4 12.5-12.5z"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function generateId() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

function SearchIcon({ size = 16 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none">
      <path
        d="M21 21l-4.35-4.35M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15z"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

function ImageIcon({ size = 16 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none">
      <path
        d="M4 6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6z"
        stroke="currentColor"
        strokeWidth="2"
      />
      <path
        d="M8 14l2.5-2.5L14 15l2-2 2 2"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M9 9.2h.01"
        stroke="currentColor"
        strokeWidth="3"
        strokeLinecap="round"
      />
    </svg>
  );
}

function normalizeThread(thread) {
  const messages = Array.isArray(thread?.messages) ? thread.messages : [];
  const latestTs = messages.length
    ? Math.max(
        ...messages.map((m) =>
          m?.timestamp ? new Date(m.timestamp).getTime() : 0
        )
      )
    : Date.now();
  return {
    id: thread?.id ?? generateId(),
    title: thread?.sessionName || "New chat",
    updatedAt: latestTs,
  };
}

function sortMessagesByTimestamp(messages) {
  return [...messages].sort((a, b) => a.ts - b.ts);
}

function normalizeMessage(message) {
  const rawRole = String(message?.role || "").toLowerCase();
  const role = rawRole === "user" ? "you" : "ai";

  return {
    id: message?.id || generateId(),
    role,
    type: message?.type === "image" ? "image" : "text",
    text: message?.message || message?.text || "",
    imageUrl: message?.imageUrl || "",
    caption: message?.caption || "",
    ts: message?.timestamp ? new Date(message.timestamp).getTime() : Date.now(),
  };
}

export default function Chat() {
  const [threads, setThreads] = useState([]);
  const [activeId, setActiveId] = useState(null);
  const [messagesByChat, setMessagesByChat] = useState({});
  const [search, setSearch] = useState("");
  const [draft, setDraft] = useState("");
  const [loadingThreads, setLoadingThreads] = useState(true);
  const [loadingMessages, setLoadingMessages] = useState(false);
  const [sending, setSending] = useState(false);
  const [hubConnected, setHubConnected] = useState(false);

  const connectionRef = useRef(null);
  const messagesEndRef = useRef(null);
  const messagesWrapRef = useRef(null);
  const activeIdRef = useRef(null);

  const user = useMemo(() => {
    try {
      return JSON.parse(localStorage.getItem("kcq_user") || "{}");
    } catch {
      return {};
    }
  }, []);

  const userId = localStorage.getItem("kcq_userId") || user.userId || null;

  const activeThread = useMemo(
    () => threads.find((t) => t.id === activeId) || null,
    [threads, activeId]
  );

  const activeMessages = useMemo(
    () => sortMessagesByTimestamp(messagesByChat[activeId] || []),
    [messagesByChat, activeId]
  );


  useEffect(() => {
    fetchChats();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId]);

  useEffect(() => {
    if (activeId) {
      fetchChatMessages(activeId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeId, userId]);

  useEffect(() => {
    if (!userId) return;

    let cancelled = false;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        transport: signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connectionRef.current = connection;

    connection.on("ChatCreated", (id, sessionName) => {
      if (cancelled) return;

      const newThread = {
        id,
        title: sessionName || "New chat",
        updatedAt: Date.now(),
      };

      setThreads((prev) => {
        const exists = prev.some((t) => t.id === id);
        return exists ? prev : [newThread, ...prev];
      });

      setMessagesByChat((prev) => ({
        ...prev,
        [id]: prev[id] || [],
      }));

      setActiveId(id);
      setSearch("");
    });

    connection.on("ReceiveMessage", (chatId, message, role, timestamp) => {
      if (cancelled || !chatId) return;

      const nextMessage = {
        id: generateId(),
        role: role === "user" ? "you" : "ai",
        type: "text",
        text: message || "",
        ts: timestamp ? new Date(timestamp).getTime() : Date.now(),
      };

      setMessagesByChat((prev) => ({
        ...prev,
        [chatId]: sortMessagesByTimestamp([
          ...(prev[chatId] || []),
          nextMessage,
        ]),
      }));

      // only stop spinner if this is the active chat
      if (chatId === activeIdRef.current && role !== "user") {
        setSending(false);
      }
    });

    connection.on("Error", (errorMessage) => {
      const currentChatId = activeIdRef.current;
      if (cancelled || !currentChatId) return;

      const errorMessageObj = {
        id: generateId(),
        role: "ai",
        type: "text",
        text: errorMessage || "Something went wrong.",
        ts: Date.now(),
      };

    setMessagesByChat((prev) => ({
      ...prev,
      [currentChatId]: sortMessagesByTimestamp([
        ...(prev[currentChatId] || []),
        errorMessageObj,
      ]),
    }));

      setSending(false);
    });

    connection.onreconnecting(() => {
      if (!cancelled) setHubConnected(false);
    });

    connection.onreconnected(() => {
      if (!cancelled) setHubConnected(true);
    });

    connection.onclose(() => {
      if (!cancelled) setHubConnected(false);
    });

    async function startConnection() {
      try {
        await connection.start();
        if (!cancelled) {
          console.log("SignalR connected");
          setHubConnected(true);
        }
      } catch (error) {
        if (
          !cancelled &&
          error?.name !== "AbortError"
        ) {
          console.error("SignalR connection error:", error);
          setHubConnected(false);
        }
      }
    }

    startConnection();

    return () => {
      cancelled = true;
      connection.stop().catch(() => {});
      if (connectionRef.current === connection) {
        connectionRef.current = null;
      }
    };
  }, [userId]);

  useEffect(() => {
    activeIdRef.current = activeId;
  }, [activeId]);

  useEffect(() => {
    const to = setTimeout(() => {
      if (messagesWrapRef.current) {
        messagesWrapRef.current.scrollTop = messagesWrapRef.current.scrollHeight;
      }
    }, 0);
    return () => clearTimeout(to);
  }, [activeId]);

  useEffect(() => {
    const id = requestAnimationFrame(() => {
      if (messagesEndRef.current) {
        messagesEndRef.current.scrollIntoView({ 
          behavior: "smooth",
          block: "end",
        });
      }
    });

    return () => cancelAnimationFrame(id);
  }, [activeId, messagesByChat]);

  async function fetchChats() {
    if (!userId) {
      setThreads([]);
      setLoadingThreads(false);
      return;
    }

    setLoadingThreads(true);
    try {
      const res = await fetch(`${API_BASE}/api/chat/${userId}`);
      if (!res.ok) throw new Error("Failed to load chats");

      const data = await res.json();
      const normalized = Array.isArray(data) ? data.map(normalizeThread) : [];

      setThreads(normalized);
      setActiveId((prev) => prev || normalized[0]?.id || null);
    } catch (error) {
      console.error("Error loading chats:", error);
      setThreads([]);
    } finally {
      setLoadingThreads(false);
    }
  }

  async function fetchChatMessages(chatId) {
    if (!userId || !chatId) return;

    setLoadingMessages(true);
    try {
      const res = await fetch(`${API_BASE}/api/chat/${userId}/${chatId}`);
      if (!res.ok) throw new Error("Failed to load chat messages");

      const data = await res.json();
      const normalizedMessages = Array.isArray(data?.messages)
        ? sortMessagesByTimestamp(data.messages.map(normalizeMessage))
        : [];

      setMessagesByChat((prev) => ({
        ...prev,
        [chatId]: normalizedMessages,
      }));

      setThreads((prev) =>
        prev.map((t) =>
          t.id === chatId
            ? {
                ...t,
                title: data?.sessionName || t.title,
                updatedAt:
                  normalizedMessages[normalizedMessages.length - 1]?.ts || t.updatedAt,
              }
            : t
        )
      );
    } catch (error) {
      console.error("Error loading messages:", error);
      setMessagesByChat((prev) => ({
        ...prev,
        [chatId]: [],
      }));
    } finally {
      setLoadingMessages(false);
    }
  }

  async function newChat() {
    if (!userId || !connectionRef.current || connectionRef.current.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await connectionRef.current.invoke("CreateChat", userId, "New chat");
    } catch (error) {
      console.error("Error creating chat:", error);
    }
  }

  async function deleteChat(id) {
    if (!userId || !id) return;

    try {
      const res = await fetch(`${API_BASE}/api/chat/${userId}/${id}`, {
        method: "DELETE",
      });

      if (!res.ok) throw new Error("Failed to delete chat");

      const remaining = threads.filter((t) => t.id !== id);
      setThreads(remaining);

      setMessagesByChat((prev) => {
        const copy = { ...prev };
        delete copy[id];
        return copy;
      });

      if (activeId === id) {
        setActiveId(remaining[0]?.id || null);
      }
    } catch (error) {
      console.error("Error deleting chat:", error);
    }
  }

  async function renameChat(chatId, newTitle) {
    if (!userId || !chatId || !newTitle?.trim()) return;

    try {
      const res = await fetch(`${API_BASE}/api/chat/${userId}/${chatId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newTitle.trim()),
      });

      if (!res.ok) throw new Error("Failed to rename chat");

      setThreads((prev) =>
        prev.map((t) =>
          t.id === chatId
            ? {
                ...t,
                title: newTitle.trim(),
                updatedAt: Date.now(),
              }
            : t
        )
      );
    } catch (error) {
      console.error("Error renaming chat:", error);
    }
  }

  async function send() {
    const text = draft.trim();
    const connection = connectionRef.current;

    if (
      !text ||
      !activeId ||
      !connection ||
      connection.state !== signalR.HubConnectionState.Connected
    ) {
      return;
    }

    const optimisticUserMessage = {
      id: generateId(),
      role: "you",
      type: "text",
      text,
      ts: Date.now(),
    };

    const shouldRename = activeThread?.title === "New chat";
    const nextTitle = shouldRename ? clampStr(text, 28) : activeThread?.title;

    setDraft("");
    setSending(true);

    setMessagesByChat((prev) => ({
      ...prev,
      [activeId]: sortMessagesByTimestamp([
        ...(prev[activeId] || []),
        optimisticUserMessage,
      ]),
    }));

    setThreads((prev) =>
      prev.map((t) =>
        t.id === activeId
          ? {
              ...t,
              title: nextTitle,
              updatedAt: Date.now(),
            }
          : t
      )
    );

    if (shouldRename) {
      renameChat(activeId, nextTitle);
    }

    try {
      await connection.invoke("SendMessage", userId, activeId, text);
    } catch (error) {
      console.error("Error sending message:", error);

      const errorMessage = {
        id: generateId(),
        role: "ai",
        type: "text",
        text: "Sorry, I couldn't get a response right now.",
        ts: Date.now(),
      };

      setMessagesByChat((prev) => ({
        ...prev,
        [activeId]: sortMessagesByTimestamp([
          ...(prev[activeId] || []),
          errorMessage,
        ]),
      }));

      setSending(false);
    }
  }

  function generateImage() {
    const aiImageMessage = {
      id: generateId(),
      role: "ai",
      type: "image",
      imageUrl:
        "https://images.unsplash.com/photo-1545239351-1141bd82e8a6?auto=format&fit=crop&w=1200&q=70",
      caption: "Mock generated image. Wire this to your image endpoint later.",
      ts: Date.now(),
    };

    setMessagesByChat((prev) => ({
      ...prev,
      [activeId]: sortMessagesByTimestamp([
        ...(prev[activeId] || []),
        aiImageMessage,
      ]),
    }));
  }

  function onComposerKeyDown(e) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      send();
    }
  }

  const filteredThreads = useMemo(() => {
    const q = search.trim().toLowerCase();
    const sorted = [...threads].sort((a, b) => b.updatedAt - a.updatedAt);

    if (!q) return sorted;

    return sorted.filter((t) => {
      const inTitle = (t.title || "").toLowerCase().includes(q);
      const inMsgs = (messagesByChat[t.id] || []).some((m) =>
        (m.text || "").toLowerCase().includes(q)
      );
      return inTitle || inMsgs;
    });
  }, [threads, search, messagesByChat]);

  const headerUpdated = activeThread?.updatedAt
    ? `Updated · ${new Date(activeThread.updatedAt).toLocaleString()}`
    : "Updated · —";

  return (
    <div className="kcqChatPage">
      <div className="kcqChatHeader">
        <div>
          <div className="title">KittyAI</div>
          <div className="sub">
            {headerUpdated}
            <span style={{ marginLeft: 12, opacity: 0.7 }}>
              {hubConnected ? "Live" : "Offline"}
            </span>
          </div>
        </div>
      </div>

      <div className="kcqChatBody">
        <aside className="kcqChatList">
          <div className="top">
            <button
              className="kcqVioletBtn"
              style={{ width: "100%", justifyContent: "center", display: "inline-flex" }}
              onClick={newChat}
              disabled={!hubConnected}
            >
              + New chat
            </button>

            <div style={{ marginTop: 10, position: "relative" }}>
              <div
                style={{
                  position: "absolute",
                  left: 10,
                  top: "50%",
                  transform: "translateY(-50%)",
                  color: "rgba(231,238,252,.55)",
                }}
              >
                <SearchIcon />
              </div>

              <input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search chats…"
                style={{
                  width: "100%",
                  padding: "12px 12px 12px 34px",
                  borderRadius: 12,
                  border: "1px solid rgba(255,255,255,.10)",
                  background: "rgba(0,0,0,.16)",
                  color: "rgba(231,238,252,1)",
                  outline: "none",
                }}
              />
            </div>
          </div>

          <div className="list">
            {loadingThreads ? (
              <div style={{ padding: 12, color: "rgba(231,238,252,.6)" }}>
                Loading chats...
              </div>
            ) : filteredThreads.length === 0 ? (
              <div style={{ padding: 12, color: "rgba(231,238,252,.6)" }}>
                No chats found.
              </div>
            ) : (
              filteredThreads.map((t) => {
                const isActive = t.id === activeId;
                const preview =
                  (messagesByChat[t.id] || [])
                    .slice()
                    .reverse()
                    .find((m) => m.type === "text")?.text || "";

                const ts = t.updatedAt ? new Date(t.updatedAt).toLocaleString() : "";

                return (
                  <div
                    key={t.id}
                    className={`kcqChatItem ${isActive ? "active" : ""}`}
                    onClick={() => setActiveId(t.id)}
                    style={{
                      border: "1px solid rgba(255,255,255,.08)",
                      borderRadius: 16,
                      background: isActive
                        ? "rgba(167,139,250,.12)"
                        : "rgba(255,255,255,.03)",
                      padding: 14,
                      marginBottom: 12,
                      cursor: "pointer",
                    }}
                  >
                    <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                      <div style={{ fontWeight: 900, fontSize: 16 }}>
                        {clampStr(t.title || "Chat", 22)}
                      </div>

                      <div style={{ display: "flex", gap: 8 }}>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            const newName = window.prompt("Rename chat", t.title);
                            if (newName?.trim()) {
                              renameChat(t.id, newName.trim());
                            }
                          }}
                          title="Rename chat"
                          style={{
                            width: 30,
                            height: 30,
                            borderRadius: 10,
                            border: "1px solid rgba(255,255,255,.10)",
                            background: "rgba(255,255,255,.04)",
                            color: "rgba(231,238,252,.75)",
                            display: "grid",
                            placeItems: "center",
                            cursor: "pointer",
                            flex: "0 0 auto",
                          }}
                        >
                          <PencilIcon />
                        </button>

                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            deleteChat(t.id);
                          }}
                          title="Delete chat"
                          style={{
                            width: 30,
                            height: 30,
                            borderRadius: 10,
                            border: "1px solid rgba(255,255,255,.10)",
                            background: "rgba(255,255,255,.04)",
                            color: "rgba(231,238,252,.75)",
                            display: "grid",
                            placeItems: "center",
                            cursor: "pointer",
                            flex: "0 0 auto",
                          }}
                        >
                          <XIcon />
                        </button>
                      </div>
                    </div>

                    <div style={{ marginTop: 8, color: "rgba(231,238,252,.55)", fontSize: 12 }}>
                      {ts}
                    </div>

                    <div style={{ marginTop: 8, color: "rgba(231,238,252,.70)", fontSize: 13 }}>
                      {clampStr(preview, 70)}
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </aside>

        <section className="kcqChatThread">
          <div className="kcqChatMessages" ref={messagesWrapRef}>
            {loadingMessages ? (
              <div style={{ color: "rgba(231,238,252,.6)" }}>Loading messages...</div>
            ) : (
              activeMessages.map((m) => {
                const isYou = m.role === "you";
                const bubbleClass = isYou ? "kcqBubbleYou" : "kcqBubbleAI";

                return (
                  <div
                    key={m.id}
                    style={{
                      display: "grid",
                      gridTemplateColumns: "36px 1fr",
                      gap: 10,
                      marginBottom: 14,
                      alignItems: "start",
                    }}
                  >
                    <AvatarPill label={isYou ? "You" : "AI"} />

                    <div
                      className={bubbleClass}
                      style={{
                        borderRadius: 18,
                        padding: 14,
                        color: "rgba(231,238,252,.92)",
                        boxShadow: "0 12px 40px rgba(0,0,0,.18)",
                      }}
                    >
                      {m.type === "image" ? (
                        <div>
                          <div
                            style={{
                              width: "100%",
                              borderRadius: 16,
                              overflow: "hidden",
                              border: "1px solid rgba(255,255,255,.10)",
                              background: "rgba(0,0,0,.12)",
                            }}
                          >
                            <img
                              src={m.imageUrl}
                              alt={m.caption || "Generated"}
                              style={{ width: "100%", height: "auto", display: "block" }}
                            />
                          </div>

                          {m.caption && (
                            <div style={{ marginTop: 10, color: "rgba(231,238,252,.75)", fontSize: 13 }}>
                              {m.caption}
                            </div>
                          )}
                        </div>
                      ) : (
                        <div style={{ whiteSpace: "pre-wrap", lineHeight: 1.55, fontSize: 15 }}>
                          {m.text}
                        </div>
                      )}

                      <div style={{ marginTop: 10, fontSize: 12, color: "rgba(231,238,252,.45)" }}>
                        {new Date(m.ts).toLocaleTimeString()}
                      </div>
                    </div>
                  </div>
                );
              })
            )}

            <div ref={messagesEndRef} />
          </div>

          <div className="kcqChatComposer">
            <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
              <textarea
                value={draft}
                onChange={(e) => setDraft(e.target.value)}
                onKeyDown={onComposerKeyDown}
                placeholder="Message KittyAI..."
                style={{
                  flex: 1,
                  resize: "none",
                  minHeight: 50,
                  maxHeight: 120,
                  padding: "12px 14px",
                  borderRadius: 14,
                  border: "1px solid rgba(255,255,255,.10)",
                  background: "rgba(0,0,0,.16)",
                  color: "rgba(231,238,252,1)",
                  outline: "none",
                  lineHeight: 1.4,
                }}
              />

              <button
                className="kcqVioletBtn"
                onClick={send}
                disabled={!draft.trim() || sending || !activeId || !hubConnected}
                style={{
                  opacity: draft.trim() && !sending && activeId && hubConnected ? 1 : 0.55,
                  cursor:
                    draft.trim() && !sending && activeId && hubConnected
                      ? "pointer"
                      : "not-allowed",
                }}
              >
                {sending ? "Sending..." : "Send"}
              </button>
            </div>

            <div className="kcqComposerBottomRow">
              <div className="kcqComposerActions">
                <button className="kcqGhostBtn" onClick={generateImage} disabled={!activeId}>
                  <ImageIcon /> Image
                </button>
                <button className="kcqGhostBtn" disabled>
                  + File
                </button>
              </div>

              <div className="kcqComposerHint">
                {hubConnected
                  ? "Enter to send · Shift+Enter for new line"
                  : "Chat connection offline"}
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}