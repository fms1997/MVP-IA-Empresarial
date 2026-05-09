import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { login } from "./authService";

export default function LoginPage() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    email: "",
    password: "",
  });

  const [error, setError] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    try {
      const data = await login(form);

      localStorage.setItem("localmind_token", data.token);
      localStorage.setItem("localmind_email", data.email);

      navigate("/chat");
    } catch {
      setError("Credenciales inválidas.");
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-white flex items-center justify-center">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm bg-slate-900 border border-slate-800 rounded-2xl p-6 space-y-4"
      >
        <div className="text-center">
          <h1 className="text-2xl font-bold">LocalMind AI</h1>
          <p className="text-slate-400 text-sm">Iniciar sesión</p>
        </div>

        {error && (
          <div className="bg-red-500/10 text-red-400 text-sm p-2 rounded">
            {error}
          </div>
        )}

        <input
          type="email"
          placeholder="Email"
          className="w-full bg-slate-800 border border-slate-700 rounded p-3"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
        />

        <input
          type="password"
          placeholder="Contraseña"
          className="w-full bg-slate-800 border border-slate-700 rounded p-3"
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
        />

        <button className="w-full bg-blue-600 hover:bg-blue-700 rounded p-3 font-semibold">
          Entrar
        </button>

        <p className="text-sm text-center text-slate-400">
          ¿No tenés cuenta?{" "}
          <Link to="/register" className="text-blue-400">
            Registrate
          </Link>
        </p>
      </form>
    </div>
  );
}