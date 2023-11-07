# C# TCP Chat Application

This repository contains a simple TCP/IP chat application implemented in C#. It showcases a basic client-server architecture where multiple clients can connect to a server and exchange messages. The example includes both the server-side and client-side code, with a focus on socket programming, threading, and basic Windows Forms GUI elements.

## Features
- TCP/IP socket communication
- Multithreading for handling concurrent client connections
- Event-driven programming for real-time message updates
- Windows Forms UI for client interaction
- Asynchronous message handling

## Structure
- `Servidor.cs`: The server code managing client connections and message broadcasting.
- `Cliente.cs`: The client code for connecting to the server, sending, and receiving messages.
- `ClienteForm.cs`: The Windows Forms UI to interact with the user, allowing them to connect to the server, display incoming messages, and send new messages.
- `DatosRecibidosEventArgs.cs`: A custom `EventArgs` class to handle incoming message events.

Feel free to explore, fork, and adapt this example for educational purposes or to kickstart your own TCP/IP based chat application projects.
