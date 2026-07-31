import { ApiError, type ApiProblem } from "./apiError";
import { authenticatedFetch } from "./authenticatedFetch";

const isJsonResponse = (response: Response): boolean => {
  const contentType = response.headers.get("content-type")?.toLowerCase() ?? "";
  return contentType.includes("application/json") ||
    contentType.includes("application/problem+json") ||
    contentType.includes("+json");
};

const readProblemDetails = async (
  response: Response,
): Promise<ApiProblem | undefined> => {
  if (!isJsonResponse(response)) {
    return undefined;
  }

  try {
    return (await response.json()) as ApiProblem;
  } catch {
    return undefined;
  }
};

const readSuccessResponse = async <T>(response: Response): Promise<T> => {
  if (response.status === 204) {
    return undefined as T;
  }

  if (!isJsonResponse(response)) {
    return undefined as T;
  }

  return (await response.json()) as T;
};

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const response = await authenticatedFetch.fetch(path, init);

  if (!response.ok) {
    const problem = await readProblemDetails(response);

    throw new ApiError(
      response.status,
      problem?.detail ??
        problem?.title ??
        `API-Anfrage fehlgeschlagen (${response.status}).`,
      problem,
    );
  }

  return readSuccessResponse<T>(response);
}

export function apiGet<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  return apiRequest<T>(path, {
    ...init,
    method: "GET",
  });
}

export function apiPost<TResponse, TRequest = unknown>(
  path: string,
  body?: TRequest,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "POST",
    body: body === undefined ? undefined : JSON.stringify(body),
  });
}

export function apiPut<TResponse, TRequest = unknown>(
  path: string,
  body: TRequest,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export function apiPatch<TResponse, TRequest = unknown>(
  path: string,
  body: TRequest,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "PATCH",
    body: JSON.stringify(body),
  });
}

export function apiDelete<TResponse = void>(
  path: string,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "DELETE",
  });
}
