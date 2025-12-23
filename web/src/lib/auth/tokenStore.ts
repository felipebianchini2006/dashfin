type Subscriber = (token: string | null) => void;

let accessToken: string | null = null;
const subs = new Set<Subscriber>();

export function getAccessToken() {
  return accessToken;
}

export function setAccessToken(token: string | null) {
  accessToken = token;
  for (const sub of subs) sub(accessToken);
}

export function clearAccessToken() {
  setAccessToken(null);
}

export function subscribeAccessToken(sub: Subscriber) {
  subs.add(sub);
  return () => subs.delete(sub);
}

