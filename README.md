#  🎶 Resonite Spotify Status 🎉

Show off your Spotify vibes directly in Resonite! 🎧 This handy tool displays your Spotify status, complete with album art, and lets you control playback.  It even syncs with your Spotify client automatically.  

## ✨ Features ✨

- **Seamless Control:** See song information, including the album icon, and enjoy effortless control over playback with play, pause, skip, and previous track functionality. ⏯️
- **Automatic Syncing:** Enjoy uninterrupted listening with automatic syncing to your Spotify client on startup and during playback. 🔄
- **Collaborative Listening:** Share the musical experience! You and your friends can use the player, with options to manage control access.  🎉
- **Stream Together:** Elevate the experience by transforming the controller into a shared Spotify player! Stream your audio with friends for a truly connected listening session.  🎤
- **Canvas Art Integration:** Immerse yourself in the visual world of music with the display of the currently playing song's Canvas art in Resonite.  🎨
- **Queue It Up:**  Songs can be queued by pasting or dropping their URIs onto the panel.

## 🚀 Getting Started 🚀

### 🔧 Server Setup 🔧

Make sure you have installed the [.NET 5.0 Runtime](https://dotnet.microsoft.com/download)!

1. **Download:** Get the latest release of the server [here](https://github.com/Banane9/NeosSpotifyStatus/releases) and extract it somewhere.
2. **Create Spotify App:** Create a Spotify application on the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/applications).
3. **Set Redirect URI:** Go to the settings of your Spotify application and add `http://localhost:5000/callback` as a Redirect URI.
4. **Configure Server:** Put your application's Client ID and Client Secret into the `config.ini` file of the server program.
5. **Run Server:** Run the program.
6. **Authorize:** Sign in with your Spotify account in the browser window that opened and grant the application access.

### 🌌 Resonite Setup 🌌

1. **Copy Link:** Copy this link: `resrec:///U-Banane9/R-88a9ae63-4861-42e5-a378-bed7468e0e50`
2. **Paste in Resonite:** Paste it into Resonite by pressing Ctrl+V or by opening your Dash menu and pressing the "Paste content from clipboard" button.
3. **Save the Item:** Grab the item that just spawned, open your context menu and save it. (Note: You may need to return to the root of your own inventory to save the folder.)
4. **Find in Inventory:** Open your inventory and enter the folder.
5. **Spawn:** Spawn the item.
6. **Connect:** While the server program is running, click on the button that says "Connect to WebSocket".
7. **Wait:**  Give it a moment to connect. 
8. **Audio Stream (Optional):** Drop your Audio Stream panel into the button being displayed to use the integrated local volume and broadcast / spatialize controls.

## 🙏 Attributions 🙏

- Icons from flaticon.com by Freepik, Pixel Perfect and Kirashastry.
- Modified for use as white on transparent masks and animations. 
