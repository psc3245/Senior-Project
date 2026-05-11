# KittyCatQuant

A full-stack stock trading platform with an AI-powered chatbot assistant. Users can manage portfolios, track watchlists, view real-time price data, read financial news, and interact with an AI assistant capable of looking up stocks, estimating trades, and executing them.

---

## Project Structure

```
ug_hf_5/
├── Frontend/
│   └── kittycatquant/      # React frontend (Create React App)
└── Backend/
    └── StockTraderBackend/ # ASP.NET Core 9 backend
```

---

## Prerequisites

Before running the project, make sure you have the following installed:

- [Node.js](https://nodejs.org/) (v18 or later recommended)
- [npm](https://www.npmjs.com/) (comes with Node.js)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A PostgreSQL database instance

---

## Backend Setup

### 1. Configure `appsettings.json`

The backend's `appsettings.json` and `appsettings.Development.json` are gitignored and must be created manually. Navigate to the backend project directory:

```
Backend/StockTraderBackend/StockTraderBackend/
```

Create an `appsettings.json` with the following structure, filling in all `<INSERT_...>` placeholders:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Host=127.0.0.1;Port=5432;Database=stockdb;Username=stockuser;Password=<INSERT_PASSWORD>"
  },
  "GNews": {
    "ApiKey": "<INSERT_API_KEY>"
  },
  "Massive": {
    "ApiKey": "<INSERT_API_KEY>",
    "BaseUrl": "https://api.massive.com"
  },
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "<INSERT_API_KEY>",
    "Model": "gpt-4.1-mini",
    "MaxHistoryMessages": 20,
    "SystemPrompt": "You are KittyCat, an AI financial assistant embedded in a stock trading simulator called KittyCatQuant. Your purpose is to help users learn about investing through hands-on experience with their simulated portfolio. You have access to tools that retrieve the user's portfolio, real market data, stock prices, price history, news articles, and watchlists. Using these tools is your default behavior — every single response should begin with you asking yourself what tools you can call to make this response more personalized and data-driven. Do not ever respond from general knowledge alone when a tool call could enrich the answer. If a user says anything at all — even a greeting — consider what portfolio data, market data, or news you could pull to make your response relevant to them specifically. If a user mentions a stock, fetch its price and recent news. If they ask about their performance, pull their portfolio, trades, and snapshots. If they ask a general investing question, still pull their portfolio and relate the answer to their actual holdings. Always ground your responses in real data from your tools. When answering, prefer data over generalities — quote actual prices, actual holdings, and actual returns. A response with numbers is almost always better than one without. You may speak as a knowledgeable financial advisor and give concrete opinions, recommendations, and analysis. This is a simulated educational environment, so be direct and actionable rather than hedging everything. Never fabricate portfolio data, prices, or trades — only report what your tools return. Keep responses focused and digestible, leading with the most important insight and supporting it with data. If the user's question is vague, make a reasonable assumption, state it briefly, and proceed rather than asking clarifying questions."
  },
  "Backfill": {
    "BackfillYears": 2,
    "MaxRetries": 3,
    "RetryDelaySeconds": 60,
    "RequestDelayMilliseconds": 250,
    "EnableRunner": true
  },
  "StockSplitScan": {
    "Enabled": true,
    "LookbackDays": 14,
    "PollInterval": "1.00:00:00",
    "StartupDelay": "00:01:00",
    "RunOnStartup": true
  }
}
```

---

## Required API Keys & Configuration

The backend depends on three external services and one database. Here is what each section means and where to get the credentials.

### `ConnectionStrings.Default` — PostgreSQL

This is the connection string for the PostgreSQL database that stores all application data (users, portfolios, trades, holdings, watchlists, price bars, news articles, stock splits, etc.).

```
Host=127.0.0.1;Port=5432;Database=stockdb;Username=stockuser;Password=<INSERT_PASSWORD>
```

Set up a local or hosted PostgreSQL instance, create a database and user, and replace `<INSERT_PASSWORD>` with that user's password. The other fields (`Host`, `Port`, `Database`, `Username`) can be adjusted to match your environment.

---

### `GNews.ApiKey` — Financial News Feed

GNews powers the in-app news feature, providing article search and top headline fetching. The backend calls `https://gnews.io/api/v4/` to retrieve and cache articles.

To get a key:

1. Go to [https://gnews.io](https://gnews.io) and create a free account.
2. Your API key is shown on your dashboard.
3. The free tier allows a limited number of requests per day — sufficient for development.

```json
"GNews": {
  "ApiKey": "your_gnews_key_here"
}
```

---

### `Massive.ApiKey` — Market Data Provider

Massive is the primary source of stock market data. It supplies:

- **Daily price bars** (OHLCV) used for charting and backfilling historical data
- **Grouped daily aggregates** used by the nightly sync service to keep prices up to date
- **Corporate actions / stock splits** used by the split detection service

The `BaseUrl` should remain `https://api.massive.com` unless you are pointing at a different environment.

To get a key, contact or sign up with your Massive market data account and retrieve your API key from the developer portal.

```json
"Massive": {
  "ApiKey": "your_massive_key_here",
  "BaseUrl": "https://api.massive.com"
}
```

---

### `AI.ApiKey` — OpenAI (Chatbot)

The AI chatbot assistant is powered by OpenAI. The backend sends conversation history and tool definitions to the OpenAI API and streams responses back to the frontend via SignalR.

The default model is `gpt-4.1-mini`. You can change `Model` to any OpenAI chat model you have access to (e.g. `gpt-4o`, `gpt-4-turbo`).

To get a key:

1. Go to [https://platform.openai.com](https://platform.openai.com) and log in or create an account.
2. Navigate to **API Keys** and create a new secret key.
3. Make sure your account has billing set up or is within the free tier limits.

```json
"AI": {
  "Provider": "OpenAI",
  "ApiKey": "sk-...",
  "Model": "gpt-4.1-mini",
  "MaxHistoryMessages": 20,
  "SystemPrompt": "..."
}
```

`MaxHistoryMessages` controls how many prior messages are sent to the model per request. Lowering this reduces token usage and cost.

---

### `Backfill` — Historical Price Backfill Settings

These are not API keys — they control the automatic historical data backfill job that runs on startup (when `EnableRunner: true`). The job fetches missing price bar history from Massive for all tracked symbols.

| Field | Description |
|---|---|
| `BackfillYears` | How many years of daily price history to fetch per symbol (default: 2) |
| `MaxRetries` | How many times to retry a failed Massive request before giving up |
| `RetryDelaySeconds` | How long to wait between retries (seconds) |
| `RequestDelayMilliseconds` | Delay between each symbol's request to avoid rate-limiting |
| `EnableRunner` | Set to `false` to disable the backfill job entirely on startup |

---

### `StockSplitScan` — Stock Split Detection Settings

These are not API keys — they control the background job that scans Massive for recent stock split events and stores them locally.

| Field | Description |
|---|---|
| `Enabled` | Set to `false` to disable the split scanner entirely |
| `LookbackDays` | How many trailing days to scan for split events |
| `PollInterval` | How often the scanner runs (format: `d.hh:mm:ss`). Default is every 24 hours |
| `StartupDelay` | How long to wait after startup before the first scan (default: 1 minute) |
| `RunOnStartup` | If `true`, runs once immediately after the startup delay |

### 2. Restore dependencies

```bash
cd Backend/StockTraderBackend
dotnet restore
```

### 3. Apply database migrations (if applicable)

```bash
cd Backend/StockTraderBackend/StockTraderBackend
dotnet ef database update
```

### 4. Run the backend

```bash
dotnet run --launch-profile http
```

The API will be available at `http://0.0.0.0:8080` by default.

To use HTTPS instead:

```bash
dotnet run --launch-profile https
```

This starts the server at `https://localhost:7151` and `http://localhost:5206`.

### 5. Swagger UI

Once running, the API documentation is available at:

```
http://localhost:8080/swagger
```

### 6. Run backend tests

```bash
cd Backend/StockTraderBackend
dotnet test
```

---

## Frontend Setup

### 1. Install dependencies

```bash
cd Frontend/kittycatquant
npm install
```

### 2. Configure environment variables

Create a `.env` file in `Frontend/kittycatquant/` (this file is gitignored):

```env
REACT_APP_API_BASE_URL=http://localhost:8080
REACT_APP_SIGNALR_CHAT_URL=http://localhost:8080/chat
```

Adjust the URLs to match wherever your backend is running.

### 3. Start the development server

```bash
npm start
```

The app will open at [http://localhost:3000](http://localhost:3000). The page reloads automatically on file changes.

### 4. Build for production

```bash
npm run build
```

Output goes to the `build/` folder, optimized and minified for deployment.

---

## Running Both Together (Local Development)

Open two terminal windows:

**Terminal 1 — Backend:**
```bash
cd Backend/StockTraderBackend/StockTraderBackend
dotnet run --launch-profile http
```

**Terminal 2 — Frontend:**
```bash
cd Frontend/kittycatquant
npm start
```

Then visit [http://localhost:3000](http://localhost:3000) in your browser.

---

## SignalR Chat Hub

The AI chatbot connects via SignalR at `/chat`. See [`Backend/StockTraderBackend/StockTraderBackend/Chat/HowToUse.md`](Backend/StockTraderBackend/StockTraderBackend/Chat/HowToUse.md) for full hub documentation including available methods and event callbacks.

Quick connection example:

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:8080/chat")
  .build();

await connection.start();
connection.invoke("CreateChat", userId, "My Session");
```

---

## Key Technologies

| Layer    | Technology                          |
|----------|-------------------------------------|
| Frontend | React 19, React Router, Recharts, SignalR JS client, Axios |
| Backend  | ASP.NET Core 9, Entity Framework Core, SignalR, PostgreSQL |
| AI       | LLM tool dispatcher with multi-level tools (read, estimate, execute) |

---

## Notes

- `appsettings.json` and `appsettings.Development.json` are excluded from source control. You must create them manually.
- `.env` files for the frontend are also excluded from source control.
- The backend CORS policy is currently set to allow all origins (`AllowAll`), suitable for local development.