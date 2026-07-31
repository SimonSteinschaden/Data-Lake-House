interface DevelopmentTokenResponse {
  accessToken: string;
  expiresAt: string;
}

let accessToken: string | null = null;
let developmentTokenRequest: Promise<string> | null = null;

const requestDevelopmentToken = async (): Promise<string> => {
  const response = await fetch("/api/v1/auth/development-token", {
    method: "POST",
    headers: { Accept: "application/json" },
    credentials: "same-origin",
  });

  if (!response.ok) {
    throw new Error("Es ist kein Bearer Token verfügbar. Bitte anmelden.");
  }

  const token = await response.json() as DevelopmentTokenResponse;
  accessToken = token.accessToken;
  return token.accessToken;
};

export const setAccessToken = (token: string | null): void => {
  accessToken = token?.trim() || null;
};

export const getAccessToken = async (): Promise<string | null> => {
  if (accessToken) return accessToken;

  developmentTokenRequest ??= requestDevelopmentToken()
    .finally(() => { developmentTokenRequest = null; });

  return developmentTokenRequest;
};
