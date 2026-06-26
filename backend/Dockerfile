FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app ./app

EXPOSE 8000

# 默认走模板；接 Ollama 时用 -e USE_OLLAMA=1 -e OLLAMA_URL=http://host.docker.internal:11434
ENV USE_OLLAMA=0

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
