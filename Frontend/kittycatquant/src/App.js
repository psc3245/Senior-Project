import "./App.css";
import { BrowserRouter, Routes, Route, Navigate} from "react-router-dom";
import Login from "./pages/Login";
import Signup from "./pages/Signup";
import Dashboard from "./pages/Dashboard";
import BuySell from "./pages/BuySell";
import Settings from "./pages/Settings";
import ProtectedRoute from "./components/ProtectedRoute";
import DashboardLayout from "./components/DashboardNavbar";
import StockNews from "./pages/StockNews";
import Chat from "./pages/Chat";
import Watchlist from "./pages/Watchlist";

export default function App() {
  const identifier = localStorage.getItem("kcq_identifier") || "demo";
  const logout = () => {
    localStorage.removeItem("kcq_logged_in");
    localStorage.removeItem("kcq_identifier");
    localStorage.removeItem("kcq_token");
    localStorage.removeItem("kcq_userId");
    window.location.href = "/login";
  };
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<Login />} />
        <Route path="/signup" element={<Signup />} />
        <Route
          element={
            <ProtectedRoute>
              <DashboardLayout logout={logout} identifier={identifier} />
            </ProtectedRoute>
          }
        >
        <Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
        <Route path="/buysell" element={<ProtectedRoute><BuySell /></ProtectedRoute>} />
        <Route path="/settings" element={<ProtectedRoute><Settings /></ProtectedRoute>} />
        <Route path="/stockNews" element={<ProtectedRoute><StockNews /></ProtectedRoute>} />
        <Route path="/chat" element={<ProtectedRoute><Chat /></ProtectedRoute>} />
        <Route path="/watchList" element={<ProtectedRoute><Watchlist /></ProtectedRoute>} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
