# LocalMind AI

LocalMind AI es un MVP full stack de asistente IA local para consultar documentos personales o técnicos con chat, RAG, tools simples, historial, métricas y seguridad básica.

## Stack

- **Frontend:** React + Vite + Tailwind
- **Backend:** ASP.NET Core + Entity Framework Core
- **IA local:** Ollama
- **Modelo chat:** `qwen2.5-coder:7b`
- **Embeddings:** `nomic-embed-text`
- **RAG:** almacenamiento local de documentos, chunks y embeddings serializados
- **DB:** SQLite
- **Auth:** JWT
- **Docker:** compose local opcional

## Funcionalidades del MVP

1. Registro, login, JWT y rutas protegidas.
2. Chat con Ollama y guardado de conversaciones.
3. Upload de PDF/TXT/MD, extracción, chunking, embeddings y respuestas con fuentes.
4. Tools simples: `calculator`, `summarizeText`, `extractTasks`, `generateStudyPlan`.
5. Orquestación simple entre tool, RAG y chat normal.
6. Panel de métricas con modelo, latencia, tokens aproximados, uso de RAG/tools, chunks y errores.
7. Seguridad básica: límites de caracteres, validación/sanitización de archivos, JWT y bloqueo básico de prompt injection.

## Requisitos locales

- .NET SDK compatible con `net10.0`
- Node.js 20+
- Ollama instalado localmente
- Modelos de Ollama:

```bash
ollama pull qwen2.5-coder:7b
ollama pull nomic-embed-text
```

## Ejecución local paso a paso

### 1. Backend

```bash
cd backend/LocalMind.Api
dotnet restore
dotnet ef database update
dotnet run
```

La API queda disponible en `http://localhost:5201/api` y Swagger en entorno Development.

### 2. Frontend

```bash
cd frontend
npm install
npm run dev
```

La app queda disponible en `http://localhost:5173`.

### 3. Flujo demo recomendado

1. Crear una cuenta nueva.
2. Iniciar sesión.
3. Subir un PDF, TXT o MD.
4. Preguntar algo sobre el documento para validar RAG y fuentes.
5. Probar una consulta matemática como `Calculá 120 / 20` para validar tools.
6. Entrar a **Métricas** para ver latencia, tokens aproximados, chunks, ruta y errores.

## Docker local opcional

```bash
docker compose up --build
```

Servicios expuestos:

- Frontend: `http://localhost:5173`
- Backend: `http://localhost:5201`
- Ollama: `http://localhost:11434`

> Nota: la primera ejecución puede requerir descargar modelos dentro del contenedor de Ollama o montarlos desde un volumen persistente.

## Estructura

```text
backend/LocalMind.Api       API ASP.NET Core
frontend/                   React + Vite
rag/documents               documentos originales
rag/chunks                  chunks de texto
docs/                       arquitectura, setup y roadmap
```

## Etapa 5 completada

La etapa 5 deja el proyecto presentable como portfolio profesional: métricas visibles, validaciones de seguridad básicas, middleware de errores, documentación de setup/demo y Docker local.
