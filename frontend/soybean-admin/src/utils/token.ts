const tokenStorageKey = "wecms.auth.tokens";

export interface TokenSet {
  accessToken: string;
  expiresAt: string;
}

let currentTokenSet: TokenSet | null = null;

export function readTokenSet(): TokenSet | null {
  return currentTokenSet;
}

export function saveTokenSet(tokenSet: TokenSet): void {
  currentTokenSet = tokenSet;
  window.localStorage.removeItem(tokenStorageKey);
}

export function clearTokenSet(): void {
  currentTokenSet = null;
  window.localStorage.removeItem(tokenStorageKey);
}
