import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";

interface Props {
  children: ReactNode;
}

export default function ProtectedRoute({ children }: Props) {
  const token = localStorage.getItem("localmind_token");

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  return children;
}