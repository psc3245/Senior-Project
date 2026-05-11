import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import axios from "axios";


const API_BASE = process.env.REACT_APP_API_BASE_URL;

export default function Login() {
  const nav = useNavigate();
  const [identifier, setIdentifier] = useState(""); // username OR email
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);

  async function onSubmit(e) {
    e.preventDefault();
    if (!identifier || !password) return alert("Enter username/email + password");

    try {
      setLoading(true);

      // Send username OR email based on what they typed
      const payload = identifier.includes("@")
        ? { email: identifier, password }
        : { username: identifier, password };

      const res = await axios.post(`${API_BASE}/auth/login`, payload, {
        headers: { "Content-Type": "application/json" },
      });

      // Save auth info locally (adjust once you confirm response shape)
      localStorage.setItem("kcq_logged_in", "true");
      localStorage.setItem("kcq_identifier", identifier);
      if (res.data?.userId) localStorage.setItem("kcq_userId", res.data.userId);


      // If backend returns a token, store it
      if (res.data?.token) {
        localStorage.setItem("kcq_token", res.data.token);
      }

      nav("/dashboard");
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.error ||
        "Login failed. Check credentials and try again.";
      alert(msg);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="authWrap">
      <div className="card authCard">
        <h2>Welcome back</h2>
        <p className="sub">Log in to continue your KittyCatQuant simulation.</p>

        <form onSubmit={onSubmit}>
          <div className="field">
            <label>Username or Email</label>
            <input
              value={identifier}
              onChange={(e) => setIdentifier(e.target.value)}
              placeholder="username or email"
              autoComplete="username"
            />
          </div>

          <div className="field">
            <label>Password</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              autoComplete="current-password"
            />
          </div>

          <div className="row" style={{ marginTop: 14 }}>
            <button className="btn btnPrimary" type="submit" disabled={loading}>
              {loading ? "Logging in..." : "Login"}
            </button>
            <span className="sub" style={{ margin: 0 }}>
              New here? <Link to="/signup">Create an account</Link>
            </span>
          </div>
        </form>
      </div>
    </div>
  );
}
