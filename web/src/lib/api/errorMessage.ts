"use client";

import axios from "axios";
import type { ProblemDetails } from "@/lib/api/types";

export function errorMessage(err: unknown, fallback: string) {
  if (axios.isAxiosError<ProblemDetails>(err)) {
    const d = err.response?.data;
    if (d?.detail) return d.detail;
    if (d?.title) return d.title;
  }
  if (err instanceof Error && err.message) return err.message;
  return fallback;
}

