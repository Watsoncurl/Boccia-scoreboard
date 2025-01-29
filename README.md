# Boccia-scoreboard
Boccia is a Paralympic sport designed for people with severe disabilities. It is a game of precision and strategy, similar to bowling or bocce

## Proposed Solution
### 1. Project Structure
#### Backend:

A .NET Core project that acts as a server.

It contains the game logic, handles the database, and uses SignalR to notify clients of changes.

#### Frontend:

Two desktop applications (Windows Forms or WPF):

Control Application: To enter the values of the game.

Display Application: To display the scoreboard in real time.

### 2. Workflow
Start of Game:

From the Control Application, the names of the players/teams are entered and the game starts.

This information is sent to the server, which stores it in the database.

Play Log:

During the game, from the Control Application, the thrown balls and their position relative to the jack are recorded.

The server calculates the score and notifies the Display Application to update the scoreboard.

### 3. Real-time Visualization:

The Display Application receives updates from the server using SignalR and displays the updated dialer.

### 4. End of Game:

At the end of the game, the server stores the result in the database and notifies both applications.

