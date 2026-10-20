# syntax=docker/dockerfile:1.7
# Argos API (.NET 10): RAG de incidentes, auth, configuración, observabilidad, auto-doc y k6.
# Solución en capas: Common → Entities → Repository → Business → Api (controllers).

# ─── Build ───────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Primero solo los .csproj: el restore queda cacheado mientras no cambien las dependencias.
COPY Directory.Build.props ./
COPY Argos.Common/Argos.Common.csproj Argos.Common/
COPY Argos.Entities/Argos.Entities.csproj Argos.Entities/
COPY Argos.Repository/Argos.Repository.csproj Argos.Repository/
COPY Argos.Business/Argos.Business.csproj Argos.Business/
COPY Argos.Api/Argos.Api.csproj Argos.Api/
RUN dotnet restore Argos.Api/Argos.Api.csproj
COPY Argos.Common/ Argos.Common/
COPY Argos.Entities/ Argos.Entities/
COPY Argos.Repository/ Argos.Repository/
COPY Argos.Business/ Argos.Business/
COPY Argos.Api/ Argos.Api/
RUN dotnet publish Argos.Api/Argos.Api.csproj -c Release -o /app --no-restore

# ─── Runtime ─────────────────────────────────────────────────────────────────
# Imagen glibc (Ubuntu): la necesita el runtime nativo de ONNX.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Modelo de embeddings (ONNX cuantizado) + tokenizer SentencePiece de XLM-R.
# Van antes del código para que el layer (~240 MB) quede cacheado entre builds.
ADD --checksum=sha256:634d0f66c29dc934c8fa72b8a4fe91dd4d420a22f1d82a241058d4316e659a99 \
    https://huggingface.co/qdrant/paraphrase-multilingual-MiniLM-L12-v2-onnx-Q/resolve/faf4aa4225822f3bc6376869cb1164e8e3feedd0/model_optimized.onnx \
    /app/models/model_optimized.onnx
ADD --checksum=sha256:cfc8146abe2a0488e9e2a0c56de7952f7c11ab059eca145a0a727afce0db2865 \
    https://huggingface.co/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2/resolve/main/sentencepiece.bpe.model \
    /app/models/sentencepiece.bpe.model

COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8000 \
    EMBEDDING_MODEL_DIR=/app/models \
    KNOWLEDGE_DIR=/app/knowledge \
    DOTNET_gcServer=0

# Corre como root: necesita el Docker socket para lanzar k6.
EXPOSE 8000
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=5 \
    CMD ["dotnet", "/app/Argos.Api.dll", "healthcheck"]
ENTRYPOINT ["dotnet", "/app/Argos.Api.dll"]
