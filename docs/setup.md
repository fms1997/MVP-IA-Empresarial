# Setup de LocalMind AI

## Ollama

```bash
ollama pull qwen2.5-coder:7b
ollama pull nomic-embed-text
ollama serve
```

## Backend

```bash
cd backend/LocalMind.Api
dotnet restore
dotnet ef database update
dotnet run
```

Variables importantes:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Ollama__BaseUrl`
- `Ollama__Model`
- `Ollama__EmbeddingModel`
- `Rag__StorageRoot`
- `Security__Chat__MaxMessageLength`

## Frontend

```bash
cd frontend
npm install
npm run dev
```

## Docker

```bash
docker compose up --build
```

Si Ollama está en Docker, descargá los modelos dentro del contenedor o mantené el volumen `ollama-data` persistente.
