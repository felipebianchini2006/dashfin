"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

export function NavLink({ href, label }: { href: string; label: string }) {
  const pathname = usePathname();
  const active = pathname === href;
  return (
    <Link
      href={href}
      style={{
        padding: "10px 12px",
        borderRadius: 10,
        border: "1px solid var(--border)",
        background: active ? "rgba(96,165,250,0.18)" : "rgba(255,255,255,0.02)"
      }}
    >
      {label}
    </Link>
  );
}

