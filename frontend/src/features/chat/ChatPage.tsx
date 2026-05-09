import { useEffect, useState } from "react";
import {
  getConversation,
  getHistory,
  sendMessage,
  type ChatMessage,
  type ConversationHistoryItem,
} from "./chatService";

export default function ChatPage() {
  const [message, setMessage] = useState("");
  const [conversationId, setConversationId] = useState<number | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [history, setHistory] = useState<ConversationHistoryItem[]>([]);
  const [loading, setLoading] = useState(false);
const handleOpenConversation = async (
  conversationId: number
) => {
  const data = await getConversation(conversationId);

  setConversationId(data.id);

  setMessages(
    data.messages.map((m) => ({
      role: m.role,
      content: m.content,
      createdAt: m.createdAt,
    }))
  );
};
  const loadHistory = async () => {
    const data = await getHistory();
    setHistory(data);
  };

  useEffect(() => {
    loadHistory();
  }, []);

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();

  console.log("handleSend ejecutado");
  console.log("message:", message);
  console.log("loading:", loading);
    if (!message.trim() || loading) return;

    const currentMessage = message;

    setMessages((prev) => [
      ...prev,
      {
        role: "user",
        content: currentMessage,
      },
    ]);

    setMessage("");
    setLoading(true);

   try {
  const data = await sendMessage({
    conversationId,
    message: currentMessage,
  });

  setConversationId(data.conversationId);

  setMessages((prev) => [
    ...prev,
    {
      role: "assistant",
      content: data.response,
    },
  ]);

  await loadHistory();
} catch (error) {
  console.error("Error enviando mensaje:", error);

  setMessages((prev) => [
    ...prev,
    {
      role: "assistant",
      content: "Error al conectar con el backend.",
    },
  ]);
} finally {
  setLoading(false);
}
  };

  const logout = () => {
    localStorage.removeItem("localmind_token");
    localStorage.removeItem("localmind_email");
    window.location.href = "/login";
  };

  return (
    <div className="h-screen bg-slate-950 text-white flex">
      <aside className="w-72 bg-slate-900 border-r border-slate-800 p-4 flex flex-col">
        <h1 className="text-xl font-bold mb-4">LocalMind AI</h1>

        <button
          onClick={() => {
            setConversationId(null);
            setMessages([]);
          }}
          className="bg-blue-600 hover:bg-blue-700 rounded p-2 mb-4"
        >
          Nueva conversación
        </button>

        <div className="flex-1 overflow-y-auto space-y-2">
          {history.map((item) => (
            <button
  key={item.id}
  onClick={() => handleOpenConversation(item.id)}
  className="w-full text-left bg-slate-800 hover:bg-slate-700 rounded p-2 text-sm cursor-pointer"
>
  {item.title}
</button>
          ))}
        </div>

        <button
          onClick={logout}
          className="text-red-400 hover:text-red-300 text-sm mt-4"
        >
          Cerrar sesión
        </button>
      </aside>

      <main className="flex-1 flex flex-col">
        <section className="flex-1 overflow-y-auto p-6 space-y-4">
          {messages.length === 0 && (
            <div className="text-center text-slate-500 mt-24">
              Empezá una conversación con LocalMind AI.
            </div>
          )}

          {messages.map((msg, index) => (
            <div
              key={index}
              className={`max-w-2xl rounded-2xl p-3 ${
                msg.role === "user"
                  ? "bg-blue-600 ml-auto"
                  : "bg-slate-800 mr-auto"
              }`}
            >
              {msg.content}
            </div>
          ))}

          {loading && (
            <div className="bg-slate-800 mr-auto rounded-2xl p-3 text-slate-400">
              Pensando...
            </div>
          )}
        </section>

        <form
          onSubmit={handleSend}
          className="border-t border-slate-800 p-4 flex gap-2"
        >
          <input
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="Escribí tu mensaje..."
            className="flex-1 bg-slate-900 border border-slate-700 rounded-xl p-3 outline-none"
          />

          {/* <button className="bg-blue-600 hover:bg-blue-700 rounded-xl px-6 font-semibold">
            Enviar
          </button> */}
          <button
  type="submit"
  disabled={loading}
  className="bg-blue-600 hover:bg-blue-700 rounded-xl px-6 font-semibold"
>
  Enviar
</button>
        </form>
      </main>
    </div>
  );
}