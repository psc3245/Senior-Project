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

Create an `appsettings.json` (and optionally `appsettings.Development.json`) with the following structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<your-db-host>;Database=<your-db>;Username=<user>;Password=<password>"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Replace the placeholders with your actual PostgreSQL connection details.

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

### 4. Run frontend tests

```bash
npm test
```

### 5. Build for production

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