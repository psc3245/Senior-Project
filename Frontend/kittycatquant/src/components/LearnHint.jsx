import { useRef, useState, useLayoutEffect, useEffect } from "react";
import { createPortal } from "react-dom";

export default function LearnHint({
  enabled,
  text,
  children,
  width = 280,
  placement = "top-right",
}) {
  const [open, setOpen] = useState(false);
  const [style, setStyle] = useState(null);
  const timer = useRef(null);
  const wrapRef = useRef(null);
  const tipRef = useRef(null);

  const handleEnter = () => {
    if (!enabled) return;
    timer.current = setTimeout(() => setOpen(true), 180);
  };

  const handleLeave = () => {
    if (timer.current) clearTimeout(timer.current);
    setOpen(false);
    setStyle(null);
  };

  useEffect(() => {
    return () => {
      if (timer.current) clearTimeout(timer.current);
    };
  }, []);

  useLayoutEffect(() => {
    if (!open || !wrapRef.current || !tipRef.current) return;

    const gap = 10;
    const margin = 16;

    const wrapRect = wrapRef.current.getBoundingClientRect();
    const tipRect = tipRef.current.getBoundingClientRect();

    let vertical = placement.startsWith("bottom") ? "bottom" : "top";
    let horizontal = placement.endsWith("left") ? "left" : "right";

    if (vertical === "top" && wrapRect.top < tipRect.height + gap + margin) {
      vertical = "bottom";
    } else if (
      vertical === "bottom" &&
      window.innerHeight - wrapRect.bottom < tipRect.height + gap + margin
    ) {
      vertical = "top";
    }

    let top =
      vertical === "top"
        ? wrapRect.top - tipRect.height - gap
        : wrapRect.bottom + gap;

    let left =
      horizontal === "right"
        ? wrapRect.right - tipRect.width
        : wrapRect.left;

    left = Math.max(margin, Math.min(left, window.innerWidth - tipRect.width - margin));
    top = Math.max(margin, Math.min(top, window.innerHeight - tipRect.height - margin));

    setStyle({
      position: "fixed",
      top: `${top}px`,
      left: `${left}px`,
      zIndex: 999999,
      width: `${Math.min(width, window.innerWidth - margin * 2)}px`,
      padding: "10px 12px",
      borderRadius: 12,
      background: "rgba(8,12,20,.97)",
      border: "1px solid rgba(255,255,255,.10)",
      boxShadow: "0 10px 30px rgba(0,0,0,.35)",
      color: "rgba(231,238,252,.92)",
      fontSize: 13,
      lineHeight: 1.45,
      pointerEvents: "none",
      whiteSpace: "normal",
      wordBreak: "break-word",
      boxSizing: "border-box",
    });
  }, [open, placement, text, width]);

  if (!enabled) return children;

  return (
    <div
      ref={wrapRef}
      style={{ position: "relative", display: "block" }}
      onMouseEnter={handleEnter}
      onMouseLeave={handleLeave}
    >
      {children}

      {open &&
        createPortal(
          <div
            ref={tipRef}
            style={
              style || {
                position: "fixed",
                top: "-9999px",
                left: "-9999px",
                width: `${Math.min(width, window.innerWidth - 32)}px`,
                padding: "10px 12px",
                borderRadius: 12,
                background: "rgba(8,12,20,.97)",
                border: "1px solid rgba(255,255,255,.10)",
                boxShadow: "0 10px 30px rgba(0,0,0,.35)",
                color: "rgba(231,238,252,.92)",
                fontSize: 13,
                lineHeight: 1.45,
                pointerEvents: "none",
                whiteSpace: "normal",
                wordBreak: "break-word",
                boxSizing: "border-box",
                zIndex: 999999,
              }
            }
          >
            {text}
          </div>,
          document.body
        )}
    </div>
  );
}