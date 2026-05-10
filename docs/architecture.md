# Arquitectura de LocalMind AI

## Vista general

LocalMind AI separa responsabilidades en frontend, backend, almacenamiento RAG local y Ollama.

```text
React/Vite
  ↓ JWT + API REST
ASP.NET Core
  ├─ Auth/JWT
  ├─ Chat orchestration
  ├─ Tools
  ├─ RAG
  ├─ Metrics
  └─ Security middleware
  ↓
SQLite + rag/
  ↓
Ollama chat + embeddings
```

## Orquestación

El backend decide la ruta de respuesta:

1. Si detecta una tool y no parece pregunta sobre documentos, ejecuta la tool.
2. Si no aplica tool, busca chunks relevantes del usuario.
3. Si encuentra contexto, responde con RAG y fuentes.
4. Si no encuentra contexto, usa chat normal con Ollama.

## Métricas

Cada request exitoso o fallido dentro del flujo de chat registra:

- modelo usado
- tiempo de respuesta
- tokens aproximados
- uso de RAG
- uso de tool
- chunks usados
- ruta (`chat`, `rag`, `tool`, `error`)
- errores
- fecha

## Seguridad básica

- JWT para rutas protegidas.
- Límite de 4000 caracteres por mensaje.
- Validación de extensión y `Content-Type` para documentos.
- Sanitización de nombres de archivo.
- Middleware global de errores con respuestas `ProblemDetails`.
- Bloqueo básico de frases comunes de prompt injection.
