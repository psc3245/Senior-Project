import { useMemo } from "react";
import { useOutletContext } from "react-router-dom";
import "../styles/settings.css";

export default function Settings({ logout }) {
  const outletContext = useOutletContext() || {};
  const { learnMode = false, setLearnMode = () => {} } = outletContext;

  const user = useMemo(() => {
    try {
      return JSON.parse(localStorage.getItem("kcq_user") || "{}");
    } catch {
      return {};
    }
  }, []);

  const identifier =
    localStorage.getItem("kcq_identifier") ||
    user.username ||
    user.email ||
    "Signed-in user";

  const email =
    user.email ||
    localStorage.getItem("kcq_email") ||
    identifier;

  const userId =
    localStorage.getItem("kcq_userId") ||
    user.userId ||
    "Not available";

  function handleLogout() {
    if (typeof logout === "function") {
      logout();
      return;
    }

    localStorage.removeItem("kcq_token");
    localStorage.removeItem("kcq_user");
    localStorage.removeItem("kcq_userId");
    localStorage.removeItem("kcq_identifier");
    window.location.href = "/";
  }

  async function handleDeleteAccount() {
    if (!userId || userId === "Not available") {
      alert("No user ID found.");
      return;
    }

    const confirmed = window.confirm(
      "Are you sure you want to delete your account? This cannot be undone."
    );

    if (!confirmed) return;

    try {
      const API_BASE = process.env.REACT_APP_API_BASE_URL || "";
      const res = await fetch(`${API_BASE}/users/${userId}`, {
        method: "DELETE",
      });

      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(`Failed to delete account: ${res.status} - ${errorText}`);
      }

      alert("Account deleted successfully.");
      handleLogout();
    } catch (err) {
      console.error(err);
      alert(err.message || "Could not delete account.");
    }
  }

  return (
    <div className="settingsPage">
      <div className="settingsHeader">
        <h1>Settings</h1>
        <p>Manage your account, security, and preferences.</p>
      </div>

      <div className="settingsGrid">
        <div className="settingsCard">
          <h2>Account Details</h2>

          <div className="settingsRow">
            <span className="label">Username</span>
            <span className="value">{identifier}</span>
          </div>

          <div className="settingsRow">
            <span className="label">Email</span>
            <span className="value">{email}</span>
          </div>

          <div className="settingsRow">
            <span className="label">User ID</span>
            <span className="value muted">{userId}</span>
          </div>
        </div>

        <div className="settingsCard">
          <h2>Preferences</h2>

          <div className="settingsRow">
            <span className="label">Learn Mode</span>
            <button
              className={`toggleBtn ${learnMode ? "on" : ""}`}
              onClick={() => setLearnMode((prev) => !prev)}
              type="button"
            >
              {learnMode ? "On" : "Off"}
            </button>
          </div>
        </div>

        <div className="settingsCard">
          <h2>Security</h2>

          <div className="settingsActions">
            <button className="secondaryBtn" type="button" onClick={handleLogout}>
              Log Out
            </button>

            <button className="dangerBtn" type="button" onClick={handleDeleteAccount}>
              Delete Account
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}