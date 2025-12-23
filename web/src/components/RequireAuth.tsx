"use client";

import React, { useEffect, useState } from "react";
import { useAuth } from "@/lib/auth/AuthContext";
import { usePathname, useRouter } from "next/navigation";

export function RequireAuth({ children }: { children: React.ReactNode }) {
  const auth = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    (async () => {
      const token = await auth.ensureAccessToken();
      if (!mounted) return;
      if (!token) {
        router.replace(`/login?next=${encodeURIComponent(pathname)}`);
        return;
      }
      setLoading(false);
    })();
    return () => {
      mounted = false;
    };
  }, [auth, pathname, router]);

  if (loading) {
    return (
      <div className="container">
        <div className="card">
          <div>Carregando...</div>
          <div className="muted" style={{ marginTop: 8 }}>
            Verificando sessão
          </div>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}

