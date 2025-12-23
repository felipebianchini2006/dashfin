export function yyyyMm01(date: Date) {
  const y = date.getUTCFullYear();
  const m = date.getUTCMonth() + 1;
  const mm = String(m).padStart(2, "0");
  return `${y}-${mm}-01`;
}

