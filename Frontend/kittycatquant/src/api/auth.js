import { api } from "./http";

export async function registerUser({ username, email, password }) {
  const res = await api.post("/auth/register", { username, email, password });
  return res.data;
}

export async function loginUser({ identifier, password }) {
  const payload =
    identifier.includes("@")
      ? { email: identifier, password }
      : { username: identifier, password };

  const res = await api.post("/auth/login", payload);
  return res.data;
}
