import { useEffect, useMemo, useState } from "react";
import {
  getConversation,
  getHistory,
  sendMessage,
  type ChatMessage,
  type ConversationHistoryItem,
} from "./chatService";
import {
  getDocuments,
  uploadDocument,
  type DocumentItem,
} from "../documents/documentService";

const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;
const ACCEPTED_FILE_EXTENSIONS = [".pdf", ".txt", ".md"];

function formatFileSize(sizeBytes: number) {
  if (sizeBytes < 1024) return `${sizeBytes} B`;
  if (sizeBytes < 1024 * 1024) return `${(sizeBytes / 1024).toFixed(1)} KB`;

  return `${(sizeBytes / 1024 / 1024).toFixed(1)} MB`;
}

function isAcceptedFile(file: File) {
  const fileName = file.name.toLowerCase();

  return ACCEPTED_FILE_EXTENSIONS.some((extension) =>
    fileName.endsWith(extension)
  );
}

export default function ChatPage() {
  const [message, setMessage] = useState("");
  const [conversationId, setConversationId] = useState<number | null>(null);

  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [history, setHistory] = useState<ConversationHistoryItem[]>([]);
  const [documents, setDocuments] = useState<DocumentItem[]>([]);

  const [isSending, setIsSending] = useState(false);
  const [isUploading, setIsUploading] = useState(false);

  const [chatError, setChatError] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const canSendMessage = useMemo(() => {
    return message.trim().length > 0 && !isSending;
  }, [message, isSending]);

  const loadInitialData = async () => {
    const [historyData, documentsData] = await Promise.all([
      getHistory(),
      getDocuments(),
    ]);

    setHistory(historyData);
    setDocuments(documentsData);
  };

  const loadHistory = async () => {
    const data = await getHistory();
    setHistory(data);
  };

  const loadDocuments = async () => {
    const data = await getDocuments();
    setDocuments(data);
  };

  useEffect(() => {
    loadInitialData().catch((error) => {
      console.error("Error cargando datos iniciales:", error);
      setChatError("No se pudieron cargar el historial y los documentos.");
    });
  }, []);

  const handleNewConversation = () => {
    setConversationId(null);
    setMessages([]);
    setChatError(null);
  };

  const handleOpenConversation = async (selectedConversationId: number) => {
    setChatError(null);

    try {
      const data = await getConversation(selectedConversationId);

      setConversationId(data.id);
      setMessages(
        data.messages.map((item) => ({
          role: item.role,
          content: item.content,
          createdAt: item.createdAt,
        }))
      );
    } catch (error) {
      console.error("Error abriendo conversación:", error);
      setChatError("No se pudo abrir la conversación seleccionada.");
    }
  };

  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (!file || isUploading) return;

    setUploadError(null);

    if (!isAcceptedFile(file)) {
      setUploadError("Formato inválido. Subí un archivo PDF, TXT o MD.");
      return;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      setUploadError("El archivo supera el límite de 10 MB.");
      return;
    }

    setIsUploading(true);

    try {
      await uploadDocument(file);
      await loadDocuments();
    } catch (error) {
      console.error("Error subiendo documento:", error);
      setUploadError("No se pudo procesar el documento.");
    } finally {
      setIsUploading(false);
    }
  };

  const handleSend = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!canSendMessage) return;

    const userMessage = message.trim();

    setMessages((previousMessages) => [
      ...previousMessages,
      {
        role: "user",
        content: userMessage,
      },
    ]);

    setMessage("");
    setIsSending(true);
    setChatError(null);

    try {
      const data = await sendMessage({
        conversationId,
        message: userMessage,
      });

      setConversationId(data.conversationId);

      setMessages((previousMessages) => [
        ...previousMessages,
        {
          role: "assistant",
          content: data.response,
          sources: data.sources,
          usedTool: data.usedTool,
          toolName: data.toolName,
          route: data.route,
        },
      ]);

      await loadHistory();
    } catch (error) {
      console.error("Error enviando mensaje:", error);
      setChatError("No se pudo conectar con el backend.");

      setMessages((previousMessages) => [
        ...previousMessages,
        {
          role: "assistant",
          content: "Error al conectar con el backend.",
        },
      ]);
    } finally {
      setIsSending(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem("localmind_token");
    localStorage.removeItem("localmind_email");
    window.location.href = "/login";
  };

  return (
    <div className="flex h-screen bg-slate-950 text-white">
      <aside className="flex w-80 flex-col gap-4 border-r border-slate-800 bg-slate-900 p-4">
        <header>
          <h1 className="text-xl font-bold">LocalMind AI</h1>
          <p className="text-xs text-slate-400">
            Chat local con documentos, RAG y tools
                      </p>
        </header>

        <button
          type="button"
          onClick={handleNewConversation}
          className="rounded bg-blue-600 p-2 font-medium hover:bg-blue-700"
        >
          Nueva conversación
        </button>

        <section className="rounded-xl border border-slate-800 bg-slate-950/60 p-3">
          <div className="mb-3 flex items-center justify-between gap-2">
            <div>
              <h2 className="text-sm font-semibold">Documentos</h2>
              <p className="text-xs text-slate-500">
                PDF, TXT o MD para consultar
              </p>
            </div>

            <label className="cursor-pointer rounded bg-emerald-600 px-3 py-2 text-xs font-semibold hover:bg-emerald-700">
              {isUploading ? "Procesando..." : "Subir"}
              <input
                type="file"
                accept=".pdf,.txt,.md,text/plain,application/pdf"
                onChange={handleUpload}
                disabled={isUploading}
                className="hidden"
              />
            </label>
          </div>

          {uploadError && (
            <p className="mb-2 rounded border border-red-500/40 bg-red-500/10 p-2 text-xs text-red-300">
              {uploadError}
            </p>
          )}

          <div className="max-h-48 space-y-2 overflow-y-auto">
            {documents.length === 0 ? (
              <p className="text-xs text-slate-500">
                Todavía no subiste documentos.
              </p>
            ) : (
              documents.map((document) => (
                <article
                  key={document.id}
                  className="rounded-lg bg-slate-800 p-2 text-xs"
                >
                  <p className="truncate font-medium">
                    {document.originalFileName}
                  </p>
                  <p className="text-slate-400">
                    {document.chunkCount} chunks ·{" "}
                    {formatFileSize(document.sizeBytes)}
                  </p>
                </article>
              ))
            )}
          </div>
        </section>
 <section className="rounded-xl border border-slate-800 bg-slate-950/60 p-3 text-xs text-slate-400">
          <h2 className="mb-2 text-sm font-semibold text-slate-200">Tools</h2>
          <div className="flex flex-wrap gap-2">
            {["calculator", "summarizeText", "extractTasks", "generateStudyPlan"].map((tool) => (
              <span key={tool} className="rounded-full bg-indigo-500/10 px-2 py-1 text-indigo-200">
                {tool}
              </span>
            ))}
          </div>
        </section>

        <section className="flex-1 space-y-2 overflow-y-auto">
          <h2 className="text-sm font-semibold text-slate-300">Historial</h2>

          {history.length === 0 ? (
            <p className="text-xs text-slate-500">
              Aún no hay conversaciones.
            </p>
          ) : (
            history.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => handleOpenConversation(item.id)}
                className={`w-full cursor-pointer rounded p-2 text-left text-sm hover:bg-slate-700 ${
                  item.id === conversationId ? "bg-slate-700" : "bg-slate-800"
                }`}
              >
                <span className="line-clamp-2">{item.title}</span>
              </button>
            ))
          )}
        </section>

        <button
          type="button"
          onClick={handleLogout}
          className="text-sm text-red-400 hover:text-red-300"
        >
          Cerrar sesión
        </button>
      </aside>

      <main className="flex flex-1 flex-col">
        <section className="flex-1 space-y-4 overflow-y-auto p-6">
          {chatError && (
            <div className="rounded-xl border border-red-500/40 bg-red-500/10 p-3 text-sm text-red-200">
              {chatError}
            </div>
          )}

          {messages.length === 0 && (
            <div className="mt-24 text-center text-slate-500">
              <p className="text-lg font-medium text-slate-400">
                Empezá una conversación
              </p>
              <p className="mt-2 text-sm">
                Subí documentos y preguntá sobre ellos, o iniciá un chat
                general.
              </p>
            </div>
          )}

          {messages.map((item, index) => {
            const isUserMessage = item.role === "user";

            return (
              <article
                key={`${item.role}-${item.createdAt ?? index}`}
                className={`max-w-3xl rounded-2xl p-3 whitespace-pre-wrap ${
                  isUserMessage
                    ? "ml-auto bg-blue-600"
                    : "mr-auto bg-slate-800"
                }`}
              >
                <p>{item.content}</p>

                {!isUserMessage && item.sources && item.sources.length > 0 && (
                  <div className="mt-3 space-y-2 border-t border-slate-700 pt-3">
                    <p className="text-xs font-semibold text-emerald-300">
                      Fuentes RAG usadas
                    </p>

                    {item.sources.map((source) => (
                      <div
                        key={`${source.documentId}-${source.chunkIndex}`}
                        className="rounded-lg bg-slate-900/60 p-2 text-xs text-slate-300"
                      >
                        <p className="font-medium">
                          {source.fileName} · chunk {source.chunkIndex} · score{" "}
                          {source.score.toFixed(2)}
                        </p>
                        <p className="mt-1 text-slate-400">
                          {source.preview}
                        </p>
                      </div>
                    ))}
                  </div>
                )}
              </article>
            );
          })}

          {isSending && (
            <div className="mr-auto rounded-2xl bg-slate-800 p-3 text-slate-400">
              Pensando...
            </div>
          )}
        </section>

        <form
          onSubmit={handleSend}
          className="flex gap-2 border-t border-slate-800 p-4"
        >
          <input
            value={message}
            onChange={(event) => setMessage(event.target.value)}
            placeholder="Preguntá sobre tus documentos..."
            disabled={isSending}
            className="flex-1 rounded-xl border border-slate-700 bg-slate-900 p-3 outline-none placeholder:text-slate-500 disabled:cursor-not-allowed disabled:opacity-60"
          />

          <button
            type="submit"
            disabled={!canSendMessage}
            className="rounded-xl bg-blue-600 px-6 font-semibold hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Enviar
          </button>
        </form>
      </main>
    </div>
  );
}