import { useEffect, useMemo, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import "../styles/chatbot.css";
import chatIcon from "../styles/chaticon.png";

const API_BASE = (process.env.REACT_APP_API_BASE_URL || "").replace(/\/$/, "");
const HUB_URL = process.env.REACT_APP_SIGNALR_CHAT_URL || `${API_BASE}/chat`;

function generateId() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

export default function ChatbotDrawer() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([
    {
      id: generateId(),
      role: "assistant",
      text: "Hey! I’m KittyCatQuant AI. Ask me anything.",
    },
  ]);
  const [input, setInput] = useState("");
  const [sending, setSending] = useState(false);
  const [hubConnected, setHubConnected] = useState(false);
  const [chatId, setChatId] = useState(null);

  const endRef = useRef(null);
  const connectionRef = useRef(null);
  const createdChatRef = useRef(false);
  const chatIdRef = useRef(null);

  const user = useMemo(() => {
    try {
      return JSON.parse(localStorage.getItem("kcq_user") || "{}");
    } catch {
      return {};
    }
  }, []);

  const userId = localStorage.getItem("kcq_userId") || user.userId || null;

  useEffect(() => {
    chatIdRef.current = chatId;
  }, [chatId]);

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, open]);

  useEffect(() => {
    function onKey(e) {
      if (e.key === "Escape") setOpen(false);
    }

    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

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

    connection.on("ChatCreated", (id) => {
      if (cancelled) return;
      setChatId(id);
    });

    connection.on("ReceiveMessage", (incomingChatId, message, role) => {
      if (cancelled) return;

      const currentChatId = chatIdRef.current;
      if (
        incomingChatId &&
        currentChatId &&
        String(incomingChatId) !== String(currentChatId)
      ) {
        return;
      }

      setMessages((prev) => [
        ...prev,
        {
          id: generateId(),
          role: role === "user" ? "user" : "assistant",
          text: message || "",
        },
      ]);

      if (role !== "user") {
        setSending(false);
      }
    });

    connection.on("Error", (errorMessage) => {
      if (cancelled) return;

      setMessages((prev) => [
        ...prev,
        {
          id: generateId(),
          role: "assistant",
          text: errorMessage || "Something went wrong.",
        },
      ]);

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
          setHubConnected(true);
        }
      } catch (error) {
        if (!cancelled) {
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
    async function ensureChatExists() {
      if (
        !open ||
        !userId ||
        chatId ||
        createdChatRef.current ||
        !connectionRef.current ||
        connectionRef.current.state !== signalR.HubConnectionState.Connected
      ) {
        return;
      }

      try {
        createdChatRef.current = true;
        await connectionRef.current.invoke("CreateChat", userId, "Drawer chat");
      } catch (error) {
        createdChatRef.current = false;
        console.error("Error creating drawer chat:", error);
      }
    }

    ensureChatExists();
  }, [open, userId, chatId, hubConnected]);

  async function send() {
    const text = input.trim();
    const connection = connectionRef.current;

    if (
      !text ||
      !userId ||
      !chatId ||
      !connection ||
      connection.state !== signalR.HubConnectionState.Connected
    ) {
      return;
    }

    setMessages((prev) => [
      ...prev,
      {
        id: generateId(),
        role: "user",
        text,
      },
    ]);

    setInput("");
    setSending(true);

    try {
      await connection.invoke("SendMessage", userId, chatId, text);
    } catch (error) {
      console.error("Error sending message:", error);

      setMessages((prev) => [
        ...prev,
        {
          id: generateId(),
          role: "assistant",
          text: "Sorry, I couldn't get a response right now.",
        },
      ]);

      setSending(false);
    }
  }

  return (
    <>
      <button className="chatFab" onClick={() => setOpen(true)} title="Chat">
        <img src={chatIcon} alt="Chat" className="chatFabIcon" />
      </button>

      {open && <div className="chatOverlay" onClick={() => setOpen(false)} />}

      <div className={`chatDrawer ${open ? "open" : ""}`}>
        <div className="chatHeader">
          <div>
            <div className="chatTitle">KittyCatQuant AI</div>
            <div className="chatSubtitle">
              {hubConnected ? "Live chat connected" : "Connecting..."}
            </div>
          </div>

          <button className="chatClose" onClick={() => setOpen(false)}>
            ✕
          </button>
        </div>

        <div className="chatBody">
          {messages.map((msg) => (
            <div
              key={msg.id}
              className={`chatMsg ${msg.role === "user" ? "user" : "assistant"}`}
            >
              {msg.text}
            </div>
          ))}
          <div ref={endRef} />
        </div>

        <div className="chatInputRow">
          <input
            className="chatInput"
            value={input}
            placeholder="Type a message…"
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") send();
            }}
            disabled={!hubConnected || !chatId || sending}
          />
          <button
            className="chatSend"
            onClick={send}
            disabled={!input.trim() || !hubConnected || !chatId || sending}
          >
            {sending ? "Sending..." : "Send"}
          </button>
        </div>
      </div>
    </>
  );
}