import client from "./client";

export async function login(email: string, password: string): Promise<string> {
  const body = new URLSearchParams({
    grant_type: "password",
    username: email,
    password,
    scope: "openid",
  });
  const res = await client.post<{ access_token: string }>("/connect/token", body, {
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
  });
  return res.data.access_token;
}

export async function changePassword(currentPassword: string, newPassword: string) {
  await client.post("/api/auth/change-password", { currentPassword, newPassword });
}
