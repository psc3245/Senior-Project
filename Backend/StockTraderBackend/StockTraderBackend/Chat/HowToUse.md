# ChatHub SignalR Documentation

## Connection

Connect to the hub at `/chat` using the SignalR client library.
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://coms-4020-031.class.las.iastate.edu:8080/chat")
    .build();
```

---

## Client → Server Methods

These are methods the client can invoke on the hub.

### CreateChat
Creates a new chat session for a user.

| Parameter | Type | Description |
|-----------|------|-------------|
| userId | Guid | The ID of the user creating the chat |
| sessionName | string | The name for the new chat session |
```javascript
connection.invoke("CreateChat", "user-guid-here", "My Chat Session");
```

---

### SendMessage
Sends a message to the AI and stores it in the chat history.

| Parameter | Type | Description |
|-----------|------|-------------|
| userId | Guid | The ID of the user sending the message |
| chatId | Guid | The ID of the chat history to send the message to |
| message | string | The message content |
```javascript
connection.invoke("SendMessage", "user-guid-here", "chat-guid-here", "What stocks should I buy?");
```

---

## Server → Client Callbacks

These are events the server pushes to the client.

### ChatCreated
Fired after a successful `CreateChat` invocation.

| Parameter | Type | Description |
|-----------|------|-------------|
| id | Guid | The ID of the newly created chat history |
| sessionName | string | The name of the newly created chat session |
```javascript
connection.on("ChatCreated", (id, sessionName) => {
    console.log(`Chat created: ${sessionName} (${id})`);
});
```

---

### ReceiveMessage
Fired after a successful `SendMessage` invocation with the AI's response.

| Parameter | Type | Description |
|-----------|------|-------------|
| message | string | The message content |
| role | string | Either "user" or "assistant" |
| timestamp | DateTime | UTC timestamp of the message |
```javascript
connection.on("ReceiveMessage", (message, role, timestamp) => {
    console.log(`${role}: ${message} at ${timestamp}`);
});
```

---

### Error
Fired when an error occurs during a hub method invocation.

| Parameter | Type | Description |
|-----------|------|-------------|
| errorMessage | string | Description of the error |
```javascript
connection.on("Error", (errorMessage) => {
    console.error(`Error: ${errorMessage}`);
});
```

---

## Example Flow
```javascript
// 1. Connect
await connection.start();

// 2. Register callbacks
connection.on("ChatCreated", (id, sessionName) => {
    // 4. Send a message once chat is created
    connection.invoke("SendMessage", userId, id, "Hello!");
});

connection.on("ReceiveMessage", (message, role, timestamp) => {
    console.log(`${role}: ${message}`);
});

connection.on("Error", (error) => {
    console.error(error);
});

// 3. Create a chat
connection.invoke("CreateChat", userId, "My Session");
```