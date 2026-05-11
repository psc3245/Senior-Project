import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import axios from "axios";

const API_BASE = process.env.REACT_APP_API_BASE_URL;

export default function Signup() {
  const nav = useNavigate();

  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [loading, setLoading] = useState(false);

  async function onSubmit(e) {
    e.preventDefault();

    if (!username || !email || !password) return alert("Please enter username, email, and password.");
    if (password !== confirm) return alert("Passwords do not match");

    try {
      setLoading(true);

      await axios.post(
        `${API_BASE}/auth/register`,
        { username, email, password },
        { headers: { "Content-Type": "application/json" } }
      );

      alert("Signup successful! Please login.");
      nav("/login");
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.error ||
        "Signup failed. Try a different username/email.";
      alert(msg);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="authWrap">
      <div className="card authCard">
    {/* <div style={{ maxWidth: 420, margin: "40px auto", padding: 16 }}> */}
      <h2>KittyCatQuant — Sign Up</h2>

      <form onSubmit={onSubmit} style={{ display: "grid", gap: 12 }}>
        <input
          placeholder="Username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          autoComplete="username"
        />
        <input
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          autoComplete="email"
        />
        <input
          placeholder="Password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="new-password"
        />
        <input
          placeholder="Confirm password"
          type="password"
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          autoComplete="new-password"
        />


        <div className="row" style={{ marginTop: 14 }}>
          <button className="btn btnPrimary" type="submit" disabled={loading}>
          {loading ? "Creating..." : "Create Account"}
        </button>
          <span className="sub" style={{ margin: 0 }}>
            Have an account? <Link to="/login">Login</Link>
          </span>
        </div>
      </form>
    </div>
    </div>
  );
}
