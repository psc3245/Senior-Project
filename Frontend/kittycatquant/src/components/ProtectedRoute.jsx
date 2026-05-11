import { Navigate } from "react-router-dom";

export default function ProtectedRoute({ children }) {
  const loggedIn = localStorage.getItem("kcq_logged_in") === "true";

  if (!loggedIn) {
    return <Navigate to="/login" replace />;
  }

  return children;
}
