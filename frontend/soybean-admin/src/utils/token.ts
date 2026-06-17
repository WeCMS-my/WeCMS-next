const tokenStorageKey = "wecms.auth.tokens";

export interface TokenSet {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export function readTokenSet(): TokenSet | null {
  const rawValue = window.localStorage.getItem(tokenStorageKey);
  if (!rawValue) {
    return null;
  }

  try {
    const parsed = JSON.parse(rawValue) as Partial<TokenSet>;
    if (!parsed.accessToken || !parsed.refreshToken || !parsed.expiresAt) {
      return null;
    }

    return {
      accessToken: parsed.accessToken,
      refreshToken: parsed.refreshToken,
      expiresAt: parsed.expiresAt
    };
  } catch {
    return null;
  }
}

export function saveTokenSet(tokenSet: TokenSet): void {
  window.localStorage.setItem(tokenStorageKey, JSON.stringify(tokenSet));
}

export function clearTokenSet(): void {
  window.localStorage.removeItem(tokenStorageKey);
}
