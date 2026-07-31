import { getAccessToken } from "./tokenProvider";

export const authenticatedFetch = {
  async fetch(
    input: RequestInfo | URL,
    init: RequestInit = {},
  ): Promise<Response> {
    const headers = new Headers(init.headers);

    headers.set("Accept", "application/json");

    if (!(init.body instanceof FormData) && !headers.has("Content-Type")) {
      headers.set("Content-Type", "application/json");
    }

    const token = await getAccessToken();

    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }

    return fetch(input, {
      ...init,
      headers,
      credentials: "same-origin",
    });
  },
};
